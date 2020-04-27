namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Text Typer/Config", fileName = "TextTyperConfig")]
    public sealed class TextTyperConfig : ScriptableObject
    {
        public float PrintDelay = 0.02f;

        public float PunctuationDelayMultiplier = 8f;

        [SerializeField]
        private List<string> punctuations = new List<string>
        {
            ".",
            ",",
            "!",
            "?"
        };

        public List<string> Punctuations
        {
            get { return this.punctuations; }
        }
    }
}