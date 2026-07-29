using System.Collections.Generic;
using System.Text;
using TMPro;

namespace GameTranslator.Patches.Utils
{
    internal static class FontDynamicLoader
    {
        private static readonly HashSet<uint> _processedChars = new HashSet<uint>();
        private static readonly HashSet<TMP_FontAsset> _dynamicFonts = new HashSet<TMP_FontAsset>();

        internal static void RegisterDynamicFont(TMP_FontAsset font)
        {
            if (font != null && font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
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
                foreach (var font in _dynamicFonts)
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
                foreach (var font in _dynamicFonts)
                {
                    try
                    {
                        bool added = font.TryAddCharacters(missingStr);
                        if (!added)
                        {
                            try
                            {
                                var multiAtlasField = typeof(TMP_FontAsset).GetField("m_IsMultiAtlasTexturesEnabled",
                                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                if (multiAtlasField != null)
                                {
                                    multiAtlasField.SetValue(font, true);
                                    added = font.TryAddCharacters(missingStr);
                                }
                            }
                            catch { }
                        }
                        if (added)
                            break;
                    }
                    catch (System.Exception ex)
                    {
                        TranslatePlugin.logger.LogWarning($"[DynamicFont] Failed: {ex.Message}");
                    }
                }
            }
        }
    }
}
