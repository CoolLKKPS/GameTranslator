using System;
using System.Collections.Generic;
using System.IO;
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
                            string shaderName = font.material != null && font.material.shader != null ? font.material.shader.name : "Unknown";
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
                            TranslatePlugin.logger.LogInfo($"Loaded TextMesh Pro font '{font.name}' version={font.version}, shader={shaderName}, {atlasInfo}, pointSize={font.faceInfo.pointSize}, padding={font.atlasPadding}");
                            fonts.Add(font);
                        }
                    }
                }
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

            if (fonts.Count == 0)
            {
                TranslatePlugin.logger.LogError("Could not find any TextMeshPro font assets: " + assetBundle);
            }

            return fonts;
        }

        /*
        // I don't think i need it, just keep it maybe useful in the future
        public static string[] GetOSInstalledFontNames()
        {
            return Font.GetOSInstalledFontNames();
        }
        */

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
    }
}
