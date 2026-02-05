using System.Collections.Generic;

namespace GameTranslator.Patches.Translatons
{
    public class UntranslatedTextInfo
    {
        internal UntranslatedTextInfo(string untranslatedText)
        {
            UntranslatedText = untranslatedText;
            ContextBefore = new List<string>();
            ContextAfter = new List<string>();
        }

        internal UntranslatedTextInfo(string untranslatedText, List<string> contextBefore, List<string> contextAfter)
        {
            UntranslatedText = untranslatedText;
            ContextBefore = contextBefore;
            ContextAfter = contextAfter;
        }

        public List<string> ContextBefore { get; }

        public string UntranslatedText { get; internal set; }

        public List<string> ContextAfter { get; }
    }
}