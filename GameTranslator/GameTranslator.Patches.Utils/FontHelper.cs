using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Utils
{
    internal static class FontHelper
    {
        public static List<global::UnityEngine.Object> GetTextMeshProFonts(string assetBundle)
        {
            var fonts = new List<global::UnityEngine.Object>();

            if (string.IsNullOrEmpty(assetBundle))
            {
                return fonts;
            }

            string fontBundlePath = Path.Combine(Paths.GameRoot, assetBundle);
            if (File.Exists(fontBundlePath))
            {
                TranslatePlugin.logger.LogInfo($"Attempting to load TextMesh Pro font from asset bundle: {fontBundlePath}");

                AssetBundle bundle = null;
                if (UnityTypes.AssetBundle_Methods.LoadFromFile != null)
                {
                    bundle = (AssetBundle)UnityTypes.AssetBundle_Methods.LoadFromFile.Invoke(null, new object[] { fontBundlePath });
                }
                else if (UnityTypes.AssetBundle_Methods.CreateFromFile != null)
                {
                    bundle = (AssetBundle)UnityTypes.AssetBundle_Methods.CreateFromFile.Invoke(null, new object[] { fontBundlePath });
                }
                else
                {
                    TranslatePlugin.logger.LogError("Could not find an appropriate asset bundle load method while loading font: " + fontBundlePath);
                    return fonts;
                }

                if (bundle == null)
                {
                    TranslatePlugin.logger.LogWarning("Could not load asset bundle while loading font: " + fontBundlePath);
                    return fonts;
                }
                FontHelper._loadedBundles.Add(bundle);

                if (UnityTypes.TMP_FontAsset != null)
                {
                    global::UnityEngine.Object[] assets = null;
                    if (UnityTypes.AssetBundle_Methods.LoadAllAssets != null)
                    {
                        assets = (global::UnityEngine.Object[])UnityTypes.AssetBundle_Methods.LoadAllAssets.Invoke(bundle, new object[] { UnityTypes.TMP_FontAsset.UnityType });
                    }
                    else if (UnityTypes.AssetBundle_Methods.LoadAll != null)
                    {
                        assets = (global::UnityEngine.Object[])UnityTypes.AssetBundle_Methods.LoadAll.Invoke(bundle, new object[] { UnityTypes.TMP_FontAsset.UnityType });
                    }

                    if (assets != null)
                    {
                        foreach (var font in assets)
                        {
                            if (font != null)
                            {
                                var versionProperty = UnityTypes.TMP_FontAsset_Properties.Version;
                                var version = (string)(versionProperty?.Get(font)) ?? "Unknown";
                                TranslatePlugin.logger.LogInfo($"Loaded TextMesh Pro font uses version: {version}");
                                global::UnityEngine.Object.DontDestroyOnLoad(font);
                                fonts.Add(font);
                            }
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
