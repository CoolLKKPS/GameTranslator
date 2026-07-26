using GameTranslator.Patches.Utils;
using System.Collections.Concurrent;

namespace GameTranslator.Patches.Translatons
{
    internal class TranslationJob
    {
        public TranslationJob(object ui, string originalText, bool saveResult, bool isTranslatable)
        {
            UI = ui;
            OriginalText = originalText;
            State = TranslationJobState.Pending;
            AssociatedUIs = new ConcurrentDictionary<object, byte>();
            RetryCount = 0;
            StartVersion = TextTranslate.ChangeTime;
        }

        public object UI { get; set; }
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public TranslationJobState State { get; set; }
        public string ErrorMessage { get; set; }
        public ConcurrentDictionary<object, byte> AssociatedUIs { get; set; }
        public object TranslationInfo { get; set; }
        public NormalTextTranslator NormalText { get; set; }
        public TranslateConfig.TranslateConfigFile Config { get; set; }
        public int RetryCount { get; set; }
        public long StartVersion { get; set; }
        public int Scope { get; set; } = -1;

        public void Associate(object ui, object info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config)
        {
            AssociatedUIs.TryAdd(ui, 0);
            TranslationInfo = info;
            NormalText = normalText;
            Config = config;
        }
    }
}