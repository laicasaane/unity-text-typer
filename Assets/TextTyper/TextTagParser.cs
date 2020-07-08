namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections.Generic;

    /// <summary>
    /// "Utility class to assist with parsing HTML-style tags in strings
    /// </summary>
    public static class TextTagParser
    {
        /// <summary>
        /// Define custom tags here. These should also be added to the CustomTagTypes List below
        /// </summary>
        public struct CustomTags
        {
            public const string Delay = "delay";
            public const string Speed = "speed";
            public const string Anim = "anim";
            public const string Animation = "animation";
        }

        private static readonly string[] UnityTags = new string[]
        {
            "b",
            "i",
            "s",
            "u",
            "br",
            "nobr",
            "size",
            "color",
            "style",
            "width",
            "align",
            "alpha",
            "cspace",
            "font",
            "indent",
            "line-height",
            "line-indent",
            "link",
            "lowercase",
            "uppercase",
            "smallcaps",
            "margin",
            "mark",
            "mspace",
            "noparse",
            "page",
            "pos",
            "space",
            "sprite",
            "sup",
            "sub",
            "voffset",
            "gradient"
        };

        private static readonly string[] CustomTagTypes = new string[]
        {
            CustomTags.Delay,
            CustomTags.Speed,
            CustomTags.Anim,
            CustomTags.Animation,
        };

        private static readonly Queue<TextSymbol> SymbolPool = new Queue<TextSymbol>();

        private static TextSymbol GetSymbol()
        {
            if (SymbolPool.Count > 0)
                return SymbolPool.Dequeue();

            return new TextSymbol();
        }

        private static void Pool(IEnumerable<TextSymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                SymbolPool.Enqueue(symbol);
            }
        }

        public static void CreateSymbolListFromText(string text, List<TextSymbol> symbolList)
        {
            Pool(symbolList);
            symbolList.Clear();

            var parsedCharacters = 0;

            while (parsedCharacters < text.Length)
            {
                TextSymbol symbol;

                // Check for tags
                var remainingText = text.Substring(parsedCharacters, text.Length - parsedCharacters);

                if (RichTextTag.StringStartsWithTag(remainingText))
                {
                    var tag = RichTextTag.ParseNext(remainingText);
                    symbol = GetSymbol().Initialize(tag);
                }
                else
                {
                    symbol = GetSymbol().Initialize(remainingText.Substring(0, 1));
                }

                parsedCharacters += symbol.Length;
                symbolList.Add(symbol);
            }
        }

        public static string RemoveAllTags(string textWithTags)
        {
            string textWithoutTags = textWithTags;
            textWithoutTags = RemoveUnityTags(textWithoutTags);
            textWithoutTags = RemoveCustomTags(textWithoutTags);

            return textWithoutTags;
        }

        public static string RemoveCustomTags(string textWithTags)
        {
            return RemoveTags(textWithTags, CustomTagTypes);
        }

        public static string RemoveUnityTags(string textWithTags)
        {
            return RemoveTags(textWithTags, UnityTags);
        }

        private static string RemoveTags(string textWithTags, params string[] tags)
        {
            string textWithoutTags = textWithTags;
            foreach (var tag in tags)
            {
                textWithoutTags = RichTextTag.RemoveTagsFromString(textWithoutTags, tag);
            }

            return textWithoutTags;
        }
    }
}