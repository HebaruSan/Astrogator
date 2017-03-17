# Contributing to Astrogator

Astrogator welcomes contributions from anyone in the form of problem reports, enhancement suggestions, and code changes. To make them as useful as possible, it's necessary to spell out some expectations.

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
