using GameTranslator.Patches.Translatons.Manipulator;
using GameTranslator.Patches.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using XUnity.Common.Constants;
using XUnity.Common.Extensions;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons
{
    internal class TextTranslationInfo
    {
        public ITextComponentManipulator TextManipulator { get; set; }

        public string OriginalText { get; set; }

        public string TranslatedText { get; set; }

        public bool IsTranslated { get; set; }

        public bool IsCurrentlySettingText { get; set; }

        public bool IsKnownTextComponent { get; set; }

        public bool ShouldIgnore { get; set; }

        public bool MustIgnore { get; set; }

        public long TextVersion { get; set; }

        public long ChangeTime
        {
            get
            {
                return this.changeTime;
            }
            set
            {
                this.changeTime = value;
            }
        }

        public HashSet<string> RedirectedTranslations
        {
            get
            {
                HashSet<string> hashSet;
                if ((hashSet = this._redirectedTranslations) == null)
                {
                    hashSet = (this._redirectedTranslations = new HashSet<string>());
                }
                return hashSet;
            }
        }

        public void Init(object ui)
        {
            if (!this._initialized)
            {
                this._initialized = true;
                this.MustIgnore = false;
                this.ShouldIgnore = ui.ShouldIgnoreTextComponent();
                this.TextManipulator = ui.GetTextManipulator();
            }
        }

        public void Reset(string newText)
        {
            this.IsTranslated = false;
            this.TranslatedText = null;
            this.OriginalText = newText;
            this.ChangeTime = TextTranslate.ChangeTime;
            this.TextVersion++;
        }

        public void SetTranslatedText(string translatedText)
        {
            this.IsTranslated = true;
            this.TranslatedText = translatedText;
        }

        public void ChangeFont(object ui)
        {
            if (ui != null)
            {
                Type unityType = ui.GetUnityType();
                if (UnityTypes.Text != null && UnityTypes.Text.IsAssignableFrom(unityType))
                {
                    return;
                }
                if ((UnityTypes.TextMeshPro != null && UnityTypes.TextMeshPro.IsAssignableFrom(unityType)) || (UnityTypes.TextMeshProUGUI != null && UnityTypes.TextMeshProUGUI.IsAssignableFrom(unityType)))
                {
                    try
                    {
                        CachedProperty cachedProperty = unityType.CachedProperty("font");
                        TMP_FontAsset originalFont = (TMP_FontAsset)cachedProperty.Get(ui);

                        if (originalFont != null && !TranslatePlugin.fallbackFontTextMeshPro.Value.IsNullOrWhiteSpace())
                        {
                            if (!TextTranslationInfo._processedFonts.Contains(originalFont))
                            {
                                TextTranslationInfo._processedFonts.Add(originalFont);
                                foreach (char c in TranslatePlugin.getShouldRemoveChars())
                                {
                                    originalFont.TryRemoveCharacter((uint)c);
                                }
                            }

                            TMP_FontAsset fallbackFont = (TMP_FontAsset)FontCache.GetOrCreateFallbackFontTextMeshPro();

                            if (fallbackFont != null && !originalFont.fallbackFontAssetTable.Contains(fallbackFont))
                            {
                                originalFont.fallbackFontAssetTable.Add(fallbackFont);
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        TranslatePlugin.logger.LogWarning("There was a problem when changing the font!" + ex2.Message);
                    }
                }
            }
        }

        public void UnchangeFont(object ui)
        {
            if (ui != null)
            {
                Action<object> unfont = this._unfont;
                if (unfont != null)
                {
                    unfont(ui);
                }
                this._unfont = null;
            }
        }

        private Action<object> _unfont;

        private bool _initialized;

        private HashSet<string> _redirectedTranslations;

        public long changeTime;

        private static HashSet<TMP_FontAsset> _processedFonts = new HashSet<TMP_FontAsset>();
    }
}
