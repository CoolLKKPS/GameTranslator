using System;
using System.Text.RegularExpressions;

namespace GameTranslator.Patches.Translatons
{
    public class RegexTranslationSplitter
    {
        public RegexTranslationSplitter(string key, string value)
        {
            this.Key = key;
            this.Value = value;
            if (key.StartsWith("sr:"))
            {
                key = key.Substring(3);
            }
            int num = key.IndexOf('"');
            if (num != -1)
            {
                num++;
                int num2 = key.LastIndexOf('"');
                if (num2 <= num - 1)
                {
                    throw new Exception("Regex with key: '" + this.Key + "' starts with a \" but does not end with a \".");
                }
                key = key.Substring(num, num2 - num);
            }
            if (value.StartsWith("sr:"))
            {
                value = value.Substring(3);
            }
            num = value.IndexOf('"');
            if (num != -1)
            {
                num++;
                int num3 = value.LastIndexOf('"');
                if (num3 == num - 1)
                {
                    throw new Exception("Regex with value: '" + this.Value + "' starts with a \" but does not end with a \".");
                }
                value = value.Substring(num, num3 - num);
            }
            this.CompiledRegex = new Regex(key, RegexOptions.Multiline | NormalTextTranslator.RegexCompiledSupportedFlag);
            this.Original = key;
            this.Translation = value;
        }

        public Regex CompiledRegex { get; set; }

        public string Original { get; set; }

        public string Translation { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}
