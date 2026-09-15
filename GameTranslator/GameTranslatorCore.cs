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

namespace GameTranslator
{
    public class GameTranslatorCore : MonoBehaviour
    {
        private static ConfigFile Config { get; set; }

        public static void Bootstrap(ConfigFile config, ManualLogSource log)
        {
            if (Instance != null)
            {
                return;
            }
            logger = log;
            Config = config;
            var obj = new GameObject("___GameTranslator") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(obj);
            obj.AddComponent<GameTranslatorCore>();
        }

        private void Awake()
        {
            Instance = this;
            ConfigFile();
            GameTranslator.Patches.Utils.TextureEnhancement.Initialize();
            HookingHelper.PatchAll(ImageHooks.All, false);
            HookingHelper.PatchAll(ImageHooks.Sprite, false);
            HookingHelper.PatchAll(ImageHooks.SpriteRenderer, false);
            ApplyBasicPatches();
            ApplyIMGUIPatch();
            ApplyTerminalPatch();
            ApplyInteractiveTerminalAPIPatch();
            if (replaceUnsupportedCharacters.Value)
            {
                GameTranslator.Patches.Utils.FontSupportChecker.InitializeFonts();
            }
            AsyncTranslationManager.Instance.Start();
            SceneManager.activeSceneChanged += (from, to) =>
            {
                if (showAvailableText.Value)
                {
                    logger.LogInfo($"[Scope] Active scene changed: '{to.name}' (buildIndex={to.buildIndex})");
                }
            };

            logger.LogInfo("GameTranslator is loaded");
        }

