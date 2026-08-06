using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Utils
{
    internal static class FontHelper
    {
        public static List<UnityEngine.Object> GetTextMeshProFonts(string assetBundle)
        {
            var fonts = new List<UnityEngine.Object>();

            if (string.IsNullOrEmpty(assetBundle))
            {
                return fonts;
            }

            string fontBundlePath = Path.Combine(Paths.GameRoot, assetBundle);
            if (File.Exists(fontBundlePath))
            {
                TranslatePlugin.logger.LogInfo($"Attempting to load TextMesh Pro font from asset bundle: {fontBundlePath}");

                AssetBundle bundle = AssetBundle.LoadFromFile(fontBundlePath);
                if (bundle == null)
                {
                    TranslatePlugin.logger.LogWarning("Could not load asset bundle while loading font: " + fontBundlePath);
                    return fonts;
                }
                FontHelper._loadedBundles.Add(bundle);

                TMP_FontAsset[] fontAssets = bundle.LoadAllAssets<TMP_FontAsset>();
                if (fontAssets != null)
                {
                    foreach (TMP_FontAsset font in fontAssets)
                    {
                        if (font != null)
                        {
                            var mat = FontHelper.GetFontMaterial(font);
                            string shaderName = mat != null && mat.shader != null ? mat.shader.name : "Unavailable";
                            string renderMode = font.atlasRenderMode.ToString();
                            int atlasCount = font.atlasTextures != null ? font.atlasTextures.Length : 0;
                            string atlasInfo;
                            if (atlasCount > 0)
                            {
                                var dims = new System.Text.StringBuilder();
                                for (int i = 0; i < atlasCount; i++)
                                {
                                    if (font.atlasTextures[i] != null)
                                        dims.Append(font.atlasTextures[i].width + "x" + font.atlasTextures[i].height);
                                    else
                                        dims.Append("null");
                                    if (i < atlasCount - 1) dims.Append(", ");
                                }
                                atlasInfo = atlasCount + " atlas(es): " + dims;
                            }
                            else
                            {
                                atlasInfo = "0 atlas";
                            }
                            string pointSizeStr = FontHelper._getFaceInfoPointSize(font);
                            string versionStr = FontHelper._getVersion(font);
                            int atlasPaddingVal = FontHelper._getAtlasPadding(font);
                            TranslatePlugin.logger.LogDebug($"Loaded TextMesh Pro font '{font.name}' version={versionStr}, shader={shaderName}, render={renderMode}, {atlasInfo}, pointSize={pointSizeStr}, padding={atlasPaddingVal}");
                            fonts.Add(font);
                        }
                    }
                }
            }
            else
            {
                var systemFont = TryGetTextMeshProFontFromSystemFont(assetBundle);
                if (systemFont != null)
                {
                    fonts.Add(systemFont);
                }
                else
                {
                    TranslatePlugin.logger.LogInfo("Attempting to load TextMesh Pro font from internal Resources API: " + assetBundle);
                    var font = Resources.Load(assetBundle);
                    if (font != null)
                    {
                        fonts.Add(font);
                    }
                }
            }

            if (fonts.Count == 0)
            {
                TranslatePlugin.logger.LogError("Could not find any TextMeshPro font assets: " + assetBundle);
            }

            return fonts;
        }

        public static string[] GetOSInstalledFontNames()
        {
            try
            {
                return Font.GetOSInstalledFontNames();
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogWarning("Unable to retrieve OS installed fonts: " + ex.Message);
                return new string[0];
            }
        }

        public static TMP_FontAsset TryGetTextMeshProFontFromSystemFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return null;

            try
            {
                string[] systemFonts = GetOSInstalledFontNames();
                if (systemFonts.Length == 0) return null;

                bool found = false;
                for (int i = 0; i < systemFonts.Length; i++)
                {
                    if (string.Equals(systemFonts[i], fontName, StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) return null;

                if (FontHelper._createFontAssetFromStringMethod != null)
                {
                    TranslatePlugin.logger.LogInfo($"System font '{fontName}' found. Creating TextMesh Pro font asset from it.");
                    var font = (TMP_FontAsset)FontHelper._createFontAssetFromStringMethod.Invoke(null, new object[] { fontName, "", 90 });
                    if (font != null)
                    {
                        font.name = fontName;
                        TranslatePlugin.logger.LogDebug($"System font asset created: {font.name}");
                    }
                    return font;
                }

                TranslatePlugin.logger.LogWarning("System font loading not supported on this TextMeshPro version.");
                return null;
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogWarning($"Error creating font from system font '{fontName}': {ex.Message}");
                return null;
            }
        }

        // Still using for other purposes
        public static void UnloadAllBundles()
        {
            foreach (AssetBundle assetBundle in FontHelper._loadedBundles)
            {
                try
                {
                    if (assetBundle != null)
                    {
                        assetBundle.Unload(true);
                    }
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError("Error unloading bundle: " + ex.Message);
                }
            }
            FontHelper._loadedBundles.Clear();
        }

        private static readonly List<AssetBundle> _loadedBundles = new List<AssetBundle>();

        private static readonly MethodInfo _createFontAssetFromStringMethod = typeof(TMP_FontAsset).GetMethod("CreateFontAsset", new[] { typeof(string), typeof(string), typeof(int) });

        private static readonly Func<TMP_Asset, Material> _getFontMaterial = ResolvePropertyOrField<TMP_Asset, Material>("material") ?? (_ => default);

        private static readonly Func<TMP_FontAsset, string> _getFaceInfoPointSize = InitFaceInfoPointSizeAccessor();

        private static readonly Func<TMP_FontAsset, string> _getVersion = ResolvePropertyOrField<TMP_FontAsset, string>("version") ?? (_ => "?");

        private static readonly Func<TMP_FontAsset, int> _getAtlasPadding = ResolvePropertyOrField<TMP_FontAsset, int>("atlasPadding") ?? (_ => default);

        internal static Material GetFontMaterial(TMP_FontAsset font)
        {
            return _getFontMaterial(font);
        }

        private static Func<T, V> ResolvePropertyOrField<T, V>(string memberName)
        {
            var type = typeof(T);

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && typeof(V).IsAssignableFrom(prop.PropertyType))
                return obj => (V)prop.GetValue(obj, null);

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && typeof(V).IsAssignableFrom(field.FieldType))
                return obj => (V)field.GetValue(obj);

            return null;
        }

        private static Func<TMP_FontAsset, string> InitFaceInfoPointSizeAccessor()
        {
            var fiProp = typeof(TMP_FontAsset).GetProperty("faceInfo", BindingFlags.Public | BindingFlags.Instance) ?? typeof(TMP_Asset).GetProperty("faceInfo", BindingFlags.Public | BindingFlags.Instance);
            if (fiProp == null)
                return font => "?";

            var fiType = fiProp.PropertyType;
            var ptProp = fiType.GetProperty("pointSize", BindingFlags.Public | BindingFlags.Instance);
            if (ptProp == null)
                return font => "?";

            return font => { var fi = fiProp.GetValue(font, null); return fi == null ? "?" : ptProp.GetValue(fi, null)?.ToString() ?? "?"; };
        }
    }
}
