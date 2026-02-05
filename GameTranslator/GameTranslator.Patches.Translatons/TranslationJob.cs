using GameTranslator.Patches.Utils;
using System.Collections.Generic;

namespace GameTranslator.Patches.Translatons
{
    public class TranslationJob
    {
        public TranslationJob(object ui, string originalText, bool saveResult, bool isTranslatable)
        {
            UI = ui;
            OriginalText = originalText;
            SaveResult = saveResult;
            IsTranslatable = isTranslatable;
            State = TranslationJobState.Pending;
            AssociatedUIs = new List<object>();
            RetryCount = 0;
            Components = new List<KeyAnd<object>>();
            TranslationResults = new HashSet<KeyAnd<InternalTranslationResult>>();
            TranslationType = TranslationType.None;
            StartVersion = TextTranslate.ChangeTime;
        }

        public object UI { get; set; }
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public bool SaveResult { get; private set; }
        public bool IsTranslatable { get; private set; }
        public TranslationJobState State { get; set; }
        public string ErrorMessage { get; set; }
        public List<object> AssociatedUIs { get; set; }
        public object TranslationInfo { get; set; }
        public NormalTextTranslator NormalText { get; set; }
        public TranslateConfig.TranslateConfigFile Config { get; set; }
        public int RetryCount { get; set; }
        public bool AllowFallback { get; set; } = true;
        public bool SaveResultGlobally
        {
            get => SaveResult;
            private set => SaveResult = value;
        }
        public List<KeyAnd<object>> Components { get; private set; }
        public HashSet<KeyAnd<InternalTranslationResult>> TranslationResults { get; private set; }
        public TranslationType TranslationType { get; set; }
        public TranslationEndpointManager Endpoint { get; internal set; }
        public UntranslatedTextInfo UntranslatedTextInfo { get; set; }
        public long StartVersion { get; set; }

        public bool ShouldPersistTranslation
        {
            get
            {
                return (TranslationType & TranslationType.Full) == TranslationType.Full;
            }
        }

        public void Associate(string originalText, object ui, object info, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config, bool saveResult, bool allowFallback)
        {
            if (!AssociatedUIs.Contains(ui))
            {
                AssociatedUIs.Add(ui);
            }

            TranslationInfo = info;
            NormalText = normalText;
            Config = config;
            AllowFallback = allowFallback;
            SaveResult = SaveResult || saveResult;

            if (ui != null && !ui.IsSpammingComponent())
            {
                var untranslatedText = new UntranslatedText(originalText, false, false, false, false, false);
                Components.Add(new KeyAnd<object>(untranslatedText, ui));
            }

            TranslationType |= TranslationType.Full;
        }
    }

    public class KeyAnd<T>
    {
        public KeyAnd(UntranslatedText key, T item)
        {
            Key = key;
            Item = item;
        }

        public UntranslatedText Key { get; set; }
        public T Item { get; set; }
    }
}