        private void Update()
        {
            try
            {
                AsyncTranslationManager.Instance.ProcessMainThreadActions();
                GameTranslator.Patches.Utils.TextureEnhancement.ProcessRefreshRequests();
                GameTranslator.Patches.Utils.TextureTranslate.FlushPendingKeys();
            }
            catch (Exception ex)
            {
                logger?.LogError("Error in GameTranslatorCore Update: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            try
            {
                AsyncTranslationManager.Instance.Stop();
            }
            catch (Exception ex)
            {
                logger?.LogError("Error in OnDestroy: " + ex.Message);
            }
            TranslateConfig.Unload();
            logger?.LogInfo("GameTranslator destroyed");
        }

        private void ConfigFile()
        {
            syncTranslationThreshold = Config.Bind<int>("ASync", "Sync Translation Threshold", 300, "Define the character threshold to not use async translation");
            showAvailableText = Config.Bind<bool>("Debug", "Show Available Text", false, "Define whether to show available text");
            showOtherDebug = Config.Bind<bool>("Debug", "Show Other Debug", false, "Define whether to show other debug");
            enableFileWatcher = Config.Bind<bool>("Debug", "Enable File Watcher", false, "Define whether to enable file system watcher for file updates");
            enablePollingCheck = Config.Bind<bool>("Debug", "Enable Polling Check", false, "Define whether to enable the 10-seconds polling fallback for file updates");
            replaceUnsupportedCharacters = Config.Bind<bool>("Debug", "Replace Unsupported Characters", false, "Define whether to replace unsupported characters with Unicode character u25A1");
            enableTypingTranslation = Config.Bind<bool>("Debug", "Enable TextWindow Typing Translation", false, "Define whether to display translated text letter-by-letter during the textwindow typing animation instead of waiting for the animation to complete");
            enableAsyncDuringTyping = Config.Bind<bool>("Debug", "Enable Async During Typing Translation", false, "Define whether to allow async translation during typing animation which terminating the animation when async translation completes");
            cacheUnmodifiedTextures = Config.Bind<bool>("Debug", "Cache Unmodified Textures", false, "Define whether to cache textures that have not been modified");
            enableTextureDumping = Config.Bind<bool>("Debug", "Enable Texture Dumping", false, "Define whether to dump original textures to disk for debug purposes");
            stabilizationMinTextLength = Config.Bind<int>("Debug", "Stabilization Min Text Length", 100, "Define minimum text length to trigger stabilization. Set to 0 to disable stabilization");
            stabilizationDelay = Config.Bind<float>("Debug", "Stabilization Delay", 0.9f, "Define delay in seconds between stabilization checks. Must be greater than 0");
            stabilizationMaxRetries = Config.Bind<int>("Debug", "Stabilization Max Retries", 60, "Define maximum retries for text stabilization safeguard. Set to 0 for unlimited retries");
            enableTerminalPatch = Config.Bind<bool>("Debug", "Enable Terminal Patch", false, "Define whether to patch Lethal Company Terminal");
            changeFont = Config.Bind<bool>("Font", "Change Font", false, "Define whether to change the font");
            enableDynamicFont = Config.Bind<bool>("Font", "Enable Dynamic Font", false, "Define whether to dynamically add missing characters to fallback fonts at runtime");
            scaleFallbackEffects = Config.Bind<bool>("Font", "Scale Fallback Effects", false, "Define whether to proportionally scale SDF effects on fallback fonts");
            fallbackEffectScale = Config.Bind<float>("Font", "Fallback Effect Scale", 1.0f, "Define the scale multiplier for fallback font SDF effects (lower = lighter effects)");
            fallbackFontTextMeshPro = Config.Bind<string>("Font", "FallbackFontTextMeshPro", "", "Define the fallback font asset bundle(s) used");
            shouldRemoveChar = Config.Bind<string>("Font", "Custom Characters", "", "Define what vanilla characters will use custom ones");
            language = Config.Bind<string>("General", "Language", "Default", "Define what language folder is used");
            shouldTranslateNormalText = Config.Bind<bool>("General", "Translate Normal Text", true, "Define whether to use Normal Translate method");
            shouldTranslateTerimal = Config.Bind<bool>("General", "Translate Terminal", false, "Define whether translate Lethal Company Terminal, Requires Enable Terminal Patch");
            shouldTranslateInteractiveTerminalAPI = Config.Bind<bool>("General", "Translate InteractiveTerminalAPI", false, "Define whether translate Lethal Company InteractiveTerminalAPI");
            TerimalCanUseShortCutOne = Config.Bind<bool>("General", "Terminal Can Use Shortcut Commands Category ZH", false, "Define whether the terminal can use category ZH shortcut commands");
            TerimalCanUseShortCutTwo = Config.Bind<bool>("General", "Terminal Can Use Shortcut Commands Category PY", false, "Define whether the terminal can use category PY shortcut commands");
            shouldTranslateGui = Config.Bind<bool>("General", "Translate Gui", false, "Define whether translate Gui");
            changeTexture = Config.Bind<bool>("Texture", "Change Texture", false, "Define whether to change the texture");
            cacheTexturesInMemory = Config.Bind<bool>("Texture", "Cache Textures In Memory", true, "Define whether to cache texture data in memory for faster loading");
            disableDuplicateTextureCheck = Config.Bind<bool>("Texture", "Disable Duplicate Texture Check", true, "Define whether to disable duplicate texture name check");
            ignoredTextureNames = Config.Bind<string>("Texture", "Ignored Texture Names", "", "Define what texture names to skip duplicate check");
            DefaultPath = Config.ConfigFilePath.Replace("GameTranslator.cfg", "translations\\" + language.Value + "\\");
            if (!Directory.Exists(DefaultPath))
            {
                logger.LogWarning("Translation path does not exist: " + DefaultPath);
                try
                {
                    Directory.CreateDirectory(DefaultPath);
                    logger.LogInfo("Created translation directory: " + DefaultPath);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to create translation directory: " + ex.Message);
                    DefaultPath = Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath), "translations", "default");
                    Directory.CreateDirectory(DefaultPath);
                    logger.LogInfo("Using fallback translation directory: " + DefaultPath);
                }
            }
            TexturesPath = DefaultPath + "Texture\\";
            if (!Directory.Exists(TexturesPath))
            {
                Directory.CreateDirectory(TexturesPath);
            }
            DumpPath = DefaultPath + "Dump\\";
            if (enableTextureDumping.Value && !Directory.Exists(DumpPath))
            {
                Directory.CreateDirectory(DumpPath);
            }
            SceneDumpPath = DumpPath + "Scene\\";
            TranslateConfig.Load();
            TranslateExtensions.Load();
        }

