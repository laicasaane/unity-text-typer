using System.Collections.Generic;

namespace RedBlueGames.Tools.TextTyper
{
    public interface IGetter<out T>
    {
        T Get();
    }

    public interface IAction<in T>
    {
        void Invoke(T item);
    }

    public class PoolableList<T>
    {
        private readonly Queue<T> pool = new Queue<T>();
        private readonly IGetter<T> getter;
        private readonly List<T> list;

        public int Count
        {
            get { return this.list.Count; }
        }

        public PoolableList(IGetter<T> getter)
        {
            this.list = new List<T>();
            this.getter = getter;
        }

        public void ReturnAll()
        {
            for (var i = 0; i < this.list.Count; i++)
            {
                this.pool.Enqueue(this.list[i]);
            }

            this.list.Clear();
        }

        public void ReturnAll<TOnReturn>() where TOnReturn : IAction<T>, new()
        {
            var onReturn = new TOnReturn();

            for (var i = 0; i < this.list.Count; i++)
            {
                var item = this.list[i];

                onReturn.Invoke(item);
                this.pool.Enqueue(item);
            }

            this.list.Clear();
        }

        public T GetItem()
        {
            var item = this.pool.Count > 0 ? this.pool.Dequeue() : this.getter.Get();
            this.list.Add(item);

            return item;
        }

        public T GetItem<TOnGet>() where TOnGet : IAction<T>, new()
        {
            var item = this.pool.Count > 0 ? this.pool.Dequeue() : this.getter.Get();
            this.list.Add(item);

            new TOnGet().Invoke(item);

            return item;
        }

        public void GetUnsafe(out List<T> list, out int count)
        {
            list = this.list;
            count = this.list.Count;
        }
    }
}
