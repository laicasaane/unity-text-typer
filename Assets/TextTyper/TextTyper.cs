namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Type text component types out Text one character at a time. Heavily adapted from synchrok's GitHub project.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TextTyper : MonoBehaviour
    {
        /// <summary>
        /// The delay time between each print.
        /// </summary>
        public const float PrintDelay = 0.02f;

        /// <summary>
        /// The amount of characters to be printed each time.
        /// </summary>
        public const int PrintAmount = 1;

        /// <summary>
        /// Default delay setting will be multiplied by this when the character is a punctuation mark
        /// </summary>
        public const float PunctuationDelayMultiplier = 8f;

        /// <summary>
        /// Characters that are considered punctuation in this language. TextTyper pauses on these characters
        /// a bit longer by default. Could be a setting sometime since this doesn't localize.
        /// </summary>
        private static readonly List<string> _punctuations = new List<string>
        {
            ".",
            ",",
            "!",
            "?"
        };

        /// <summary>
        /// Characters that are considered punctuation in this language. TextTyper pauses on these characters
        /// a bit longer by default. Could be a setting sometime since this doesn't localize.
        /// </summary>
        public static IEnumerable<string> Punctuations
        {
            get
            {
                return _punctuations;
            }
        }

        [SerializeField]
        [Tooltip("The configuration that overrides default settings.")]
        private TextTyperConfig config = null;

        [SerializeField]
        [Tooltip("The library of ShakePreset animations that can be used by this component.")]
        private ShakeLibrary shakeLibrary = null;

        [SerializeField]
        [Tooltip("The library of CurvePreset animations that can be used by this component.")]
        private CurveLibrary curveLibrary = null;

        [SerializeField]
        [Tooltip("If set, the typer will type text even if the game is paused (Time.timeScale = 0)")]
        private bool useUnscaledTime = false;

        [SerializeField]
        [Tooltip("Event that's called when the text has finished printing.")]
        private UnityEvent printCompleted = new UnityEvent();

        [SerializeField]
        [Tooltip("Event called when a character is printed. Inteded for audio callbacks.")]
        private CharacterPrintedEvent characterPrinted = new CharacterPrintedEvent();

        private TMP_Text textComponent;
        private TextTyperConfig defaultConfig;
        private Coroutine typeTextCoroutine;
        private string taglessText;

        private readonly List<float> characterPrintDelays;
        private readonly List<TextAnimation> animations;
        private readonly List<TextSymbol> textAsSymbolList;

        /// <summary>
        /// Gets the PrintCompleted callback event.
        /// </summary>
        /// <value>The print completed callback event.</value>
        public UnityEvent PrintCompleted
        {
            get
            {
                return this.printCompleted;
            }
        }

        /// <summary>
        /// Gets the CharacterPrinted event, which includes a string for the character that was printed.
        /// </summary>
        /// <value>The character printed event.</value>
        public CharacterPrintedEvent CharacterPrinted
        {
            get
            {
                return this.characterPrinted;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TextTyper"/> is currently printing text.
        /// </summary>
        /// <value><c>true</c> if printing; otherwise, <c>false</c>.</value>
        public bool IsTyping
        {
            get
            {
                return this.typeTextCoroutine != null;
            }
        }

        private TMP_Text TextComponent
        {
            get
            {
                if (this.textComponent == null)
                {
                    this.textComponent = GetComponent<TMP_Text>();
                }

                return this.textComponent;
            }
        }

        /// <summary>
        /// Gets the number of characters that have been printed.
        /// This number will be reset each time this <see cref="TextTyper"/> starts printing text.
        /// </summary>
        public int PrintedCharacters { get; private set; }

        public TextTyper()
        {
            this.characterPrintDelays = new List<float>();
            this.animations = new List<TextAnimation>();
            this.textAsSymbolList = new List<TextSymbol>();
        }

        private float GetPrintDelay()
        {
            if (this.defaultConfig)
                return this.defaultConfig.PrintDelay;

            return PrintDelay;
        }

        private int GetPrintAmount()
        {
            if (this.defaultConfig)
                return this.defaultConfig.PrintAmount;

            return PrintAmount;
        }

        private float GetPunctuationDelayMultiplier()
        {
            if (this.defaultConfig)
                return this.defaultConfig.PunctuationDelayMultiplier;

            return PunctuationDelayMultiplier;
        }

        private List<string> GetPunctutations()
        {
            if (this.defaultConfig)
                return this.defaultConfig.Punctuations;

            return _punctuations;
        }

        /// <summary>
        /// Types the text into the Text component character by character, using the specified (optional) print delay per character.
        /// </summary>
        /// <param name="text">Text to type.</param>
        /// <param name="printDelay">Print delay (in seconds) per character.</param>
        /// <param name="skipChars">The number of characters to be already typed initially.</param>
        /// <param name="printAmount">The amount of characters to be printed each time.</param>
        public void TypeText(string text, float printDelay = -1, int skipChars = 0, int printAmount = -1)
        {
            TypeText(text, this.config, printDelay, skipChars, printAmount);
        }

        /// <summary>
        /// Types the text into the Text component character by character, using the specified (optional) print delay per character.
        /// </summary>
        /// <param name="text">Text to type.</param>
        /// <param name="config">The alternated config. If null the <see cref="TextTyper.config"/> will be used.</param>
        /// <param name="skipChars">The number of characters to be already typed initially.</param>
        public void TypeText(string text, TextTyperConfig config, int skipChars = 0)
        {
            TypeText(text, config, -1f, skipChars, -1);
        }

        private void TypeText(string text, TextTyperConfig config, float printDelay, int skipChars, int printAmount)
        {
            CleanupCoroutine();

            // Remove all existing TextAnimations
            // TODO - Would be better to pool/reuse these components
            foreach (var anim in GetComponents<TextAnimation>())
            {
                Destroy(anim);
            }

            this.defaultConfig = config ? config : this.config;
            ProcessCustomTags(text, printDelay > 0 ? printDelay : GetPrintDelay());

            if (skipChars < 0)
                skipChars = 0;

            if (printAmount < 1)
                printAmount = GetPrintAmount();

            this.typeTextCoroutine = StartCoroutine(TypeTextCharByChar(text, skipChars, printAmount));
        }

        /// <summary>
        /// Pauses the typing.
        /// </summary>
        public void Pause()
        {
            CleanupCoroutine();
        }

        /// <summary>
        /// Resume the typing.
        /// </summary>
        /// <param name="printDelay">Print delay (in seconds) per character.</param>
        /// <param name="skipChars">The number of characters to be already typed initially.</param>
        /// <param name="printAmount">The amount of characters to be printed each time.</param>
        public void Resume(float printDelay = -1, int? skipChars = null, int printAmount = -1)
        {
            Resume(this.config, printDelay, skipChars ?? this.PrintedCharacters, printAmount);
        }

        /// <summary>
        /// Resume the typing.
        /// </summary>
        /// <param name="config">The alternated config. If null the <see cref="TextTyper.config"/> will be used.</param>
        /// <param name="skipChars">The number of characters to be already typed initially.</param>
        public void Resume(TextTyperConfig config, int? skipChars = null)
        {
            Resume(config, -1f, skipChars ?? this.PrintedCharacters, -1);
        }

        private void Resume(TextTyperConfig config, float printDelay, int skipChars, int printAmount)
        {
            if (skipChars >= this.taglessText.Length)
                return;

            CleanupCoroutine();

            this.defaultConfig = config ? config : this.config;
            ProcessCustomTags(printDelay > 0 ? printDelay : GetPrintDelay());

            if (skipChars < 0)
                skipChars = 0;

            if (printAmount < 1)
                printAmount = GetPrintAmount();

            this.typeTextCoroutine = StartCoroutine(ResumeTypingTextCharByChar(skipChars, printAmount));
        }

        /// <summary>
        /// Skips the typing to the end.
        /// </summary>
        public void Skip()
        {
            CleanupCoroutine();

            this.TextComponent.maxVisibleCharacters = int.MaxValue;
            UpdateMeshAndAnims();

            OnTypewritingComplete();
        }

        /// <summary>
        /// Determines whether this instance is skippable.
        /// </summary>
        /// <returns><c>true</c> if this instance is skippable; otherwise, <c>false</c>.</returns>
        public bool IsSkippable()
        {
            // For now there's no way to configure this. Just make sure it's currently typing.
            return this.IsTyping;
        }

        private void CleanupCoroutine()
        {
            if (this.typeTextCoroutine != null)
            {
                StopCoroutine(this.typeTextCoroutine);
                this.typeTextCoroutine = null;
            }
        }

        private IEnumerator TypeTextCharByChar(string text, int skipChars, int printAmount)
        {
            this.taglessText = TextTagParser.RemoveAllTags(text);
            var totalChars = this.taglessText.Length;
            int index;

            this.PrintedCharacters = skipChars;
            this.TextComponent.text = TextTagParser.RemoveCustomTags(text);

            while (this.PrintedCharacters < totalChars)
            {
                this.PrintedCharacters = Mathf.Clamp(this.PrintedCharacters + printAmount, 0, totalChars);
                index = this.PrintedCharacters - 1;

                this.TextComponent.maxVisibleCharacters = this.PrintedCharacters;
                UpdateMeshAndAnims();

                OnCharacterPrinted(this.taglessText[index].ToString());

                var delay = this.characterPrintDelays[index];

                if (this.useUnscaledTime)
                    yield return new WaitForSecondsRealtime(delay);
                else
                    yield return new WaitForSeconds(delay);
            }

            this.typeTextCoroutine = null;
            OnTypewritingComplete();
        }

        private IEnumerator ResumeTypingTextCharByChar(int skipChars, int printAmount)
        {
            var totalChars = this.taglessText.Length;
            int index;

            this.PrintedCharacters = skipChars;

            while (this.PrintedCharacters < totalChars)
            {
                this.PrintedCharacters = Mathf.Clamp(this.PrintedCharacters + printAmount, 0, totalChars);
                index = this.PrintedCharacters - 1;

                this.TextComponent.maxVisibleCharacters = this.PrintedCharacters;
                UpdateMeshAndAnims();

                OnCharacterPrinted(this.taglessText[index].ToString());

                var delay = this.characterPrintDelays[index];

                if (this.useUnscaledTime)
                    yield return new WaitForSecondsRealtime(delay);
                else
                    yield return new WaitForSeconds(delay);
            }

            this.typeTextCoroutine = null;
            OnTypewritingComplete();
        }

        private void UpdateMeshAndAnims()
        {
            // This must be done here rather than in each TextAnimation's OnTMProChanged
            // b/c we must cache mesh data for all animations before animating any of them

            // Update the text mesh data (which also causes all attached TextAnimations to cache the mesh data)
            this.TextComponent.ForceMeshUpdate();

            // Force animate calls on all TextAnimations because TMPro has reset the mesh to its base state
            // NOTE: This must happen immediately. Cannot wait until end of frame, or the base mesh will be rendered
            for (var i = 0; i < this.animations.Count; i++)
            {
                this.animations[i].AnimateAllChars();
            }
        }

        /// <summary>
        /// Calculates print delays for every visible character in the string.
        /// Processes delay tags, punctuation delays, and default delays
        /// Also processes shake and curve animations and spawns
        /// the appropriate TextAnimation components
        /// </summary>
        /// <param name="text">Full text string with tags</param>
        private void ProcessCustomTags(string text, float printDelay)
        {
            TextTagParser.CreateSymbolListFromText(text, this.textAsSymbolList);

            ProcessCustomTags(printDelay);
        }

        /// <summary>
        /// Calculates print delays for every visible character in the string.
        /// Processes shake and curve animations and spawns
        /// the appropriate TextAnimation components
        /// </summary>
        private void ProcessCustomTags(float printDelay)
        {
            this.characterPrintDelays.Clear();
            this.animations.Clear();

            var printedCharCount = 0;
            var customTagOpenIndex = 0;
            var customTagParam = string.Empty;
            var nextDelay = printDelay;
            var punctuations = GetPunctutations();
            var punctuationDelayMultiplier = GetPunctuationDelayMultiplier();

            foreach (var symbol in this.textAsSymbolList)
            {
                if (symbol.IsTag)
                {
                    // TODO - Verification that custom tags are not nested, b/c that will not be handled gracefully
                    if (symbol.Tag.TagType == TextTagParser.CustomTags.Delay)
                    {
                        if (symbol.Tag.IsClosingTag)
                        {
                            nextDelay = printDelay;
                        }
                        else
                        {
                            nextDelay = symbol.GetFloatParameter(printDelay);
                        }
                    }
                    else if (symbol.Tag.TagType == TextTagParser.CustomTags.Speed)
                    {
                        if (symbol.Tag.IsClosingTag)
                        {
                            nextDelay = printDelay;
                        }
                        else
                        {
                            var speed = symbol.GetFloatParameter(1f);

                            if (Mathf.Approximately(speed, 0f))
                            {
                                nextDelay = printDelay;
                            }
                            else if (speed < 0f)
                            {
                                nextDelay = printDelay * Mathf.Abs(speed);
                            }
                            else if (speed > 0f)
                            {
                                nextDelay = printDelay / Mathf.Abs(speed);
                            }
                            else
                            {
                                nextDelay = printDelay;
                            }
                        }
                    }
                    else if (symbol.Tag.TagType == TextTagParser.CustomTags.Anim ||
                             symbol.Tag.TagType == TextTagParser.CustomTags.Animation)
                    {
                        if (symbol.Tag.IsClosingTag)
                        {
                            // Add a TextAnimation component to process this animation
                            TextAnimation anim = null;
                            if (IsAnimationShake(customTagParam))
                            {
                                anim = this.gameObject.AddComponent<ShakeAnimation>();
                                ((ShakeAnimation)anim).LoadPreset(this.shakeLibrary, customTagParam);
                            }
                            else if (IsAnimationCurve(customTagParam))
                            {
                                anim = this.gameObject.AddComponent<CurveAnimation>();
                                ((CurveAnimation)anim).LoadPreset(this.curveLibrary, customTagParam);
                            }
                            else
                            {
                                // Could not find animation. Should we error here?
                            }

                            anim.UseUnscaledTime = this.useUnscaledTime;
                            anim.SetCharsToAnimate(customTagOpenIndex, printedCharCount - 1);
                            anim.enabled = true;
                            this.animations.Add(anim);
                        }
                        else
                        {
                            customTagOpenIndex = printedCharCount;
                            customTagParam = symbol.Tag.Parameter;
                        }
                    }
                    else
                    {
                        // Unrecognized CustomTag Type. Should we error here?
                    }
                }
                else
                {
                    printedCharCount++;

                    if (punctuations.Contains(symbol.Character))
                    {
                        this.characterPrintDelays.Add(nextDelay * punctuationDelayMultiplier);
                    }
                    else
                    {
                        this.characterPrintDelays.Add(nextDelay);
                    }
                }
            }
        }

        private bool IsAnimationShake(string animName)
        {
            return this.shakeLibrary.ContainsKey(animName);
        }

        private bool IsAnimationCurve(string animName)
        {
            return this.curveLibrary.ContainsKey(animName);
        }

        private void OnCharacterPrinted(string printedCharacter)
        {
            if (this.CharacterPrinted != null)
            {
                this.CharacterPrinted.Invoke(printedCharacter);
            }
        }

        private void OnTypewritingComplete()
        {
            if (this.PrintCompleted != null)
            {
                this.PrintCompleted.Invoke();
            }
        }

        /// <summary>
        /// Event that signals a Character has been printed to the Text component.
        /// </summary>
        [System.Serializable]
        public class CharacterPrintedEvent : UnityEvent<string>
        {
        }
    }
}
