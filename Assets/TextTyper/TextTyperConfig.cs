namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Text Typer/Config", fileName = "TextTyperConfig")]
    public sealed class TextTyperConfig : ScriptableObject
    {
        [Tooltip("The delay time between each print")]
        public float PrintDelay = 0.02f;

        [Tooltip("The amount of characters to be printed each time")]
        public int PrintAmount = 1;

        [Tooltip("The delay time will be multiplied by this when the character is a punctuation mark")]
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