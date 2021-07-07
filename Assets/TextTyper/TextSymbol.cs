namespace RedBlueGames.Tools.TextTyper
{
    using UnityEngine;

    public class TextSymbol
    {
        public TextSymbol Initialize(string character)
        {
            this.Tag = null;
            this.Character = character ?? string.Empty;

            return this;
        }

        public TextSymbol Initialize(RichTextTag tag)
        {
            this.Character = null;
            this.Tag = tag;

            return this;
        }

        public string Character { get; private set; }

        public RichTextTag Tag { get; private set; }

        public int Length
        {
            get
            {
                return this.Text.Length;
            }
        }

        public string Text
        {
            get
            {
                if (this.IsTag)
                {
                    return this.Tag.TagText;
                }
                else
                {
                    return this.Character;
                }
            }
        }

        public bool IsTag
        {
            get
            {
                return this.Tag != null;
            }
        }

        /// <summary>
        /// Gets a value indicating this Symbol represents a Sprite, which is treated
        /// as a visible character by TextMeshPro.
        /// See Issue #35 for details.
        /// </summary>
        /// <value></value>
        public bool IsReplacedWithSprite
        {
            get
            {
                return this.IsTag && this.Tag.TagType == "sprite";
            }
        }

        public float GetFloatParameter(float defaultValue = 0f)
        {
            if (!this.IsTag)
            {
                Debug.LogWarning("Attempted to retrieve parameter from symbol that is not a tag.");
                return defaultValue;
            }

            float paramValue;
            if (!float.TryParse(this.Tag.Parameter, out paramValue))
            {
                var warning = string.Format(
                              "Found Invalid parameter format in tag [{0}]. " +
                              "Parameter [{1}] does not parse to a float.",
                              this.Tag,
                              this.Tag.Parameter);
                Debug.LogWarning(warning);
                paramValue = defaultValue;
            }

            return paramValue;
        }
    }
}