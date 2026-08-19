# GameTranslator
A normal translate plugin similar like AutoTranslator<br>

## What does this mod do?

This provided some translate solution, you need to place your localization content according to the methods required by this plugin:<br>
- Once you run the game with this plugin, you will see this plugin create something inside your config folder, for advance user please read "GameTranslator.cfg" before using<br>
- translations folder store all language folders that user defined, default to use "Default" language folder, you need to place font files and texture folder inside the language folder<br>
- "Normal-Translate.cfg" works just like AutoTranslator, you can import some translate files content in here<br>
- "GuiText-Translate.cfg" is for GUI<br>
- "Terminal-Translate.cfg" and "InteractiveTerminalAPI-Translate.cfg" is for Lethal Company Terminal<br>
- The above translation files regex rules are independent of each other<br>
- "CMD-ZH-Translate.cfg" and "CMD-PY-Translate.cfg" can define Lethal Company Terminal shortcut commands, for example "transmit=tm" means you only just need to type "tm" and words in your terminal to send a transmit<br>

## Notice: This is not a replacement for AutoTranslator
GameTranslator doesn't mean to replace AutoTranslator, the key design is different, but the core still using AutoTranslator and XUnity.Common<br>
I don't recommand use both AutoTranslator and GameTranslator even this is technically feasible, GameTranslator already do the same things, you may have unexpected issues if you do<br>

## Support Games (Please game dev add language support naturally)
Lethal Company<br>
R.E.P.O<br>

## Contact
You can contact me on Github<br>

## Credits
[SweetFox](https://thunderstore.io/c/lethal-company/p/SweetFox) - Made this mod, check out his [Bilibili space website](https://space.bilibili.com/403741521)<br>
[chuxiaaaa](https://thunderstore.io/c/lethal-company/p/chuxiaaaa) - Made some fixes for this mod, under MIT license<br>
[Hayrizan](https://thunderstore.io/c/lethal-company/p/Hayrizan) and [bbepis](https://github.com/bbepis) - Provide [AutoTranslator and XUnity.Common](https://github.com/bbepis/XUnity.AutoTranslator) that GameTranslator can based on, under MIT license<br>
CoolLKK - Tweaks some code, create icon, rework readme and changelog<br>
