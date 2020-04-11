namespace RedBlueGames.Tools.TextTyper.Tests
{
    using UnityEditor;
    using UnityEngine;
    using NUnit.Framework;

    public class TextTagParserTests
    {
        [Test]
        public void RemoveCustomTags_EmptyString_ReturnsEmpty( )
        {
            var textToType = string.Empty;
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveCustomTags_OnlyUnityRichTextTags_ReturnsUnityTags( )
        {
            var textToType = "<b><i></i></b>";
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveCustomTags_OnlyCustomRichTextTags_ReturnsEmpty( )
        {
            var textToType = "<delay=5></delay><anim=3></anim><animation=sine></animation>";
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = string.Empty;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveUnityTags_AllUnityTags_ReturnsNoTags( )
        {
            //"b", "i", "size", "color", "style" };
            var textToType = "<b>a</b><i>b</i><size=40>c</size><color=red>d</color><style=C1>e</style>";
            var generatedText = TextTagParser.RemoveUnityTags(textToType);

            var expectedText = "abcde";

            Assert.AreEqual(expectedText, generatedText);
        }
    }
}