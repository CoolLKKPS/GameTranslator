using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameTranslator.Patches.Utils
{
    internal static class FontCache
    {
        public static List<global::UnityEngine.Object> GetOrCreateFallbackFontTextMeshPro()
        {
            if (!FontCache._hasReadFallbackFontTextMeshPro)
            {
                FontCache._hasReadFallbackFontTextMeshPro = true;

                try
                {
                    if (string.IsNullOrEmpty(TranslatePlugin.fallbackFontTextMeshPro.Value))
                    {
                        FontCache.FallbackFontsTextMeshPro = new List<global::UnityEngine.Object>();
                        return FontCache.FallbackFontsTextMeshPro;
                    }

                    FontCache.FallbackFontsTextMeshPro = new List<global::UnityEngine.Object>();

                    string configValue = TranslatePlugin.fallbackFontTextMeshPro.Value;

                    if (!configValue.Contains(","))
                    {
                        string fmtPath = Path.Combine(TranslatePlugin.DefaultPath, configValue.Trim());
                        if (File.Exists(fmtPath))
                        {
                            LoadFontFile(fmtPath);
                            return FontCache.FallbackFontsTextMeshPro;
                        }
                        if (Directory.Exists(fmtPath))
                        {
                            TranslatePlugin.logger.LogInfo("Loading fallback fonts from directory: " + fmtPath);
                            foreach (string filePath in Directory.GetFiles(fmtPath, "*").OrderBy(f => f))
                            {
                                LoadFontFile(filePath);
                            }
                            return FontCache.FallbackFontsTextMeshPro;
                        }
                    }

                    string[] fontPaths = configValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string fontSegment in fontPaths)
                    {
                        string trimmed = fontSegment.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;

                        string fontPath = Path.Combine(TranslatePlugin.DefaultPath, trimmed);
                        LoadFontFile(fontPath);
                    }
                }
                catch (Exception e)
                {
                    TranslatePlugin.logger.LogError("An error occurred while loading fallback fonts. Error: " + e.Message);
                }
            }
            return FontCache.FallbackFontsTextMeshPro;
        }

        private static void LoadFontFile(string fontPath)
        {
            try
            {
                var fonts = FontHelper.GetTextMeshProFonts(fontPath);
                if (fonts.Count > 0)
                {
                    FontCache.FallbackFontsTextMeshPro.AddRange(fonts);
                }
            }
            catch (Exception e) when (e.ToString().ToLowerInvariant().Contains("missing") || e.ToString().ToLowerInvariant().Contains("not found"))
            {
                TranslatePlugin.logger.LogWarning("An error occurred while loading text mesh pro fallback font. This may be due to missing font file. Error: " + e.Message);
            }
            catch (Exception e)
            {
                TranslatePlugin.logger.LogError("An error occurred while loading text mesh pro fallback font: " + fontPath + ". Error: " + e.Message);
            }
        }

        private static bool _hasReadFallbackFontTextMeshPro = false;

        private static List<global::UnityEngine.Object> FallbackFontsTextMeshPro;
    }
}
