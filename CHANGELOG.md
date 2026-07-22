## v2.2.2

Breaking change:
- [Config] ShortCutCommand config name changed, setting will be reset to default value, but won't affect old config
- [SpecialText/HUDText/ItemText] Removed, translator should **ONLY** affect UI, and i don't want to maintain many translate modules for now
- Some fixes and code cleanup
- Rework Readme

## v2.2.1

- More code cleanup

## v2.2.0

- Code cleanup

## v2.1.7

- Added Scope feature, similar like AutoTranslator (between `#set level 2` and `#unset level 2` will only translate MainMenu)
- Fixed the issue when reloading translation files sometimes cause thread exception
- Fixed the issue when original text not equal to translated text will re-translate translated text again
- Fixed the issue where regex will do twice in certain cases
- Now same length text will sort by line
- Additional code cleanup

## v2.1.6

- Added debug config and remove hardcoded value
- Added UIElements tiny support

## v2.1.5

- ASync Translation Reliability Improve x2, for some mods similar like FinallyCorrectKeys
- Instead removing \\u200B that user can never see in UI, i more like to make more accurate translation

## v2.1.4

- ASync Translation Reliability Improve

## v2.1.3

- Finally fixed OnDestroy issue
- Async translation now will remove translation job when the GameObject is inactive
- Added more error logs

## v2.1.2

- InteractiveTerminalAPI Patch performance improve, thanks hu_luo_bo_ya!

## v2.1.1

- Fixed OnDestroy issue (Nope!)

## v2.0.9

- Removed old Unity API support
- Added some debug configs, for solving LethalPerformance filewatcher patch compatibility
- Fixed CleanupInvalidEntries, now it should no longer clean valid textures at the first time

## v2.0.8

- Rework configs, sorry but that's necessary
- Texture loading speed improve

## v2.0.7

I made a rushed update cuz i don't want to miss my friend's birthday

- Improve hooks to fix patch issues
- Create GameTranslator GameObject so i don't need to use BepInEx GameObject anymore
- Fix an issue where the translate won't reload when user changed the translate config files
- Use AutoTranslator's logic again to let it full control async logic, so i won't need to worry about async issues

## v2.0.6

- Use more difficult way to fix patch issues
- InteractiveTerminalAPI patch should work now
- Add more debug options that can let user deside to disable some patches

## v2.0.5

- Adjust config default values

## v2.0.4

I've made some changes to make translate experience more like AutoTranslator

- Add Async, failed regex cache, that should be can improve performance but still can have better response
- Fix texture loading performance when the game is loading
- Add new configs
- Add InteractiveTerminalAPI support
- Uses chuxiaaaa's `FixCharWarn` idea to fix unsupport character issue in TextMeshPro
- Deprecated OverrideTextMeshPro, use FallbackTextMeshPro instead

## v2.0.3

- Add texture cache auto cleanup feature
- Texture cache auto cleanup will trigger by memory pressure and timer, auto cleanup interval will be 5 mins if encounter memory pressure, and 30 mins interval for inactive texture cache
- Adjust cleanup interval so regex and tag cache auto cleanup only trigger every 30 minutes no matter memory pressure
- Adjust regex and tag cache capacity
- Tag protection will disabled by default now
- Fixed a bug that were made by v2.0.0, now you should be able to translate multiline with the same regex now, im so sorry!

## v2.0.2

- Tag protection should only protect whitelisted tags

## v2.0.1

- Changed how TagRegex works, a little change and try to improve performance

## v2.0.0

I've made some changes to improve sr experience and fix format exception

- The current experience will be more inclined towards the native .NET syntax
- (Breaking change) you no longer can use "$\\" to tell the plugin this is not the capture group, instead you should use "$$"
- Fixed the issue where using invalid capture group numbers would throw an exception
- Already done a lot the test, but if encounter any issues feel free to report to me

## v1.9.9

- rework NormalTranslate and TranslateConfig (does not change any config files)
- add tag cache and memorypressure detection
- increase cache limit for regex from 500 to 1000
- set cache limit for tag to 10000
- set if memory pressure reached to 100MB, will trigger cache half cleanup (not full cleanup)
- after this update, maybe i will take a bit rest, until this plugin encounter problem that i really need to take care of, but for now, i just really need some rest

## v1.9.8

- Removed AHook
- just in case for some people who don't know, AHook plugin is original designed for ReservedItem plugin due GameTranslator will replace GrabbableObject item name IF you use Item-translate method
- Fix RichText not working in some cases, protect rich text symbol markers by using 8-bit placeholders and emergency placeholders
- Try to prevent another ArgumentOutOfRangeException that were made by HUD Patch

## v1.9.7

- (Deprecated) Improved AHook

## v1.9.6

- Optimize performance

## v1.9.5

I've made some changes to change the translate experience

- (Breaking change) You no longer can use "rex", "stg" and "sm" prefix
- Now you can use "r" and "sr" to all translate files
- Removed force replace "$" to ""
- The longest field will be prioritized for matching
- Fix some issues that were made by v1.9.4 (i forgot)

## v1.9.4

I've made some changes to improve translate experience

- (Breaking change) Translate experience will be more like AutoTranslator in NormalText, so you can import your translate content to NormalText, but that will effect old translate files
- Removed force replace "^" to ""
- You now won't get exception when you use "r", "sr" in the wrong place

## v1.9.3

I've made some changes to improve texture replacement experience

- Referring to AutoTranslator's texture replacement plan, fix the issue of white spots appearing on the texture
- Fix some NRE cases, and add texture cleanup feature to fix memory issue
- But tbh that still took a bit long time

## v1.9.2

- Fix a mistake, not caused any problem, but will mess up some translation files
- Add more information in readme for beginner

## v1.9.1

- Code cleanup

## v1.9.0

I've made some changes to improve the experience

- Made the mod no longer handle the color by default
- Remove some features that changes notification and tip display theme, nope i won't add that, keep vanilla style
- Rework log output (let me know if i done something wrong)
- Add "FixGameTranslate" feature so the plugin can handle unsupport characters correctly
- Rework config, please regenerate "GameTranslator.cfg" file

## Old Version
You can visit old changelog in [Lethal_Company_Simplified_Chinese_Localization changelog](https://thunderstore.io/c/lethal-company/p/SweetFox/Lethal_Company_Simplified_Chinese_Localization/changelog).