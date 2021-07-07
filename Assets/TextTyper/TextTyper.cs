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
        private GameObject m_gameObject;

        private readonly List<TextSymbol> symbols;

        private readonly PoolableList<TypableCharacter> charactersToType;
        private readonly PoolableList<ShakeAnimation> shakeAnimations;
        private readonly PoolableList<CurveAnimation> curveAnimations;

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
                if (!this.textComponent)
                    this.textComponent = GetComponent<TMP_Text>();

                return this.textComponent;
            }
        }

        private GameObject GameObject
        {
            get
            {
                if (!this.m_gameObject)
                    this.m_gameObject = this.gameObject;

                return this.m_gameObject;
            }
        }

        /// <summary>
        /// Gets the number of characters that have been printed.
        /// This number will be reset each time this <see cref="TextTyper"/> starts printing text.
        /// </summary>
        public int PrintedCharacters { get; private set; }

        public TextTyper()
        {
            this.symbols = new List<TextSymbol>();

            this.charactersToType = new PoolableList<TypableCharacter>(new TypableCharacterGetter());
            this.shakeAnimations = new PoolableList<ShakeAnimation>(new ShakeAnimationGetter(this));
            this.curveAnimations = new PoolableList<CurveAnimation>(new CurveAnimationGetter(this));
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

            this.defaultConfig = config ? config : this.config;
            ProcessTags(text, printDelay > 0 ? printDelay : GetPrintDelay());

            if (skipChars < 0)
                skipChars = 0;

            if (printAmount < 1)
                printAmount = GetPrintAmount();

            var textInfo = this.TextComponent.textInfo;
            textInfo.ClearMeshInfo(false);

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
            if (skipChars >= this.charactersToType.Count)
                return;

            CleanupCoroutine();

            this.defaultConfig = config ? config : this.config;
            ProcessTags(printDelay > 0 ? printDelay : GetPrintDelay());

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
            this.charactersToType.GetUnsafe(out var characters, out var totalChars);
            this.PrintedCharacters = Mathf.Clamp(skipChars, 0, totalChars);
            this.TextComponent.SetText(TextTagParser.RemoveCustomTags(text));

            while (this.PrintedCharacters < totalChars)
            {
                this.PrintedCharacters = Mathf.Clamp(this.PrintedCharacters + printAmount, 0, totalChars);
                var index = this.PrintedCharacters - 1;

                this.TextComponent.maxVisibleCharacters = this.PrintedCharacters;
                UpdateMeshAndAnims();

                var printedChar = characters[index];
                OnCharacterPrinted(printedChar.ToString());

                if (this.useUnscaledTime)
                    yield return new WaitForSecondsRealtime(printedChar.Delay);
                else
                    yield return new WaitForSeconds(printedChar.Delay);
            }

            this.typeTextCoroutine = null;
            OnTypewritingComplete();
        }

        private IEnumerator ResumeTypingTextCharByChar(int skipChars, int printAmount)
        {
            this.charactersToType.GetUnsafe(out var characters, out var totalChars);
            this.PrintedCharacters = Mathf.Clamp(skipChars, 0, totalChars);

            while (this.PrintedCharacters < totalChars)
            {
                this.PrintedCharacters = Mathf.Clamp(this.PrintedCharacters + printAmount, 0, totalChars);
                var index = this.PrintedCharacters - 1;

                this.TextComponent.maxVisibleCharacters = this.PrintedCharacters;
                UpdateMeshAndAnims();

                var printedChar = characters[index];
                OnCharacterPrinted(printedChar.ToString());

                if (this.useUnscaledTime)
                    yield return new WaitForSecondsRealtime(printedChar.Delay);
                else
                    yield return new WaitForSeconds(printedChar.Delay);
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

            this.shakeAnimations.GetUnsafe(out var shakeAnimations, out var count);

            for (var i = 0; i < count; i++)
            {
                shakeAnimations[i].AnimateAllChars();
            }

            this.curveAnimations.GetUnsafe(out var curveAnimations, out count);

            for (var i = 0; i < count; i++)
            {
                curveAnimations[i].AnimateAllChars();
            }
        }

        /// <summary>
        /// Calculates print delays for every visible character in the string.
        /// Processes delay tags, punctuation delays, and default delays
        /// Also processes shake and curve animations and spawns
        /// the appropriate TextAnimation components
        /// </summary>
        /// <param name="text">Full text string with tags</param>
        private void ProcessTags(string text, float printDelay)
        {
            TextTagParser.CreateSymbolListFromText(text, this.symbols);

            ProcessTags(printDelay);
        }

        /// <summary>
        /// Calculates print delays for every visible character in the string.
        /// Processes shake and curve animations and spawns
        /// the appropriate TextAnimation components
        /// </summary>
        private void ProcessTags(float printDelay)
        {
            this.charactersToType.ReturnAll();
            this.curveAnimations.ReturnAll<OnReturnAnimation>();
            this.shakeAnimations.ReturnAll<OnReturnAnimation>();

            var printedCharCount = 0;
            var tagOpenIndex = 0;
            var tagParam = string.Empty;
            var nextDelay = printDelay;
            var punctuations = GetPunctutations();
            var punctuationDelayMultiplier = GetPunctuationDelayMultiplier();

            foreach (var symbol in this.symbols)
            {
                if (symbol.IsTag && !symbol.IsReplacedWithSprite)
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
                            if (IsAnimationShake(tagParam))
                            {
                                anim = this.shakeAnimations.GetItem();
                                ((ShakeAnimation)anim).LoadPreset(this.shakeLibrary, tagParam);
                            }
                            else if (IsAnimationCurve(tagParam))
                            {
                                anim = this.curveAnimations.GetItem();
                                ((CurveAnimation)anim).LoadPreset(this.curveLibrary, tagParam);
                            }
                            else
                            {
                                // Could not find animation. Should we error here?
                            }

                            anim.UseUnscaledTime = this.useUnscaledTime;
                            anim.SetCharsToAnimate(tagOpenIndex, printedCharCount - 1);
                            anim.enabled = true;
                        }
                        else
                        {
                            tagOpenIndex = printedCharCount;
                            tagParam = symbol.Tag.Parameter;
                        }
                    }
                    else
                    {
                        // Tag type is likely a Unity tag, but it might be something we don't know... could error if unrecognized.
                    }
                }
                else
                {
                    printedCharCount++;

                    var characterToType = this.charactersToType.GetItem();

                    if (symbol.IsTag && symbol.IsReplacedWithSprite)
                    {
                        characterToType.InitializeAsSprite();
                    }
                    else
                    {
                        characterToType.InitializeAsCharacter(symbol.Character);
                    }

                    characterToType.Delay = nextDelay;

                    if (punctuations.Contains(symbol.Character))
                    {
                        characterToType.Delay *= punctuationDelayMultiplier;
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

        public readonly struct TypableCharacterGetter : IGetter<TypableCharacter>
        {
            public TypableCharacter Get()
            {
                return new TypableCharacter();
            }
        }

        private readonly struct OnReturnAnimation : IAction<TextAnimation>
        {
            public void Invoke(TextAnimation item)
            {
                if (item)
                    item.enabled = false;
            }
        }

        private readonly struct ShakeAnimationGetter : IGetter<ShakeAnimation>
        {
            private readonly TextTyper target;

            public ShakeAnimationGetter(TextTyper target)
            {
                this.target = target;
            }

            public ShakeAnimation Get()
            {
                return this.target.GameObject.AddComponent<ShakeAnimation>();
            }
        }

        private readonly struct CurveAnimationGetter : IGetter<CurveAnimation>
        {
            private readonly TextTyper target;

            public CurveAnimationGetter(TextTyper target)
            {
                this.target = target;
            }

            public CurveAnimation Get()
            {
                return this.target.GameObject.AddComponent<CurveAnimation>();
            }
        }
    }
}
