namespace RedBlueGames.Tools.TextTyper.Tests
{
    using UnityEditor;
    using UnityEngine;
    using NUnit.Framework;

    public class TextTagParserTests
    {
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

        [Test]
        public void RemoveCustomTags_EmptyString_ReturnsEmpty()
        {
            var textToType = string.Empty;
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveCustomTags_OnlyUnityRichTextTags_ReturnsUnityTags()
        {
            var textToType = "<b><i></i></b>";
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveCustomTags_OnlyCustomRichTextTags_ReturnsEmpty()
        {
            var textToType = "<delay=5></delay><anim=3></anim><animation=sine></animation>";
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = string.Empty;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveUnityTags_AllUnityTags_ReturnsNoTags()
        {
            var builder = new System.Text.StringBuilder();
            var expectedTextBuilder = new System.Text.StringBuilder();

            for (int i = 0; i < UnityTags.Length; ++i)
            {
                var tag = UnityTags[i];
                builder.Append($"<{tag}>{i}</{tag}>");
                expectedTextBuilder.Append($"{i}");
            }

            var textToType = builder.ToString();
            var generatedText = TextTagParser.RemoveUnityTags(textToType);

            var expectedText = expectedTextBuilder.ToString();

            Assert.AreEqual(expectedText, generatedText);
        }
    }
}