        private void ApplyBasicPatches()
        {
            try
            {
                logger.LogInfo("Applying basic patches...");
                var patchTypes = new Type[]
                {typeof(GameTranslator.Patches.Hooks.GameObjectHook),
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
                typeof(GameTranslator.Patches.Hooks.texture.Object_Instantiate_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Renderer_Material_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.AsyncInstantiate_Hook),
                };
                var patchNames = patchTypes.Select(t => t.Name).ToList();
                logger.LogDebug($"Found {patchNames.Count} basic patch types: {string.Join(", ", patchNames)}");
                int appliedCount = 0;
                var appliedPatches = new List<string>();
                foreach (var patchType in patchTypes)
                {
                    try
                    {
                        harmony.PatchAll(patchType);
                        appliedCount++;
                        appliedPatches.Add(patchType.Name);
                        logger.LogDebug($"Applied basic patch: {patchType.Name}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Failed to apply basic patch {patchType.Name}: {ex.Message}");
                    }
                }
                logger.LogInfo($"Basic patches applied. Successfully applied {appliedCount}/{patchTypes.Length} patches.");
                if (appliedPatches.Count > 0)
                {
                    logger.LogDebug($"Successfully applied patches: {string.Join(", ", appliedPatches)}");
                }
                if (appliedCount < patchTypes.Length)
                {
                    var failedPatches = patchNames.Except(appliedPatches).ToList();
                    logger.LogWarning($"Failed to apply {failedPatches.Count} patches: {string.Join(", ", failedPatches)}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Error applying basic patches: {ex.Message}");
            }
        }

        private void ApplyIMGUIPatch()
        {
            try
            {
                if (shouldTranslateGui != null && shouldTranslateGui.Value)
                {
                    harmony.PatchAll(typeof(GameTranslator.Patches.Hooks.GuiContentHook));
                    logger?.LogInfo("IMGUI patch applied successfully");
                }
                else
                {
                    logger?.LogInfo("IMGUI patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Error applying IMGUI patch: {ex.Message}");
            }
        }

        private void ApplyTerminalPatch()
        {
#if MANAGED
            try
            {
                if (enableTerminalPatch != null && enableTerminalPatch.Value)
                {
                    harmony.PatchAll(typeof(GameTranslator.Patches.TerminalPatch));
                    logger?.LogInfo("Terminal patch applied successfully");
                }
                else
                {
                    logger?.LogInfo("Terminal patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Error applying Terminal patch: {ex.Message}");
            }
#else
            logger?.LogInfo("Terminal patch not available");
#endif
        }

        private void ApplyInteractiveTerminalAPIPatch()
        {
#if MANAGED
            try
            {
                if (shouldTranslateInteractiveTerminalAPI != null && shouldTranslateInteractiveTerminalAPI.Value)
                {
                    GameTranslator.Patches.InteractiveTerminalAPI.InteractiveTerminalAPIPatch.Initialize(harmony);
                    logger?.LogInfo("InteractiveTerminalAPI patch applied successfully");
                }
                else
                {
                    logger?.LogInfo("InteractiveTerminalAPI patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Error applying InteractiveTerminalAPI patch: {ex.Message}");
            }
#else
            logger?.LogInfo("InteractiveTerminalAPI patch not available");
#endif
        }

        private readonly Harmony harmony = new Harmony("GameTranslator");

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

        public static bool textureEnhancement = false;

        public static bool textureEnhancementDump = false;

        public static ConfigEntry<bool> cacheTexturesInMemory;

        public static ConfigEntry<bool> disableDuplicateTextureCheck;

        public static ConfigEntry<string> ignoredTextureNames;

        internal static GameTranslatorCore Instance;

        internal static string DefaultPath;

        internal static string TexturesPath;

        internal static string DumpPath;

        internal static string SceneDumpPath;
    }
}
