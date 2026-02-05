# GameTranslator

A Lethal Company translator plugin.

## What does this mod do?

This provided a more targeted translate solution for Lethal Company.
Notice: You need to place your localization content according to the methods required by this plugin.
 - Once you run the game with this plugin, you will see this plugin create something inside your config folder, for advance user please read "GameTranslator.cfg" before using
 - translations folder store all language folders that user defined, default to use "Default" language folder, you need to place font files and texture folder inside the language folder
 - "Normal-Translate.cfg" works just like AutoTranslator, you can import some translate files content in here
 - "HUD-Translate.cfg" is for HUDManager
 - "Terminal-Translate.cfg" and "GuiText-Translate.cfg" both have special use
 - "Item-Translate.cfg" will directly replace GrabbableObject name
 - "SpecialText-Translate.cfg" is for other special cases
 - The above 6 translation files regex rules are independent of each other
 - "CMD-PY-Translate.cfg" stores most terminal shortcut commands, for example "transmit=tm" means you only just need to type "tm" and words in your terminal to send a transmit
 - "CMD-ZH-Translate.cfg" are use to store some terminal shortcut commands but contain non-english characters; The actual function is still to establish a shortcut, but it is recommended to store non-English characters

## Contact

You can contact me on Github

## Disclaimer

I don't recommand use both AutoTranslator and GameTranslator even this is technically feasible.

## Credits

[SweetFox](https://thunderstore.io/c/lethal-company/p/SweetFox) - Made this mod, check out his [Bilibili space website](https://space.bilibili.com/403741521)

[chuxiaaaa](https://thunderstore.io/c/lethal-company/p/chuxiaaaa) - Made some fixes for this mod, under MIT license

[Hayrizan](https://thunderstore.io/c/lethal-company/p/Hayrizan) and [bbepis](https://github.com/bbepis) - Provide [AutoTranslator and XUnity.Common](https://github.com/bbepis/XUnity.AutoTranslator) that GameTranslator can based on, under MIT license

CoolLKK - Tweaks some code, create icon, rework readme and changelog
