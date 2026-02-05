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
using XUnity.Common.Utilities;

namespace GameTranslator
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TranslatePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            TranslatePlugin.logger = base.Logger;
            TranslatePlugin.Instance = this;
            this.ConfigFile();
            HookingHelper.PatchAll(ImageHooks.All, false);
            HookingHelper.PatchAll(ImageHooks.Sprite, false);
            HookingHelper.PatchAll(ImageHooks.SpriteRenderer, false);
            this.ApplyBasicPatches();
            this.ApplyGrabbableObjectPatch();
            this.ApplyHUDManagerPatch();
            this.ApplyTerminalPatch();
            this.ApplyInteractiveTerminalAPIPatch();
            if (TranslatePlugin.replaceUnsupportedCharacters.Value)
            {
                GameTranslator.Patches.Utils.FontSupportChecker.InitializeFonts();
            }
            GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.Start();
            GameTranslatorManager.EnsureExists();
            base.Logger.LogInfo("GameTranslator is loaded");
        }

        private void ConfigFile()
        {
            TranslatePlugin.syncTranslationThreshold = base.Config.Bind<int>("ASync", "Sync Translation Threshold", 300, "Define the character threshold to not use async translation");
            TranslatePlugin.showAvailableText = base.Config.Bind<bool>("Debug", "Show Available Text", false, "Define whether to show available text");
            TranslatePlugin.showOtherDebug = base.Config.Bind<bool>("Debug", "Show Other Debug", false, "Define whether to show other debug");
            TranslatePlugin.replaceUnsupportedCharacters = base.Config.Bind<bool>("Debug", "Replace Unsupported Characters", false, "Define whether to replace unsupported characters with Unicode character u25A1");
            TranslatePlugin.cacheUnmodifiedTextures = base.Config.Bind<bool>("Debug", "Cache Unmodified Textures", false, "Define whether to cache textures that have not been modified");
            TranslatePlugin.enableGrabbableObjectPatch = base.Config.Bind<bool>("Debug", "Enable GrabbableObject Patch", true, "Define whether to patch GrabbableObject");
            TranslatePlugin.enableHUDManagerPatch = base.Config.Bind<bool>("Debug", "Enable HUDManager Patch", true, "Define whether to patch HUDManager");
            TranslatePlugin.enableTerminalPatch = base.Config.Bind<bool>("Debug", "Enable Terminal Patch", true, "Define whether to patch Terminal");
            TranslatePlugin.changeFont = base.Config.Bind<bool>("Font", "Change Font", false, "Define whether to change the font");
            TranslatePlugin.fallbackFontTextMeshPro = base.Config.Bind<string>("Font", "FallbackFontTextMeshPro", "", "Define the fallback font file used");
            TranslatePlugin.shouldRemoveChar = base.Config.Bind<string>("Font", "Custom Characters", "", "Define what vanilla characters will use custom ones");
            TranslatePlugin.language = base.Config.Bind<string>("General", "Language", "Default", "Define what language folder is used");
            TranslatePlugin.shouldTranslateNormalText = base.Config.Bind<bool>("General", "Translate Normal Text", true, "Define whether to use Normal Translate method");
            TranslatePlugin.shouldTranslateSpecialText = base.Config.Bind<bool>("General", "Translate Special Text", false, "Define whether to use SpecialText Translate method");
            TranslatePlugin.shouldTranslateTerimal = base.Config.Bind<bool>("General", "Translate Terminal", false, "Define whether translate Terminal");
            TranslatePlugin.shouldTranslateInteractiveTerminalAPI = base.Config.Bind<bool>("General", "Translate InteractiveTerminalAPI", false, "Define whether translate InteractiveTerminalAPI");
            TranslatePlugin.TerimalCanUseChinese = base.Config.Bind<bool>("General", "Terminal Can Use Non-English Shortcut Commands", false, "Define whether the terminal can use non-English shortcut commands, such as Chinese");
            TranslatePlugin.TerimalCanUsePinyinAbbreviation = base.Config.Bind<bool>("General", "Terminal Can Use Custom Shortcut Commands", false, "Define whether the terminal can use custom shortcut commands");
            TranslatePlugin.shouldTranslateGui = base.Config.Bind<bool>("General", "Translate Gui", false, "Define whether translate Gui");
            TranslatePlugin.shouldTranslateItems = base.Config.Bind<bool>("General", "Translate Items", false, "Define whether translate Items");
            TranslatePlugin.shouldTranslateHUD = base.Config.Bind<bool>("General", "Translate HUD", false, "Define whether translate HUD");
            TranslatePlugin.changeTexture = base.Config.Bind<bool>("Texture", "Change Texture", false, "Define whether to change the texture");
            TranslatePlugin.cacheTexturesInMemory = base.Config.Bind<bool>("Texture", "Cache Textures In Memory", true, "Define whether to cache texture data in memory for faster loading");
            TranslatePlugin.disableDuplicateTextureCheck = base.Config.Bind<bool>("Texture", "Disable Duplicate Texture Check", true, "Define whether to disable duplicate texture name check");
            TranslatePlugin.ignoredTextureNames = base.Config.Bind<string>("Texture", "Ignored Texture Names", "", "Define what texture names to skip duplicate check");
            TranslatePlugin.DefaultPath = base.Config.ConfigFilePath.Replace("GameTranslator.cfg", "translations\\" + TranslatePlugin.language.Value + "\\");
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
                    TranslatePlugin.DefaultPath = Path.Combine(Path.GetDirectoryName(base.Config.ConfigFilePath), "translations", "default");
                    Directory.CreateDirectory(TranslatePlugin.DefaultPath);
                    TranslatePlugin.logger.LogInfo("Using fallback translation directory: " + TranslatePlugin.DefaultPath);
                }
            }
            TranslatePlugin.TexturesPath = TranslatePlugin.DefaultPath + "Texture\\";
            if (!Directory.Exists(TranslatePlugin.TexturesPath))
            {
                Directory.CreateDirectory(TranslatePlugin.TexturesPath);
            }
            TranslateConfig.Load();
            TranslateExtensions.Load();
        }

        public static List<char> getShouldRemoveChars()
        {
            return TranslatePlugin.shouldRemoveChar.Value.ToCharArray().ToList<char>();
        }

        public static void LogInfo(string info)
        {
            if (TranslatePlugin.logger != null)
            {
                TranslatePlugin.logger.LogInfo(info);
            }
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
                typeof(GameTranslator.Patches.Hooks.TextArea2DHook),
                typeof(GameTranslator.Patches.Hooks.TextFieldHook),
                typeof(GameTranslator.Patches.Hooks.TextHook),
                typeof(GameTranslator.Patches.Hooks.TextMeshHook),
                typeof(GameTranslator.Patches.Hooks.Texture2DHook),
                typeof(GameTranslator.Patches.Hooks.TMP_FontAssetHook),
                typeof(GameTranslator.Patches.Hooks.TMP_TextHook),
                typeof(GameTranslator.Patches.Hooks.texture.CubismRenderer_MainTexture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.CubismRenderer_TryInitialize_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Cursor_SetCursor_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.DicingTextures_GetTexture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Image_material_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Image_overrideSprite_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Image_sprite_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.ImageHooks),
                typeof(GameTranslator.Patches.Hooks.texture.MaskableGraphic_OnEnable_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Material_mainTexture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.RawImage_texture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.Sprite_texture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.SpriteRenderer_sprite_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UI2DSprite_material_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UI2DSprite_sprite2D_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UIAtlas_spriteMaterial_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UIPanel_clipTexture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UIRect_OnInit_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UISprite_atlas_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UISprite_material_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UISprite_OnInit_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UITexture_mainTexture_Hook),
                typeof(GameTranslator.Patches.Hooks.texture.UITexture_material_Hook),
                typeof(GameTranslator.Patches.Translatons.AsyncTranslationManager),
                typeof(GameTranslator.Patches.Translatons.ImageTranslationInfo),
                typeof(GameTranslator.Patches.Translatons.NormalTextTranslator),
                typeof(GameTranslator.Patches.Translatons.RegexTranslation),
                typeof(GameTranslator.Patches.Translatons.RegexTranslationSplitter),
                typeof(GameTranslator.Patches.Translatons.TextTranslationInfo),
                typeof(GameTranslator.Patches.Translatons.TextureDataResult),
                typeof(GameTranslator.Patches.Translatons.TextureTranslationCache),
                typeof(GameTranslator.Patches.Translatons.TextureTranslationInfo),
                typeof(GameTranslator.Patches.Translatons.TranslatedImage),
                typeof(GameTranslator.Patches.Translatons.TranslateExtensions),
                typeof(GameTranslator.Patches.Translatons.Manipulator.DefaultTextComponentManipulator),
                typeof(GameTranslator.Patches.Translatons.Manipulator.FairyGUITextComponentManipulator),
                typeof(GameTranslator.Patches.Translatons.Manipulator.ITextComponentManipulator),
                typeof(GameTranslator.Patches.Translatons.Manipulator.TextArea2DComponentManipulator),
                typeof(GameTranslator.Patches.Translatons.Manipulator.UguiNovelTextComponentManipulator),
                typeof(GameTranslator.Patches.Utils.FontCache),
                typeof(GameTranslator.Patches.Utils.FontHelper),
                typeof(GameTranslator.Patches.Utils.FontSupportChecker),
                typeof(GameTranslator.Patches.Utils.SceneManagerLoader),
                typeof(GameTranslator.Patches.Utils.StringBuffer),
                typeof(GameTranslator.Patches.Utils.TextHelper),
                typeof(GameTranslator.Patches.Utils.TextTranslate),
                typeof(GameTranslator.Patches.Utils.TextureTranslate),
                typeof(GameTranslator.Patches.Utils.TranslationScopeHelper),
                typeof(GameTranslator.Patches.Utils.Textures.ITextureLoader),
                typeof(GameTranslator.Patches.Utils.Textures.LoadImageImageLoader),
                typeof(GameTranslator.Patches.Utils.Textures.TextureLoader),
                typeof(GameTranslator.Patches.Utils.Textures.TgaImageLoader),
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

        private void ApplyGrabbableObjectPatch()
        {
            try
            {
                if (TranslatePlugin.enableGrabbableObjectPatch != null && TranslatePlugin.enableGrabbableObjectPatch.Value)
                {
                    this.harmony.PatchAll(typeof(GameTranslator.Patches.GrabbableObjectPatcher));
                    TranslatePlugin.logger?.LogInfo("GrabbableObject patch applied successfully");
                }
                else
                {
                    TranslatePlugin.logger?.LogInfo("GrabbableObject patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogWarning($"Error applying GrabbableObject patch: {ex.Message}");
            }
        }

        private void ApplyHUDManagerPatch()
        {
            try
            {
                if (TranslatePlugin.enableHUDManagerPatch != null && TranslatePlugin.enableHUDManagerPatch.Value)
                {
                    this.harmony.PatchAll(typeof(GameTranslator.Patches.HUDManagerPatcher));
                    TranslatePlugin.logger?.LogInfo("HUDManager patch applied successfully");
                }
                else
                {
                    TranslatePlugin.logger?.LogInfo("HUDManager patch disabled by config");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogWarning($"Error applying HUDManager patch: {ex.Message}");
            }
        }

        private void ApplyTerminalPatch()
        {
            try
            {
                if (TranslatePlugin.enableTerminalPatch != null && TranslatePlugin.enableTerminalPatch.Value)
                {
                    this.harmony.PatchAll(typeof(GameTranslator.Patches.TerminalPatch));
                    TranslatePlugin.logger?.LogInfo("Terminal patch applied successfully");
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
                    this.harmony.PatchAll(typeof(GameTranslator.Patches.InteractiveTerminalAPI.InteractiveTerminalAPIPatch));
                    TranslatePlugin.logger?.LogInfo("InteractiveTerminalAPI patch applied successfully");
                    GameTranslator.Patches.InteractiveTerminalAPI.InteractiveTerminalAPIPatch.Initialize();
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

        private const string PLUGIN_NAME = "GameTranslator";

        private const string PLUGIN_VERSION = "2.0.9";

        public static bool CacheTexturesInMemory => TranslatePlugin.cacheTexturesInMemory.Value;

        public static ManualLogSource logger;

        public static ConfigEntry<int> syncTranslationThreshold;

        public static ConfigEntry<bool> showAvailableText;

        public static ConfigEntry<bool> showOtherDebug;

        public static ConfigEntry<bool> replaceUnsupportedCharacters;

        public static ConfigEntry<bool> cacheUnmodifiedTextures;

        public static ConfigEntry<bool> enableGrabbableObjectPatch;

        public static ConfigEntry<bool> enableHUDManagerPatch;

        public static ConfigEntry<bool> enableTerminalPatch;

        public static ConfigEntry<bool> changeFont;

        public static ConfigEntry<string> fallbackFontTextMeshPro;

        public static ConfigEntry<string> shouldRemoveChar;

        public static ConfigEntry<string> language;

        public static ConfigEntry<bool> shouldTranslateNormalText;

        public static ConfigEntry<bool> shouldTranslateSpecialText;

        public static ConfigEntry<bool> shouldTranslateTerimal;

        public static ConfigEntry<bool> shouldTranslateInteractiveTerminalAPI;

        public static ConfigEntry<bool> TerimalCanUseChinese;

        public static ConfigEntry<bool> TerimalCanUsePinyinAbbreviation;

        public static ConfigEntry<bool> shouldTranslateGui;

        public static ConfigEntry<bool> shouldTranslateItems;

        public static ConfigEntry<bool> shouldTranslateHUD;

        public static ConfigEntry<bool> changeTexture;

        public static ConfigEntry<bool> cacheTexturesInMemory;

        public static ConfigEntry<bool> disableDuplicateTextureCheck;

        public static ConfigEntry<string> ignoredTextureNames;

        public static ConfigEntry<bool> generateTerimalCommand;

        internal static TranslatePlugin Instance;

        public static string DefaultPath;

        public static string TexturesPath;

        public static bool shouldTranslate;

    }
}
