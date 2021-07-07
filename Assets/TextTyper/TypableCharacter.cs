namespace RedBlueGames.Tools.TextTyper
{
    /// <summary>
    /// This class represents a printed character moment, which should correspond with a
    /// delay in the text typer. It became necessary to make this a class when I had
    /// to account for Sprite tags which are replaced by a sprite that counts as a "visble"
    /// character. These sprites would not be in the Text string stripped of tags,
    /// so this allows us to track and print them with a delay.
    /// </summary>
    public class TypableCharacter
    {
        public float Delay;

        private string value;
        private bool isSprite;

        public void InitializeAsSprite()
        {
            this.value = string.Empty;
            this.isSprite = true;
            this.Delay = 0f;
        }

        public void InitializeAsCharacter(string value)
        {
            this.value = value ?? string.Empty;
            this.isSprite = false;
            this.Delay = 0f;
        }

        public override string ToString()
        {
            return this.isSprite ? Sprite : this.value;
        }

        private const string Sprite = nameof(Sprite);
    }
}