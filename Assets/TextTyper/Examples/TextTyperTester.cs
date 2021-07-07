namespace RedBlueGames.Tools.TextTyper
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.UI;

    /// <summary>
    /// Class that tests TextTyper and shows how to interface with it.
    /// </summary>
    public class TextTyperTester : MonoBehaviour
    {
        [SerializeField]
        private AudioClip printSoundEffect = null;

        [Header("UI References")]

        [SerializeField]
        private Button printNextButton = null;

        [SerializeField]
        private Button printNoSkipButton = null;

        private readonly Queue<string> dialogueLines = new Queue<string>();

        [SerializeField]
        [Tooltip("The text typer element to test typing with")]
        private TextTyper testTextTyper = null;

        [SerializeField]
        private TextTyperConfig speedTyperConfig = null;

        private bool canSpeedUp;

        public void Start()
        {
            this.testTextTyper.PrintCompleted.AddListener(this.HandlePrintCompleted);
            this.testTextTyper.CharacterPrinted.AddListener(this.HandleCharacterPrinted);

            this.printNextButton.onClick.AddListener(this.HandlePrintNextClicked);
            this.printNoSkipButton.onClick.AddListener(this.HandlePrintNoSkipClicked);

            this.dialogueLines.Enqueue("Hello! My name is... <delay=0.5>NPC</delay>. Got it, <i><speed=-10>bub</speed></i>?");
            this.dialogueLines.Enqueue("You can <b>use</b> <i>uGUI</i> <size=40>text</size> <size=20>tag</size> and <color=#ff0000ff>color</color> tag <color=#00ff00ff>like this</color>.");
            this.dialogueLines.Enqueue("...");
            this.dialogueLines.Enqueue("bold <b>text</b> test <b>bold</b> text <b>test</b>");
            this.dialogueLines.Enqueue("Sprites!<sprite index=0><sprite index=1><sprite index=2><sprite index=3>Isn't that neat?");
            this.dialogueLines.Enqueue("You can <size=40>size 40</size> and <size=20>size 20</size>");
            this.dialogueLines.Enqueue("You can <color=#ff0000ff>color</color> tag <color=#00ff00ff>like this</color>.");
            this.dialogueLines.Enqueue("Sample Shake Animations: <anim=lightrot>Light Rotation</anim>, <anim=lightpos>Light Position</anim>, <anim=fullshake>Full Shake</anim>\nSample Curve Animations: <animation=slowsine>Slow Sine</animation>, <animation=bounce>Bounce Bounce</animation>, <animation=crazyflip>Crazy Flip</animation>");

            ShowScript();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var tag = RichTextTag.ParseNext("blah<color=red>boo</color");
                LogTag(tag);
                tag = RichTextTag.ParseNext("<color=blue>blue</color");
                LogTag(tag);
                tag = RichTextTag.ParseNext("No tag in here");
                LogTag(tag);
                tag = RichTextTag.ParseNext("No <color=blueblue</color tag here either");
                LogTag(tag);
                tag = RichTextTag.ParseNext("This tag is a closing tag </bold>");
                LogTag(tag);
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                SpeedUp();
            }
            else
            {
                StopSpeedUp();
            }
        }

        private void HandlePrintNextClicked()
        {
            if (this.testTextTyper.IsSkippable() && this.testTextTyper.IsTyping)
            {
                this.testTextTyper.Skip();
            }
            else
            {
                ShowScript();
            }
        }

        private void HandlePrintNoSkipClicked()
        {
            ShowScript();
        }

        private void SpeedUp()
        {
            if (this.canSpeedUp)
                return;

            if (this.testTextTyper.IsTyping)
            {
                this.testTextTyper.Pause();
                this.testTextTyper.Resume(this.speedTyperConfig);
            }
            else
            {
                ShowScript(this.speedTyperConfig);
            }

            this.canSpeedUp = true;
        }

        private void StopSpeedUp()
        {
            if (!this.canSpeedUp)
                return;

            if (this.testTextTyper.IsTyping)
            {
                this.testTextTyper.Pause();
                this.testTextTyper.Resume();
            }

            this.canSpeedUp = false;
        }

        private void ShowScript(TextTyperConfig config = null)
        {
            if (this.dialogueLines.Count <= 0)
            {
                return;
            }

            this.testTextTyper.TypeText(this.dialogueLines.Dequeue(), config);
        }

        private void LogTag(RichTextTag tag)
        {
            if (tag != null)
            {
                Debug.Log("Tag: " + tag.ToString());
            }
        }

        private void HandleCharacterPrinted(string printedCharacter)
        {
            // Do not play a sound for whitespace
            if (printedCharacter == " " || printedCharacter == "\n")
            {
                return;
            }

            var audioSource = this.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = this.gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = this.printSoundEffect;
            audioSource.Play();
        }

        private void HandlePrintCompleted()
        {
            this.canSpeedUp = false;
            Debug.Log("TypeText Complete");
        }
    }
}