using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Utils
{
    internal static class FontHelper
    {
        public static global::UnityEngine.Object GetTextMeshProFont(string assetBundle)
        {
            if (string.IsNullOrEmpty(assetBundle))
            {
                return null;
            }

            global::UnityEngine.Object font = null;
            string overrideFontPath = Path.Combine(Paths.GameRoot, assetBundle);
            if (File.Exists(overrideFontPath))
            {
                TranslatePlugin.logger.LogInfo($"Attempting to load TextMesh Pro font from asset bundle: {overrideFontPath}");

                AssetBundle bundle = null;
                if (UnityTypes.AssetBundle_Methods.LoadFromFile != null)
                {
                    bundle = (AssetBundle)UnityTypes.AssetBundle_Methods.LoadFromFile.Invoke(null, new object[] { overrideFontPath });
                }
                else if (UnityTypes.AssetBundle_Methods.CreateFromFile != null)
                {
                    bundle = (AssetBundle)UnityTypes.AssetBundle_Methods.CreateFromFile.Invoke(null, new object[] { overrideFontPath });
                }
                else
                {
                    TranslatePlugin.logger.LogError("Could not find an appropriate asset bundle load method while loading font: " + overrideFontPath);
                    return null;
                }

                if (bundle == null)
                {
                    TranslatePlugin.logger.LogWarning("Could not load asset bundle while loading font: " + overrideFontPath);
                    return null;
                }

                FontHelper._loadedBundles.Add(bundle);

                if (UnityTypes.TMP_FontAsset != null)
                {
                    if (UnityTypes.AssetBundle_Methods.LoadAllAssets != null)
                    {
                        var assets = (global::UnityEngine.Object[])UnityTypes.AssetBundle_Methods.LoadAllAssets.Invoke(bundle, new object[] { UnityTypes.TMP_FontAsset.UnityType });
                        font = assets?.FirstOrDefault();
                    }
                    else if (UnityTypes.AssetBundle_Methods.LoadAll != null)
                    {
                        var assets = (global::UnityEngine.Object[])UnityTypes.AssetBundle_Methods.LoadAll.Invoke(bundle, new object[] { UnityTypes.TMP_FontAsset.UnityType });
                        font = assets?.FirstOrDefault();
                    }
                }
            }
            else
            {
                TranslatePlugin.logger.LogInfo("Attempting to load TextMesh Pro font from internal Resources API: " + overrideFontPath);
                font = Resources.Load(assetBundle);
            }

            if (font != null)
            {
                var versionProperty = UnityTypes.TMP_FontAsset_Properties.Version;
                var version = (string)(versionProperty?.Get(font)) ?? "Unknown";
                TranslatePlugin.logger.LogInfo($"Loaded TextMesh Pro font uses version: {version}");
                global::UnityEngine.Object.DontDestroyOnLoad(font);
            }
            else
            {
                TranslatePlugin.logger.LogError("Could not find the TextMeshPro font asset: " + assetBundle);
            }

            return font;
        }

        public static string[] GetOSInstalledFontNames()
        {
            return Font.GetOSInstalledFontNames();
        }

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
