using System;
using System.IO;

namespace GameTranslator.Patches.Utils
{
    internal static class FontCache
    {
        public static object GetOrCreateFallbackFontTextMeshPro()
        {
            if (!FontCache._hasReadFallbackFontTextMeshPro)
            {
                try
                {
                    FontCache._hasReadFallbackFontTextMeshPro = true;

                    if (string.IsNullOrEmpty(TranslatePlugin.fallbackFontTextMeshPro.Value))
                    {
                        FontCache.FallbackFontTextMeshPro = null;
                        return null;
                    }

                    string fontPath = Path.Combine(TranslatePlugin.DefaultPath, TranslatePlugin.fallbackFontTextMeshPro.Value);
                    FontCache.FallbackFontTextMeshPro = FontHelper.GetTextMeshProFont(fontPath);
                }
                catch (Exception e) when (e.ToString().ToLowerInvariant().Contains("missing") || e.ToString().ToLowerInvariant().Contains("not found"))
                {
                    TranslatePlugin.logger.LogWarning("An error occurred while loading text mesh pro fallback font. This may be due to missing font file. Error: " + e.Message);
                }
                catch (Exception e)
                {
                    TranslatePlugin.logger.LogError("An error occurred while loading text mesh pro fallback font: " + TranslatePlugin.fallbackFontTextMeshPro.Value + ". Error: " + e.Message);
                }
            }
            return FontCache.FallbackFontTextMeshPro;
        }

        private static bool _hasReadFallbackFontTextMeshPro = false;

        private static global::UnityEngine.Object FallbackFontTextMeshPro;
    }
}
