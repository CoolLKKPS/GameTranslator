using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameTranslator.Patches.Hooks.texture;
using GameTranslator.Patches.Translatons;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using XUnity.Common.Utilities;
#if IL2CPP
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
#endif

namespace GameTranslator
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
#if MANAGED
    public class TranslatePlugin : BaseUnityPlugin
#else
    public class TranslatePlugin : BasePlugin
#endif
    {
#if MANAGED
        private void Awake()
#else
        public override void Load()
#endif
        {
#if IL2CPP
            try
            {
                var interopManagerType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .FirstOrDefault(t => t.FullName == "BepInEx.Unity.IL2CPP.Il2CppInteropManager");
                if (interopManagerType != null)
                {
                    var pathProp = interopManagerType.GetProperty("IL2CPPInteropAssemblyPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (pathProp != null)
                    {
                        var path = (string)pathProp.GetValue(null);
                        XUnity.Common.Constants.Il2CppProxyAssemblies.Location = path;
                    }
                }
            }
            catch { }
#endif

            TranslatePlugin.logger = BepInEx.Logging.Logger.CreateLogSource(TranslatePlugin.PLUGIN_NAME);
            TranslatePlugin.Instance = this;

#if MANAGED
            this.gameObject.AddComponent<TranslationUpdater>();
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(this.gameObject);
#else
            ClassInjector.RegisterTypeInIl2Cpp<TranslationUpdater>();
            var updaterObj = new GameObject("GameTranslator_Updater")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(updaterObj);
            updaterObj.AddComponent<TranslationUpdater>();
#endif

            this.ConfigFile();
            HookingHelper.PatchAll(ImageHooks.All, false);
            HookingHelper.PatchAll(ImageHooks.Sprite, false);
            HookingHelper.PatchAll(ImageHooks.SpriteRenderer, false);
            this.ApplyBasicPatches();
            this.ApplyTerminalPatch();
            this.ApplyInteractiveTerminalAPIPatch();
            if (TranslatePlugin.replaceUnsupportedCharacters.Value)
            {
                GameTranslator.Patches.Utils.FontSupportChecker.InitializeFonts();
            }
            AsyncTranslationManager.Instance.Start();

#if MANAGED
            SceneManager.activeSceneChanged += (Scene from, Scene to) =>
            {
                if (TranslatePlugin.showAvailableText.Value)
                {
                    TranslatePlugin.logger.LogInfo($"[Scope] Active scene changed: '{to.name}' (buildIndex={to.buildIndex})");
                }
            };
#endif

            TranslatePlugin.logger.LogInfo("GameTranslator is loaded");
        }

#if MANAGED
        private void OnDestroy()
#else
        public override bool Unload()
#endif
        {
            try
            {
                AsyncTranslationManager.Instance.Stop();
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError("Error in OnDestroy: " + ex.Message);
            }
            TranslateConfig.Unload();
            TranslatePlugin.logger?.LogInfo("GameTranslator destroyed");
#if IL2CPP
            return true;
#endif
        }

        private class TranslationUpdater : MonoBehaviour
        {
            private void Update()
            {
                try
                {
                    AsyncTranslationManager.Instance.ProcessMainThreadActions();
#if IL2CPP
                    PollSceneChange();
#endif
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger?.LogError("Error in TranslationUpdater Update: " + ex.Message);
                }
            }

#if IL2CPP
            private int _lastSceneBuildIndex = -1;

            private void PollSceneChange()
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.buildIndex != _lastSceneBuildIndex)
                {
                    _lastSceneBuildIndex = scene.buildIndex;
                    if (TranslatePlugin.showAvailableText.Value)
                    {
                        TranslatePlugin.logger.LogInfo($"[Scope] Active scene changed: '{scene.name}' (buildIndex={scene.buildIndex})");
                    }
                }
            }
#endif
        }

        private void ConfigFile()
        {
            TranslatePlugin.syncTranslationThreshold = Config.Bind<int>("ASync", "Sync Translation Threshold", 300, "Define the character threshold to not use async translation");
            TranslatePlugin.showAvailableText = Config.Bind<bool>("Debug", "Show Available Text", false, "Define whether to show available text");
            TranslatePlugin.showOtherDebug = Config.Bind<bool>("Debug", "Show Other Debug", false, "Define whether to show other debug");
            TranslatePlugin.enableFileWatcher = Config.Bind<bool>("Debug", "Enable File Watcher", false, "If true, enable file system watcher for file updates");
            TranslatePlugin.enablePollingCheck = Config.Bind<bool>("Debug", "Enable Polling Check", false, "If true, enable the 10-seconds polling fallback for file updates");
            TranslatePlugin.replaceUnsupportedCharacters = Config.Bind<bool>("Debug", "Replace Unsupported Characters", false, "Define whether to replace unsupported characters with Unicode character u25A1");
            TranslatePlugin.enableTypingTranslation = Config.Bind<bool>("Debug", "Enable TextWindow Typing Translation", false, "Define whether to display translated text letter-by-letter during the textwindow typing animation instead of waiting for the animation to complete");
            TranslatePlugin.enableAsyncDuringTyping = Config.Bind<bool>("Debug", "Enable Async During Typing Translation", false, "Define whether to allow async translation during typing animation which terminating the animation when async translation completes");
            TranslatePlugin.cacheUnmodifiedTextures = Config.Bind<bool>("Debug", "Cache Unmodified Textures", false, "Define whether to cache textures that have not been modified");
            TranslatePlugin.enableTextureDumping = Config.Bind<bool>("Debug", "Enable Texture Dumping", false, "Define whether to dump original textures to disk for debug purposes");
            TranslatePlugin.stabilizationMinTextLength = Config.Bind<int>("Debug", "Stabilization Min Text Length", 100, "Define minimum text length to trigger stabilization. Set to 0 to disable stabilization");
            TranslatePlugin.stabilizationDelay = Config.Bind<float>("Debug", "Stabilization Delay", 0.9f, "Define delay in seconds between stabilization checks. Must be greater than 0");
            TranslatePlugin.stabilizationMaxRetries = Config.Bind<int>("Debug", "Stabilization Max Retries", 60, "Define maximum retries for text stabilization safeguard. Set to 0 for unlimited retries");
            TranslatePlugin.enableTerminalPatch = Config.Bind<bool>("Debug", "Enable Terminal Patch", true, "Define whether to patch Terminal");
            TranslatePlugin.changeFont = Config.Bind<bool>("Font", "Change Font", false, "Define whether to change the font");
            TranslatePlugin.enableDynamicFont = Config.Bind<bool>("Font", "Enable Dynamic Font", false, "Define whether to dynamically add missing characters to fallback fonts at runtime");
            TranslatePlugin.scaleFallbackEffects = Config.Bind<bool>("Font", "Scale Fallback Effects", false, "Define whether to proportionally scale SDF effects on fallback fonts");
            TranslatePlugin.fallbackEffectScale = Config.Bind<float>("Font", "Fallback Effect Scale", 1.0f, "Define the scale multiplier for fallback font SDF effects (lower = lighter effects)");
            TranslatePlugin.fallbackFontTextMeshPro = Config.Bind<string>("Font", "FallbackFontTextMeshPro", "", "Define the fallback font asset bundle(s) used");
            TranslatePlugin.shouldRemoveChar = Config.Bind<string>("Font", "Custom Characters", "", "Define what vanilla characters will use custom ones");
            TranslatePlugin.language = Config.Bind<string>("General", "Language", "Default", "Define what language folder is used");
            TranslatePlugin.shouldTranslateNormalText = Config.Bind<bool>("General", "Translate Normal Text", true, "Define whether to use Normal Translate method");
            TranslatePlugin.shouldTranslateTerimal = Config.Bind<bool>("General", "Translate Terminal", false, "Define whether translate Terminal");
            TranslatePlugin.shouldTranslateInteractiveTerminalAPI = Config.Bind<bool>("General", "Translate InteractiveTerminalAPI", false, "Define whether translate InteractiveTerminalAPI");
            TranslatePlugin.TerimalCanUseShortCutOne = Config.Bind<bool>("General", "Terminal Can Use Shortcut Commands Category ZH", false, "Define whether the terminal can use category ZH shortcut commands");
            TranslatePlugin.TerimalCanUseShortCutTwo = Config.Bind<bool>("General", "Terminal Can Use Shortcut Commands Category PY", false, "Define whether the terminal can use category PY shortcut commands");
            TranslatePlugin.shouldTranslateGui = Config.Bind<bool>("General", "Translate Gui", false, "Define whether translate Gui");
            TranslatePlugin.changeTexture = Config.Bind<bool>("Texture", "Change Texture", false, "Define whether to change the texture");
            TranslatePlugin.cacheTexturesInMemory = Config.Bind<bool>("Texture", "Cache Textures In Memory", true, "Define whether to cache texture data in memory for faster loading");
            TranslatePlugin.disableDuplicateTextureCheck = Config.Bind<bool>("Texture", "Disable Duplicate Texture Check", true, "Define whether to disable duplicate texture name check");
            TranslatePlugin.ignoredTextureNames = Config.Bind<string>("Texture", "Ignored Texture Names", "", "Define what texture names to skip duplicate check");
            TranslatePlugin.DefaultPath = Config.ConfigFilePath.Replace("GameTranslator.cfg", "translations\\" + TranslatePlugin.language.Value + "\\");
            if (!Directory.Exists(TranslatePlugin.DefaultPath))
            {
                TranslatePlugin.logger.LogWarning("Translation path does not exist: " + TranslatePlugin.DefaultPath);
                try
                {
                    Directory.CreateDirectory(TranslatePlugin.DefaultPath);
                    TranslatePlugin.logger.LogInfo("Created translation directory: " + TranslatePlugin.DefaultPath);
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError("Failed to create translation directory: " + ex.Message);
                    TranslatePlugin.DefaultPath = Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), "translations", "default");
                    Directory.CreateDirectory(TranslatePlugin.DefaultPath);
                    TranslatePlugin.logger.LogInfo("Using fallback translation directory: " + TranslatePlugin.DefaultPath);
                }
            }
            TranslatePlugin.TexturesPath = TranslatePlugin.DefaultPath + "Texture\\";
            if (!Directory.Exists(TranslatePlugin.TexturesPath))
            {
                Directory.CreateDirectory(TranslatePlugin.TexturesPath);
            }
            TranslatePlugin.DumpPath = TranslatePlugin.DefaultPath + "Dump\\";
            if (TranslatePlugin.enableTextureDumping.Value && !Directory.Exists(TranslatePlugin.DumpPath))
            {
                Directory.CreateDirectory(TranslatePlugin.DumpPath);
            }
            TranslateConfig.Load();
            TranslateExtensions.Load();
        }

        private void ApplyBasicPatches()
        {
            try
            {
                TranslatePlugin.logger.LogInfo("Applying basic patches...");
                var patchTypes = new Type[]
                {typeof(GameTranslator.Patches.Hooks.GameObjectHook),
                typeof(GameTranslator.Patches.Hooks.GuiContentHook),
                typeof(GameTranslator.Patches.Hooks.TeshMeshProHook),
                typeof(GameTranslator.Patches.Hooks.TeshMeshProUGUIHook),
                typeof(GameTranslator.Patches.Hooks.TextHook),
                typeof(GameTranslator.Patches.Hooks.TextMeshHook),
                typeof(GameTranslator.Patches.Hooks.TMP_FallbackMaterialHook),
                typeof(GameTranslator.Patches.Hooks.TMP_FontAsset_FontFeaturesHook),
                typeof(GameTranslator.Patches.Hooks.TMP_FallbackMaterialHook_AtlasIndexRegister),
                typeof(GameTranslator.Patches.Hooks.TMP_FontAssetHook),
                typeof(GameTranslator.Patches.Hooks.TMP_GetTextElementHook),
                typeof(GameTranslator.Patches.Hooks.TMP_TextHook),
                typeof(GameTranslator.Patches.Hooks.TextElement_text_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Texture2DHook),
                };
                var patchNames = patchTypes.Select(t => t.Name).ToList();
                TranslatePlugin.logger.LogDebug($"Found {patchNames.Count} basic patch types: {string.Join(", ", patchNames)}");
                int appliedCount = 0;
                var appliedPatches = new List<string>();
                foreach (var patchType in patchTypes)
                {
                    try
                    {
                        this.harmony.PatchAll(patchType);
                        appliedCount++;
                        appliedPatches.Add(patchType.Name);
                        TranslatePlugin.logger.LogDebug($"Applied basic patch: {patchType.Name}");
                    }
                    catch (Exception ex)
                    {
                        TranslatePlugin.logger.LogWarning($"Failed to apply basic patch {patchType.Name}: {ex.Message}");
                    }
                }
                TranslatePlugin.logger.LogInfo($"Basic patches applied. Successfully applied {appliedCount}/{patchTypes.Length} patches.");
                if (appliedPatches.Count > 0)
                {
                    TranslatePlugin.logger.LogDebug($"Successfully applied patches: {string.Join(", ", appliedPatches)}");
                }
                if (appliedCount < patchTypes.Length)
                {
                    var failedPatches = patchNames.Except(appliedPatches).ToList();
                    TranslatePlugin.logger.LogWarning($"Failed to apply {failedPatches.Count} patches: {string.Join(", ", failedPatches)}");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogWarning($"Error applying basic patches: {ex.Message}");
            }
        }

        private void ApplyTerminalPatch()
        {
            try
            {
                if (TranslatePlugin.enableTerminalPatch != null && TranslatePlugin.enableTerminalPatch.Value)
                {
#if MANAGED
                    this.harmony.PatchAll(typeof(GameTranslator.Patches.TerminalPatch));
                    TranslatePlugin.logger?.LogInfo("Terminal patch applied successfully");
#else
                    TranslatePlugin.logger?.LogInfo("Terminal patch not available on IL2CPP");
#endif
                }
                else
                {
                    TranslatePlugin.logger?.LogInfo("Terminal patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogWarning($"Error applying Terminal patch: {ex.Message}");
            }
        }

        private void ApplyInteractiveTerminalAPIPatch()
        {
            try
            {
                if (TranslatePlugin.shouldTranslateInteractiveTerminalAPI != null && TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
                {
#if MANAGED
                    GameTranslator.Patches.InteractiveTerminalAPI.InteractiveTerminalAPIPatch.Initialize(this.harmony);
                    TranslatePlugin.logger?.LogInfo("InteractiveTerminalAPI patch applied successfully");
#else
                    TranslatePlugin.logger?.LogInfo("InteractiveTerminalAPI patch not available on IL2CPP");
#endif
                }
                else
                {
                    TranslatePlugin.logger?.LogInfo("InteractiveTerminalAPI patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogWarning($"Error applying InteractiveTerminalAPI patch: {ex.Message}");
            }
        }

        private readonly Harmony harmony = new Harmony("GameTranslator");

        private const string PLUGIN_GUID = "GameTranslator";

        internal const string PLUGIN_NAME = "GameTranslator";

        internal const string PLUGIN_VERSION = "2.2.9";

        internal const string PLUGIN_VERSION_FULL = PLUGIN_VERSION + ".0";

        public static ManualLogSource logger;

        public static ConfigEntry<int> syncTranslationThreshold;

        public static ConfigEntry<bool> showAvailableText;

        public static ConfigEntry<bool> showOtherDebug;

        public static ConfigEntry<bool> enableFileWatcher;

        public static ConfigEntry<bool> enablePollingCheck;

        public static ConfigEntry<bool> replaceUnsupportedCharacters;

        public static ConfigEntry<bool> enableTypingTranslation;

        public static ConfigEntry<bool> enableAsyncDuringTyping;

        public static ConfigEntry<bool> cacheUnmodifiedTextures;

        public static ConfigEntry<bool> enableTextureDumping;

        public static ConfigEntry<int> stabilizationMinTextLength;

        public static ConfigEntry<float> stabilizationDelay;

        public static ConfigEntry<int> stabilizationMaxRetries;

        public static ConfigEntry<bool> enableTerminalPatch;

        public static ConfigEntry<bool> changeFont;

        public static ConfigEntry<bool> enableDynamicFont;

        public static ConfigEntry<bool> scaleFallbackEffects;

        public static ConfigEntry<float> fallbackEffectScale;

        public static ConfigEntry<string> fallbackFontTextMeshPro;

        public static ConfigEntry<string> shouldRemoveChar;

        public static ConfigEntry<string> language;

        public static ConfigEntry<bool> shouldTranslateNormalText;

        public static ConfigEntry<bool> shouldTranslateTerimal;

        public static ConfigEntry<bool> shouldTranslateInteractiveTerminalAPI;

        public static ConfigEntry<bool> TerimalCanUseShortCutOne;

        public static ConfigEntry<bool> TerimalCanUseShortCutTwo;

        public static ConfigEntry<bool> shouldTranslateGui;

        public static ConfigEntry<bool> changeTexture;

        public static ConfigEntry<bool> cacheTexturesInMemory;

        public static ConfigEntry<bool> disableDuplicateTextureCheck;

        public static ConfigEntry<string> ignoredTextureNames;

        internal static TranslatePlugin Instance;

        internal static string DefaultPath;

        internal static string TexturesPath;

        internal static string DumpPath;
    }
}
