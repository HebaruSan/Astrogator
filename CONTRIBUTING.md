# Contributing to Astrogator

Astrogator welcomes contributions from anyone in the form of translations, problem reports, enhancement suggestions, and code changes. To make them as useful as possible, it's necessary to spell out some expectations.

## Translating to your language

![Languages supported by KSP 1.3: English, Spanish, Chinese, Russian, Japanese](https://i.imgur.com/DbCCJWK.png)

The 1.3 release of KSP introduces localization, which allows in-game text to be translated to other languages. This allows more people to enjoy the game in their preferred language and enlarges the community. However, it does not happen automatically for mods; by default, a mod will appear in English regardless of the language of the base game. In order to have both the base game and mods available in the same non-English languages, some additional work must be done by the modder.

Unfortunately, I only speak English, and I maintain this mod for free. This means I cannot create my own translations, and I cannot pay a professional translation service to produce high quality translations. The best I can do on my own is to use Google Translate, which is of dubious value for the terse, idiomatic strings needed in a KSP mod's UI. Instead, I must rely on the expertise of you, the multilingual KSP mod user, to tell me what good translations look like for your language. If you would like to help in this effort, please keep reading to learn how the mod's language files are structured and how to submit translations for use by others.

Note: Even though you will appear to be editing the project's files, don't worry about making mistakes. Github will keep your changes separate from the main files until I have verified that they are OK to use. It is even possible for me to ask questions or request changes before your work is committed to the main files.

### Creating or editing a translation

It is recommended to make your changes on your own computer at first so you can test them before uploading, especially if you are creating a new translation from scratch.

1. Install the [current release of Astrogator](https://github.com/HebaruSan/Astrogator/releases/latest) if you have not already
2. Open your `Kerbal Space Program/GameData/Astrogator/lang` folder on your local disk
3. Look for a file called *lang*.cfg, where *lang* is KSP's name for your locale; as of KSP 1.3, this includes:
    - en-us (English)
    - es-es (Spanish)
    - ja (Japanese)
    - ru (Russian)
    - zh-cn (Chinese)

The remaining steps are different depending on whether the file already exists:

####  If the file exists

Follow these steps to make improvements to an existing translation:

4. Edit the file for your language in your favorite text editor
5. Make the changes you wish to see in-game (see the [File format section](#file-format) below for details)
6. Save your changes
7. Remember to [test your changes](#testing)!

#### If the file does not exist

Follow these steps to start your own translation from scratch:

4. Make a copy of `en-us.cfg` in the `lang` folder
5. Rename the file according to the list of languages above
6. Edit the file for your language in your favorite text editor
7. Change the third line from `en-us` to the string for your language
8. Translate each string from English to your language (see the [File format section](#file-format) below for details)
9. Save your changes
10. Remember to [test your changes](#testing)!

#### File format

The middle part of the `cfg` file contains the strings to translate. The format is `name = translation`, where the name is a special string defined by the mod. For example:

    astrogator_launchSubtitle = Transfers from <<1>>\n(Launch ~<<2>>)

Do **not** change the part to the left of the equals sign ("=")! These names must be the same in every language file.

The part to the right of the equals sign is the string to be used in-game. Most of the text will be shown as-is, but it can contain a few special strings as shown in the [Lingoona grammar module demo](http://lingoona.com/cgi-bin/grammar#l=en):

| String | Purpose |
| --- | --- |
| \n | Line break; try to preserve these based on the original strings to make sure the strings will fit |
| <<1>> | The first substitutable token in the string, will be replaced by a number, name of a planet, etc., depending on the string |
| <<2>> | Second token, and so on |
| <<A:1>> | The first token, but substituted with a proper article |

For example, this is a possible translation of the above line into Spanish, courtesy of Google Translate:

    astrogator_launchSubtitle = Transferencias desde <<1>>\n(Lanzamiento ~<<2>>)

#### Testing

It's important to make sure that your changes work correctly. If you use Steam:

1. [Select the language to use in Steam](https://www.youtube.com/watch?v=iBwYCvQxfeI)
2. Wait for the language pack download to complete
3. Run KSP
4. Use Astrogator and make sure your changes appear as you intended

If you do not use Steam, I don't know the steps to choose a language. Contact SQUAD if you can't figure it out.

### Contributing your translation for others to use

After you have prepared a `cfg` file for your language and confirmed that it works as you intend, if you are willing to contribute it for redistribution under the GLPv3 license, follow these steps to upload it for inclusion in the main mod distribution:

1. Log in to [Github](https://github.com); you may need to register an account if you do not already have one
2. Navigate to the [lang folder](https://github.com/HebaruSan/Astrogator/tree/master/assets/lang)
3. Look for the file you edited

The remaining steps are different depending on whether the file already exists:

#### If the file exists

4. Click the file's name to view it
5. Click the [pencil icon](https://help.github.com/assets/images/help/repository/edit-file-edit-button.png) to edit
6. Replace the text with the pasted contents of the file you edited locally
7. **Important**: At the bottom of the page, under Propose file change, type an English description of the changes you have made and the reason you think they should be made. This will help me to confirm that your changes are appropriate. Remember, I do not speak the language in the `cfg` file, so I need you to tell me why your way is better!
6. Click `Propose file change` at the bottom when done

#### If the file does not exist

4. Click [Create new file](https://help.github.com/assets/images/help/repository/create_new_file.png) to create it
5. Enter the correct file name in the box at the top
6. Paste the contents of the file you edited locally into the big box in the middle
8. Click `Propose new file` at the bottom when done

#### Review

Once you finish your changes, Github will send me a notification that a pull request has been submitted. I will take a look at it within a day or two and attempt to verify that the changes make sense by:

- Confirming that the file name and the third line of the file match one of the supported locale names
- Viewing each change string in-game
- Checking Google Translate
- Asking individual human experts
- Requesting help on the KSP forum

If I have any questions about specific changes you've made, I will add them to the pull request, which should trigger a notification to you. Please try to respond to these in as timely a manner as you can manage. Your pull request may be closed without merging if you do not reply for a long time.

Once all the questions and comments are resolved to my satisfaction, your changes will be merged into the main files and included in the next release. I will also add your Github name to the Acknowledgements section of the README file.

## [Issues](https://github.com/HebaruSan/Astrogator/issues)

Please review these guidelines before submitting an issue! **Non-compliant issues may be closed without comment.**

- Install Astrogator and try it! Idle questions that clearly have not been tested will be [closed with the `invalid` label](https://github.com/HebaruSan/Astrogator/issues?q=is%3Aissue+label%3Ainvalid+is%3Aclosed) and linked here for all to see.
- Submit problem reports via the Github [issue system](https://github.com/HebaruSan/Astrogator/issues) for the project.
- Describe the problem as clearly, specifically, and concisely as you can. Click-by-click numbered steps are wonderful for this! The level of detail should be such that I can open the plug-in myself and see the same problem you're seeing by following your steps.
- If there is a visual component to the problem you're reporting (i.e. something doesn't look right), capture a screenshot illustrating the problem and attach it to your issue. You can press F1 in-game to generate a screenshot in your `Kerbal Space Program/Screenshots` folder, or F12 if you have Steam (Steam screenshots are a bit harder to find on disk, so you may want to upload them through Steam as well). Then click the link under the issue text box to attach them to your issue.
- If there is a functional component to the problem you're reporting (i.e., it should do X but it does Y instead), attach your KSP.log file. If you are using a build without debugging messages, switch to one with debugging messages and re-capture a log file before submitting. (As of v0.5.1, all downloads ship with debugging messages turned on. They will be turned off when I determine it's complete and stable enough for a v1.0.0 release, assuming I can find a good way to provide optional debug builds on the side as well.)

If you think you've seen a problem, but you're not sure or don't know enough to describe it, consider chatting with other users on the [KSP forum thread](http://forum.kerbalspaceprogram.com/index.php?/topic/155998-122-astrogator-v051/). They may be able to help you collect enough information.

## [Enhancement suggestions](https://github.com/HebaruSan/Astrogator/issues)

Please follow these guidelines for submitting ideas for features.

- Install Astrogator and try it! Suggestions that clearly have not been tested will be [closed with the `invalid` label](https://github.com/HebaruSan/Astrogator/issues?q=is%3Aissue+label%3Ainvalid+is%3Aclosed) and linked here for all to see.
- Submit ideas for new features via the Github [issue system](https://github.com/HebaruSan/Astrogator/issues) for the project
- Before submitting, check both the [open and closed issues list](https://github.com/HebaruSan/Astrogator/issues?utf8=%E2%9C%93&q=is%3Aissue%20) to see whether the idea has already come up
- A mock-up screenshot is extremely helpful, both in making your suggestion clear, and in selling it as a good idea
- If you know of a mod that does something similar, provide a link; a reference implementation can be very helpful if your feature ends up getting worked on

Even if your suggestion is valid, specific, and clear, I may still decide at my sole discretion that it's not the direction I want to take this mod and close it anyway. Please understand that this likely has more to do with my vision for Astrogator than with your idea. However, if you feel motivated enough, the license (GPLv3) permits you to create and distribute your own forked version with whatever changes you like. If you decide to do this, please read the section on [forking](#Forks).

## [Pull requests](https://github.com/HebaruSan/Astrogator/pulls)

Any proposed code changes are welcome, but will have to meet my personal coding standards to be merged. Pull requests may be deferred until these requirements are satisfied.

- Submissions must be licensed under GPLv3. Do not submit code that you do not have the right to submit (e.g., from mods with All Rights Reserved licenses).
- Spell all words correctly, in code, comments, and especially in user-visible strings
- Add comments to explain anything subtle, odd, or surprising ("The API requires this value to be in X format")
- Omit comments for anything obvious or self-evident ("Declare int variables")
- The code should compile with no errors or warnings on the strictest warning level of the [Mono C# compiler](http://www.mono-project.com/docs/about-mono/languages/csharp/)
- Match the format of the existing code; generally this means indenting with tab characters and following [K&R style](https://en.wikipedia.org/wiki/Indent_style#K.26R_style) plus mandatory curly braces { }
- Never copy/paste whole blocks of code; if you need to use it in more than one place, create a function or class and call it from both places
- Code defensively; check all references against `null` before using them unless there's a compiler-enforced guarantee they'll never be null (e.g., assigned a `new` object in the constructor and never cleared), and check all numbers against 0 before dividing by them
- No premature optimization; if the rationale for a change is "performance," then it should be backed up with before-and-after measurements or reports from multiple users
- Minimize `public` interfaces; if the users of a class don't need to use a function, they shouldn't be able to see it
- Keep the UI simple; if we can't fit something in the main window, it may not belong in this mod
- Whenever possible, break complex physics calculations down to clear steps that a non-physics major can understand
- If a value is the same every time, it should be `const` or `static`, especially if it has to be loaded from a file
- No classes just for the sake of classes; if you're not storing instance-level data, don't make me create an object (we have several static classes in the `*Tools.cs` files that should be able to accommodate new code)

## Forks

Astrogator's license (GLPv3) permits modification of source code and redistribution. The following requests are for the sake of courtesy and not legally mandated.

My wishes vary depending on whether I'm still actively working on Astrogator. To determine whether I'm still active, I request a grace period of at least two calendar months from the time of last activity. When in doubt, ask on the forum thread (but please read at least a few of the previous pages of posts to see if there has already been such a discussion).

### If I'm still active

If I have posted on the KSP forums recently, released a new version of Astrogator recently, or otherwise made my presence known, my preferences are to keep any derivative works easily distinguishable from my version. This is especially the case if you wish to take it in a different direction than I do; your new concept should be free to grow into its own unique mod.

Please contact me to work out ways reduce confusion among users. I'll probably request that you release under a different but similar name, a favorite being "Cosmogator" due to the fun Cold War connotations.

### If I am no longer active

If I have vanished or stopped developing or supporting Astrogator, my preferences are to avoid "X Continued" naming schemes and to have the option to pick up where you left off if I do come back, especially if your changes are only for the sake of compatibility with new versions of KSP. This implies continuity in naming.

Please do **not** change the name. Cooperate with any others who are interested in updating to avoid redundant efforts, then continue releasing new versions of Astrogator from your fork according to [Semantic Versioning](http://semver.org).
