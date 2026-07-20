using GameTranslator.Patches.Utils;
using System.Collections.Generic;

namespace GameTranslator.Patches.Translatons
{
    internal class TranslationJob
    {
        public TranslationJob(object ui, string originalText, bool saveResult, bool isTranslatable)
        {
            UI = ui;
            OriginalText = originalText;
            State = TranslationJobState.Pending;
            AssociatedUIs = new List<object>();
            RetryCount = 0;
            StartVersion = TextTranslate.ChangeTime;
        }

        public object UI { get; set; }
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public TranslationJobState State { get; set; }
        public string ErrorMessage { get; set; }
        public List<object> AssociatedUIs { get; set; }
        public object TranslationInfo { get; set; }
        public NormalTextTranslator NormalText { get; set; }
        public TranslateConfig.TranslateConfigFile Config { get; set; }
        public int RetryCount { get; set; }
        public long StartVersion { get; set; }
        public int Scope { get; set; } = -1;

        public void Associate(string originalText, object ui, object info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool saveResult, bool allowFallback)
        {
            if (!AssociatedUIs.Contains(ui))
            {
                AssociatedUIs.Add(ui);
            }

            TranslationInfo = info;
            NormalText = normalText;
            Config = config;
        }
    }
}