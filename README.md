Unity-TextTyper
=========================

TextTyper is a text typing effect component for Unity. TextTyper prints out characters one by one to a uGUI Text component. Adapted by RedBlueGames from synchrok's GitHub project (https://github.com/synchrok/TypeText).

It's easy to find other examples of Text printing components, but TextTyper provides two major differences:
* Correct wrapping as characters are printed
* Support for Rich Text Tags

**Note this verison of TextTyper requires TextMeshPro, which can be installed for free from the Package Manager in Unity 2018, or the Asset Store in older versions. If you want the version that is compatible with Unity's uGUI Text component, [this branch](https://github.com/redbluegames/unity-text-typer/tree/ugui-text-typer) or download [this release](https://github.com/redbluegames/unity-text-typer/releases/tag/v1.2).**

How To Use
--------
To start TextTyping all you need to do is add the `TextTyper` component to a unity Text widget:

1. Create a GameObject with a Unity UI Text Widget, as normal.
2. Add a TextTyper component to the GameObject.
3. Call the TextTyper component's public functions ```TypeText``` and ```Skip``` to print the desired text with the specified delay. We call these from a 3rd component we call `Talker`.

You can optionally subscribe to the `PrintCompleted` and `CharacterPrinted` [UnityEvents](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.AddListener.html) to add additional feedback like sounds and paging widgets.

Features
--------
- **Define Text Speed Per Character**: ```Mayday! <delay=0.05>S.O.S.</delay>```
- **Support for uGUI Rich Text Tags**: ```<b>,<i>,<size>,<color>,...```
- **Additional delay for punctuation**
- **Skip to end**
- **OnComplete Callback**
- **OnCharacterPrinted Callback (for audio)**

Changelog on UPM branch
--------

### 3.1.0
- Re-implement commits from upstream
- Implement pools to reduce GC calls
- `RedBlueGames.TextTyper.Examples.dll` is now excluded from the build by default
- To include it in the build, just add `ENABLE_TEXTTYPER_EXAMPLES` into `Project Settings > Player > Scripting Define Symbols`

### 3.0.0
- Re-implmenet commits from upstream
- Fix an issue that prevents speed typer to work correctly when PrintAmount is greater than the text length

### 2.3.0
- Add `PrintAmount` to the config to support printing any number of characters each time.

### 2.2.0
- The fields named `PunctuationCharacters` inside `TextTyper` and `TextTyperConfig` have been renamed to `Punctuations`. Their type has been changed to `List<string>`
- `TextSymbol` class has been moved out of `TextTagParser`
- The constructors of `TextSymbol` has been changed into `Initialize(...)` methods
- `TextTagParser` is now a static class
- The signature of `TextTagParser.CreateSymbolListFromText(...)` has been changed. It now requires a `List<TextSymbol>` parameter to reduce allocation
- `TextTagParser` now uses a pool of `TextSymbol` to reduce allocation
- Add `Pause()` and `Resume(...)` methods to `TextTyper`

### 2.1.3
- Add `TextTyperConfig` field to `TextTyper` to allow overriding default settings: `PrintDelay`, `PunctuationDelayMultiplier`, `PunctuationCharacters`
- `TextTyper.TypeText(...)` can now receive an instance of `TextTyperConfig` as an alternated config. It's only valid until the next invocation of `TypeText(...)`

### 2.1.2
- `TextTyper` now requires `TMP_Text` instead of `TextMeshProUGUI`

Screenshots
--------
![TypeText Screenshot GIF](https://github.com/redbluegames/unity-text-typer/blob/master/README-Images/ss_chat_watermarked.gif)
Image of TextTyper in Sparklite (© RedBlueGames 2016)

The Code
--------
The core of our text typer is a coroutine that’s triggered via the TypeText method. The coroutine has the following steps:
Check next character to see if it’s the start of a Rich Text tag
- If It’s a tag, parse it and apply it. Right now “applying” a tag just means modifying the print delay, but other tags could be added. Add it to a list of tags that need to be closed (because we have not yet reached the corresponding closing tag in our string). Move to next character and repeat
- If it’s not a tag, print it
- Wait for the print delay
- Check if we are complete

The tool also uses RichTextTag.cs, a class that’s used to help with parsing.
There are a few details I left out, but this should give you enough to start reading through the code if you want to know more.

How to Help
-------
The easiest way to help with the project is to just to use it! As you use it, you can submit bugs and feature requests through GitHub issues.

Contributors
-------
**Issue Resporters**
- JonathanGorr

License and Credits
-------
- **TypeText** is under MIT license. See the [LICENSE](LICENSE) file for more info.
- Typing sound effect (UITextDisplay.wav) provided by @kevinrmabie. Free for others to use, no attribution necessary.
