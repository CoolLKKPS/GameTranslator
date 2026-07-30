using System.Collections.Generic;
using System.Text;
using TMPro;

namespace GameTranslator.Patches.Utils
{
    internal static class FontDynamicLoader
    {
        private static readonly HashSet<uint> _processedChars = new HashSet<uint>();
        private static readonly HashSet<TMP_FontAsset> _dynamicFonts = new HashSet<TMP_FontAsset>();
        private static readonly HashSet<TMP_FontAsset> _allFonts = new HashSet<TMP_FontAsset>();
        private static bool _warnedAtlasFull;

        internal static void RegisterDynamicFont(TMP_FontAsset font)
        {
            if (font == null) return;
            _allFonts.Add(font);
            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                _dynamicFonts.Add(font);
        }

        internal static void EnsureCharactersAvailable(string text)
        {
            if (string.IsNullOrEmpty(text) || _dynamicFonts.Count == 0)
                return;

            var missing = new StringBuilder();
            var seen = new HashSet<uint>();

            for (int i = 0; i < text.Length; i++)
            {
                uint cp = (uint)char.ConvertToUtf32(text, i);
                if (char.IsSurrogatePair(text, i)) i++;

                if (_processedChars.Contains(cp) || !seen.Add(cp))
                    continue;

                bool found = false;
                foreach (var font in _allFonts)
                {
                    if (font.HasCharacter((int)cp))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missing.Append(char.ConvertFromUtf32((int)cp));
                    _processedChars.Add(cp);
                }
            }

            if (missing.Length > 0)
            {
                string missingStr = missing.ToString();
                var canRasterize = new StringBuilder();
                for (int i = 0; i < missingStr.Length; i++)
                {
                    uint cp = (uint)char.ConvertToUtf32(missingStr, i);
                    if (char.IsSurrogatePair(missingStr, i)) i++;

                    bool supported = false;
                    foreach (var font in _dynamicFonts)
                    {
                        if (font.sourceFontFile != null && font.sourceFontFile.HasCharacter((int)cp))
                        {
                            supported = true;
                            break;
                        }
                    }
                    if (supported)
                        canRasterize.Append(char.ConvertFromUtf32((int)cp));
                }

                if (canRasterize.Length > 0)
                {
                    string toAdd = canRasterize.ToString();
                    bool added = false;
                    foreach (var font in _dynamicFonts)
                    {
                        try
                        {
                            if (font.TryAddCharacters(toAdd))
                            {
                                added = true;
                                break;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            TranslatePlugin.logger.LogWarning($"[DynamicFont] Failed: {ex.Message}");
                        }
                    }

                    if (!added && !_warnedAtlasFull)
                    {
                        _warnedAtlasFull = true;
                        TranslatePlugin.logger.LogWarning($"[DynamicFont] Cannot add chars, dynamic atlas is full. Consider enabling multi-atlas in Font Asset Creator or increasing atlas size.");
                    }
                }
            }
        }
    }
}
