using System.Collections.Generic;
using TMPro;

namespace GameTranslator.Patches.Utils
{
    internal static class FontDynamicLoader
    {
        private static readonly HashSet<uint> _processedChars = new HashSet<uint>();
        private static readonly HashSet<TMP_FontAsset> _dynamicFonts = new HashSet<TMP_FontAsset>();
        private static bool _warnedAtlasFull;

        internal static void RegisterDynamicFont(TMP_FontAsset font)
        {
            if (font != null && font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                _dynamicFonts.Add(font);
        }

        internal static void TryAddCharacterOnDemand(uint unicode)
        {
            if (_dynamicFonts.Count == 0 || !TranslatePlugin.enableDynamicFont.Value)
                return;

            if (!_processedChars.Add(unicode))
                return;

            string charStr = char.ConvertFromUtf32((int)unicode);
            foreach (var font in _dynamicFonts)
            {
                try
                {
                    if (font.TryAddCharacters(charStr))
                        return;
                }
                catch (System.Exception ex)
                {
                    TranslatePlugin.logger.LogWarning($"[DynamicFont] Failed: {ex.Message}");
                }
            }

            if (!_warnedAtlasFull)
            {
                _warnedAtlasFull = true;
                TranslatePlugin.logger.LogWarning($"[DynamicFont] Cannot add chars, dynamic atlas is full. Consider enabling multi-atlas in Font Asset Creator or increasing atlas size.");
            }
        }
    }
}
