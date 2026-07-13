using BepInEx.Logging;
using GameTranslator.Patches.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GameTranslator.Patches.Translatons
{
    public class NormalTextTranslator
    {
        public NormalTextTranslator(string fileName)
        {
            try
            {
                this.FileName = fileName;
                this.FilePath = Path.Combine(TranslatePlugin.DefaultPath, fileName);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("An error occurred while initializing the translation file: " + fileName);
                TranslatePlugin.logger.LogError(ex);
            }
        }

        public void Load()
        {
            TranslatePlugin.logger.LogInfo("--- Loading " + this.FileName + " file ---");
            if (!File.Exists(this.FilePath))
            {
                TranslatePlugin.logger.LogWarning("Translation file not found: " + this.FilePath);
                return;
            }
            try
            {
                lock (this._regexLock)
                {
                    this.CleanupCurrentFileRegexCache();
                    this._defaultRegexes.Clear();
                    this._translations.Clear();
                    this._reverseTranslations.Clear();
                    this._partialTranslations.Clear();
                    this._splitterRegexes.Clear();
                    this._registeredRegexes.Clear();
                    this._registeredSplitterRegexes.Clear();
                    this._failedRegexLookups.Clear();
                    this._scopedTranslations.Clear();
                    this._tokenTranslations.Clear();
                    this._reverseTokenTranslations.Clear();
                    this.LoadTranslationsInStream(this.FilePath, true);
                    this.PrecompileAndCacheRegexes();
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("An error occurred while loading " + this.FileName + " file!");
                TranslatePlugin.logger.LogError(ex);
            }
        }

        private void LoadTranslationsInStream(string stream, bool isLoad)
        {
            if (isLoad)
            {
                TranslatePlugin.logger.LogInfo("Loading text file: " + stream + ".");
            }
            using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                var activeLevels = new HashSet<int>();
                foreach (string text in streamReader.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = text.TrimStart();
                    if (trimmed.StartsWith("//"))
                    {
                        continue;
                    }
                    if (trimmed.StartsWith("#set level "))
                    {
                        string levelsStr = trimmed.Substring(11).Trim();
                        if (int.TryParse(levelsStr, out int singleLevel) && singleLevel >= 0)
                        {
                            activeLevels.Add(singleLevel);
                            GetOrCreateScopedData(singleLevel);
                        }
                        else
                        {
                            foreach (string part in levelsStr.Split(','))
                            {
                                if (int.TryParse(part.Trim(), out int level) && level >= 0)
                                {
                                    activeLevels.Add(level);
                                    GetOrCreateScopedData(level);
                                }
                            }
                        }
                        continue;
                    }
                    if (trimmed.StartsWith("#unset level "))
                    {
                        string levelsStr = trimmed.Substring(13).Trim();
                        if (int.TryParse(levelsStr, out int singleLevel))
                        {
                            activeLevels.Remove(singleLevel);
                        }
                        else
                        {
                            foreach (string part in levelsStr.Split(','))
                            {
                                if (int.TryParse(part.Trim(), out int level))
                                {
                                    activeLevels.Remove(level);
                                }
                            }
                        }
                        continue;
                    }
                    if (trimmed.StartsWith("#"))
                    {
                        continue;
                    }
                    try
                    {
                        string[] array2 = TextHelper.ReadTranslationLineAndDecode(text);
                        if (array2 != null)
                        {
                            string text2 = array2[0];
                            string text3 = array2[1];
                            if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(text3))
                            {
                                if (text2.StartsWith("sr:"))
                                {
                                    try
                                    {
                                        RegexTranslationSplitter regexTranslationSplitter = new RegexTranslationSplitter(text2, text3);
                                        if (activeLevels.Count > 0)
                                        {
                                            foreach (int level in activeLevels)
                                            {
                                                var scoped = _scopedTranslations[level];
                                                if (!scoped.RegisteredSplitterRegexes.Contains(regexTranslationSplitter.Original))
                                                {
                                                    scoped.RegisteredSplitterRegexes.Add(regexTranslationSplitter.Original);
                                                    scoped.SplitterRegexes.Add(regexTranslationSplitter);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            this.AddTranslationSplitterRegex(regexTranslationSplitter);
                                        }
                                        continue;
                                    }
                                    catch (Exception ex)
                                    {
                                        ManualLogSource logger = TranslatePlugin.logger;
                                        string[] array3 = new string[5];
                                        array3[0] = "An error occurred while constructing the regexTranslationSplitter: '";
                                        array3[1] = text;
                                        array3[2] = "'.";
                                        array3[3] = Environment.NewLine;
                                        int num = 4;
                                        Exception ex2 = ex;
                                        array3[num] = ((ex2 != null) ? ex2.ToString() : null);
                                        logger.LogWarning(string.Concat(array3));
                                        continue;
                                    }
                                }
                                if (text2.StartsWith("r:"))
                                {
                                    try
                                    {
                                        RegexTranslation regexTranslation = new RegexTranslation(text2, text3);
                                        if (activeLevels.Count > 0)
                                        {
                                            foreach (int level in activeLevels)
                                            {
                                                var scoped = _scopedTranslations[level];
                                                if (!scoped.RegisteredRegexes.Contains(regexTranslation.Original))
                                                {
                                                    scoped.RegisteredRegexes.Add(regexTranslation.Original);
                                                    scoped.DefaultRegexes.Add(regexTranslation);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            this.AddTranslationRegex(regexTranslation);
                                        }
                                        continue;
                                    }
                                    catch (Exception ex3)
                                    {
                                        ManualLogSource logger2 = TranslatePlugin.logger;
                                        string[] array4 = new string[5];
                                        array4[0] = "An error occurred while constructing the regexTranslation: '";
                                        array4[1] = text;
                                        array4[2] = "'.";
                                        array4[3] = Environment.NewLine;
                                        int num2 = 4;
                                        Exception ex4 = ex3;
                                        array4[num2] = ((ex4 != null) ? ex4.ToString() : null);
                                        logger2.LogWarning(string.Concat(array4));
                                        continue;
                                    }
                                }
                                if (activeLevels.Count > 0)
                                {
                                    foreach (int level in activeLevels)
                                    {
                                        var scoped = _scopedTranslations[level];
                                        scoped.Translations[text2] = text3;
                                        scoped.ReverseTranslations[text3] = text2;
                                    }
                                }
                                else
                                {
                                    this.AddTranslation(text2, text3);
                                }
                            }
                        }
                    }
                    catch (Exception ex5)
                    {
                        ManualLogSource logger3 = TranslatePlugin.logger;
                        string[] array5 = new string[5];
                        array5[0] = "An error occurred while reading the translation: '";
                        array5[1] = text;
                        array5[2] = "'.";
                        array5[3] = Environment.NewLine;
                        int num3 = 4;
                        Exception ex6 = ex5;
                        array5[num3] = ((ex6 != null) ? ex6.ToString() : null);
                        logger3.LogWarning(string.Concat(array5));
                    }
                }
            }
        }

        private void AddTranslation(string key, string value)
        {
            if (key != null && value != null)
            {
                this._translations[key] = value;
                this._reverseTranslations[value] = key;
            }
        }

        private void AddTranslationSplitterRegex(RegexTranslationSplitter regex)
        {
            if (!this._registeredSplitterRegexes.Contains(regex.Original))
            {
                this._registeredSplitterRegexes.Add(regex.Original);
                this._splitterRegexes.Add(regex);
            }
        }

        private void AddTranslationRegex(RegexTranslation regex)
        {
            if (!this._registeredRegexes.Contains(regex.Original))
            {
                this._registeredRegexes.Add(regex.Original);
                this._defaultRegexes.Add(regex);
            }
        }

        private bool IsTranslation(string translation, int scope = -1)
        {
            if (this._reverseTranslations.ContainsKey(translation))
                return true;
            if (scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scoped)
                && scoped.ReverseTranslations.ContainsKey(translation))
                return true;
            return false;
        }

        private bool IsTokenTranslation(string translation)
        {
            return this._reverseTokenTranslations.ContainsKey(translation);
        }

        public bool IsTranslatable(string text, bool isToken, int scope = -1)
        {
            bool flag = !this.IsTranslation(text, scope);
            if (isToken && flag)
            {
                flag = !this.IsTokenTranslation(text);
            }
            return flag;
        }

        public string SplitterTranslate(string text, RegexTranslationSplitter splitter, int scope = -1)
        {
            if (!string.IsNullOrEmpty(text))
            {
                RegexTranslationSplitter splitter2 = splitter;
                if (((splitter2 != null) ? splitter2.CompiledRegex : null) != null)
                {
                    try
                    {
                        Func<string, string> translationFunc = null;
                        int capturedScope = scope;
                        return splitter.CompiledRegex.Replace(text, delegate (Match match)
                        {
                            if (translationFunc == null)
                            {
                                translationFunc = delegate (string groupValue)
                                {
                                    string text2;
                                    if (capturedScope >= 0 && _scopedTranslations.TryGetValue(capturedScope, out var s)
                                        && s.Translations.TryGetValue(groupValue, out text2))
                                    {
                                        return text2;
                                    }
                                    if (this._translations.TryGetValue(groupValue, out text2))
                                    {
                                        return text2;
                                    }
                                    return groupValue;
                                };
                            }
                            string translation = splitter.Translation;
                            return this.ApplyDotNetReplacement(translation, match, translationFunc);
                        });
                    }
                    catch (Exception ex)
                    {
                        TranslatePlugin.logger.LogWarning($"Splitter regex '{splitter.Original}' error: {ex.Message}");
                        return text;
                    }
                }
            }
            return text;
        }

        public string TryTranslate(string text, int scope = -1)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scoped)
                && scoped.Translations.TryGetValue(text, out var scopedResult))
            {
                return scopedResult;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            string text2;
            try
            {
                NormalTextTranslator.CheckAndCleanupRegexCache();
                if (!this._translations.TryGetValue(text, out text2))
                {
                    string text3 = text;
                    try
                    {
                        bool skipRegex = scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scopedCheck)
                            ? scopedCheck.FailedRegexLookups.ContainsKey(text3)
                            : this._failedRegexLookups.ContainsKey(text3);

                        if (!skipRegex)
                        {
                            bool regexMatched = false;

                            RegexTranslationSplitter[] splitterSnapshot;
                            RegexTranslation[] regexSnapshot;
                            lock (this._regexLock)
                            {
                                if (scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scopedSplitter)
                                    && scopedSplitter.SplitterRegexes.Count > 0)
                                {
                                    splitterSnapshot = scopedSplitter.SplitterRegexes
                                        .Concat(this._splitterRegexes).ToArray();
                                }
                                else
                                {
                                    splitterSnapshot = this._splitterRegexes.ToArray();
                                }
                                if (scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scopedRegex)
                                    && scopedRegex.DefaultRegexes.Count > 0)
                                {
                                    regexSnapshot = scopedRegex.DefaultRegexes
                                        .Concat(this._defaultRegexes).ToArray();
                                }
                                else
                                {
                                    regexSnapshot = this._defaultRegexes.ToArray();
                                }
                            }

                            foreach (RegexTranslationSplitter splitter in splitterSnapshot)
                            {
                                text3 = this.SplitterTranslate(text3, splitter, scope);
                            }

                            for (int i = 0; i < regexSnapshot.Length; i++)
                            {
                                RegexTranslation regexTrans = regexSnapshot[i];
                                string text5 = "default_" + regexTrans.Original;
                                Regex orAdd = NormalTextTranslator._regexCache.GetOrAdd(text5, (string _) => new Regex(regexTrans.Original, RegexOptions.Multiline | _regexCompiledSupportedFlag | RegexOptions.Singleline));
                                NormalTextTranslator._regexCacheLastAccess[text5] = DateTime.Now;

                                if (orAdd.IsMatch(text3))
                                {
                                    text3 = orAdd.Replace(text3, regexTrans.Translation);
                                    regexMatched = true;
                                }
                            }

                            if (!regexMatched)
                            {
                                if (scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scopedFail))
                                {
                                    scopedFail.FailedRegexLookups.TryAdd(text3, 0);
                                    if (scopedFail.FailedRegexLookups.Count > 10000)
                                    {
                                        scopedFail.FailedRegexLookups.Clear();
                                        TranslatePlugin.logger.LogInfo($"Scoped failed regex lookup cache reached limit for scope {scope}, cleared");
                                    }
                                }
                                else
                                {
                                    this._failedRegexLookups.TryAdd(text3, 0);
                                    if (this._failedRegexLookups.Count > 10000)
                                    {
                                        this._failedRegexLookups.Clear();
                                        TranslatePlugin.logger.LogInfo("Failed regex lookup cache reached limit, cleared");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string textSnippet = NormalTextTranslator.GetTextSnippet(text, 50);
                        TranslatePlugin.logger.LogError("There is a problem with the translation method: " + textSnippet);
                        TranslatePlugin.logger.LogError("Translation error: " + ex.Message + "\n" + ex.StackTrace);
                    }
                    text2 = text3;
                }
            }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 500L)
                {
                    string textSnippet2 = NormalTextTranslator.GetTextSnippet(text, 50);
                    TranslatePlugin.logger.LogWarning(string.Format("TryTranslate took {0}ms for text: {1}", stopwatch.ElapsedMilliseconds, textSnippet2));
                }
            }
            return text2;
        }

        public static string getRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return pattern;
            }
            return pattern.Replace("=", "\\=");
        }

        private bool HasTranslated(string key)
        {
            return this._translations.ContainsKey(key);
        }

        [CompilerGenerated]
        internal static string ChangeRegexReplaceFunc(Match match)
        {
            int num = int.Parse(match.Groups[1].Value);
            return "{" + (num - 1).ToString() + "}";
        }

        private static void CheckAndCleanupRegexCache()
        {
            if (DateTime.Now - NormalTextTranslator._lastRegexCacheCleanupTime <= NormalTextTranslator.REGEX_CACHE_CLEANUP_INTERVAL)
            {
                return;
            }
            try
            {
                bool isMemoryPressure = GC.GetTotalMemory(false) > 104857600L;
                int numToRemove = 0;

                if (isMemoryPressure)
                {
                    numToRemove = (int)((float)NormalTextTranslator._regexCache.Count * 0.3f);
                    numToRemove = Math.Max(1, Math.Min(numToRemove, NormalTextTranslator._regexCache.Count));
                }
                else if (NormalTextTranslator._regexCache.Count > 800)
                {
                    numToRemove = NormalTextTranslator._regexCache.Count - 800;
                }

                if (numToRemove > 0)
                {
                    List<string> keysToRemove = NormalTextTranslator._regexCacheLastAccess
                        .Where(kv => NormalTextTranslator._regexCache.ContainsKey(kv.Key))
                        .OrderBy(kv => kv.Value)
                        .Take(numToRemove)
                        .Select(kv => kv.Key)
                        .ToList();

                    int removedCount = 0;
                    foreach (string key in keysToRemove)
                    {
                        Regex regex;
                        DateTime dateTime;
                        if (NormalTextTranslator._regexCache.TryRemove(key, out regex) &&
                            NormalTextTranslator._regexCacheLastAccess.TryRemove(key, out dateTime))
                        {
                            removedCount++;
                        }
                    }

                    if (removedCount > 0)
                    {
                        TranslatePlugin.logger.LogInfo($"Cleaned {removedCount} regex cache entries. Remaining: {NormalTextTranslator._regexCache.Count}");
                    }
                }

                NormalTextTranslator._lastRegexCacheCleanupTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error cleaning up regex cache: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void CleanupCurrentFileRegexCache()
        {
            List<string> list = this._defaultRegexes.Select((RegexTranslation r) => "default_" + r.Original).ToList<string>();
            int num = 0;
            foreach (string text in list)
            {
                Regex regex;
                if (NormalTextTranslator._regexCache.TryRemove(text, out regex))
                {
                    num++;
                }
                DateTime dateTime;
                NormalTextTranslator._regexCacheLastAccess.TryRemove(text, out dateTime);
            }
            TranslatePlugin.logger.LogInfo(string.Format("Cleaned {0} old regex cache entries for {1}", num, this.FileName));
        }

        private void PrecompileAndCacheRegexes()
        {
            using (List<RegexTranslation>.Enumerator enumerator2 = this._defaultRegexes.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    RegexTranslation regexTrans = enumerator2.Current;
                    string text2 = "default_" + regexTrans.Original;
                    NormalTextTranslator._regexCache.GetOrAdd(text2, (string _) => new Regex(regexTrans.Original, RegexOptions.Multiline | _regexCompiledSupportedFlag | RegexOptions.Singleline));
                    NormalTextTranslator._regexCacheLastAccess[text2] = DateTime.Now;
                }
            }
        }

        private static string GetTextSnippet(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "[Empty Text]";
            }
            if (text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength) + "...";
        }

        private string ApplyDotNetReplacement(string replacement, Match match, Func<string, string> translate)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int i = 0;
            while (i < replacement.Length)
            {
                if (replacement[i] == '$')
                {
                    if (i + 1 < replacement.Length)
                    {
                        if (replacement[i + 1] == '$')
                        {
                            stringBuilder.Append('$');
                            i += 2;
                        }
                        else if (char.IsDigit(replacement[i + 1]))
                        {
                            int num = i + 1;
                            int num2 = num;
                            while (num2 < replacement.Length && char.IsDigit(replacement[num2]))
                            {
                                num2++;
                            }
                            int num3;
                            if (int.TryParse(replacement.Substring(num, num2 - num), out num3) && num3 >= 0 && num3 < match.Groups.Count)
                            {
                                string value = match.Groups[num3].Value;
                                stringBuilder.Append(translate(value));
                                i = num2;
                            }
                            else
                            {
                                stringBuilder.Append('$');
                                i++;
                            }
                        }
                        else
                        {
                            stringBuilder.Append('$');
                            i++;
                        }
                    }
                    else
                    {
                        stringBuilder.Append('$');
                        i++;
                    }
                }
                else
                {
                    stringBuilder.Append(replacement[i]);
                    i++;
                }
            }
            return stringBuilder.ToString();
        }

        private ConcurrentDictionary<string, string> _translations = new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, string> _reverseTranslations = new ConcurrentDictionary<string, string>();

        internal readonly object _regexLock = new object();

        internal List<RegexTranslation> _defaultRegexes = new List<RegexTranslation>();

        private HashSet<string> _registeredRegexes = new HashSet<string>();

        internal List<RegexTranslationSplitter> _splitterRegexes = new List<RegexTranslationSplitter>();

        private HashSet<string> _registeredSplitterRegexes = new HashSet<string>();

        private ConcurrentDictionary<string, byte> _failedRegexLookups = new ConcurrentDictionary<string, byte>();

        private ConcurrentDictionary<int, ScopedTranslationData> _scopedTranslations = new ConcurrentDictionary<int, ScopedTranslationData>();

        private ConcurrentDictionary<string, string> _tokenTranslations = new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, string> _reverseTokenTranslations = new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, string> _partialTranslations = new ConcurrentDictionary<string, string>();

        internal class ScopedTranslationData
        {
            internal ConcurrentDictionary<string, string> Translations { get; set; } = new ConcurrentDictionary<string, string>();
            internal ConcurrentDictionary<string, string> ReverseTranslations { get; set; } = new ConcurrentDictionary<string, string>();
            internal List<RegexTranslation> DefaultRegexes { get; set; } = new List<RegexTranslation>();
            internal HashSet<string> RegisteredRegexes { get; set; } = new HashSet<string>();
            internal List<RegexTranslationSplitter> SplitterRegexes { get; set; } = new List<RegexTranslationSplitter>();
            internal HashSet<string> RegisteredSplitterRegexes { get; set; } = new HashSet<string>();
            internal ConcurrentDictionary<string, byte> FailedRegexLookups { get; set; } = new ConcurrentDictionary<string, byte>();
        }

        internal bool IsScopedTranslation(string text, int scope)
        {
            return scope >= 0 && _scopedTranslations.TryGetValue(scope, out var scoped)
                && scoped.Translations.ContainsKey(text);
        }

        internal ScopedTranslationData GetOrCreateScopedData(int scope)
        {
            return _scopedTranslations.GetOrAdd(scope, _ => new ScopedTranslationData());
        }

        internal void ClearScopedFailedRegexLookups(int scope)
        {
            if (_scopedTranslations.TryGetValue(scope, out var scopedData))
            {
                scopedData.FailedRegexLookups.Clear();
            }
        }

        public string FileName;

        public string FilePath;

        private static readonly ConcurrentDictionary<string, Regex> _regexCache = new ConcurrentDictionary<string, Regex>();

        private static readonly ConcurrentDictionary<string, DateTime> _regexCacheLastAccess = new ConcurrentDictionary<string, DateTime>();

        private static DateTime _lastRegexCacheCleanupTime = DateTime.Now;

        private static readonly TimeSpan REGEX_CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(30.0);

        private static RegexOptions _regexCompiledSupportedFlag = RegexOptions.None;

        static NormalTextTranslator()
        {
            CheckRegexCompiledSupport();
        }

        private static void CheckRegexCompiledSupport()
        {
            try
            {
                string testSubject = "She believed";
                string testPattern = ".he ..lie..d";
                Regex testRegex = new Regex(testPattern, RegexOptions.Compiled);
                var testResult = testRegex.Match(testSubject);
                if (testResult.Success)
                {
                    _regexCompiledSupportedFlag = RegexOptions.Compiled;
                }
                else
                {
                    TranslatePlugin.logger.LogInfo("Regex compilation support check encountered unknown error");
                }
            }
            catch (Exception)
            {
                TranslatePlugin.logger.LogInfo("Current game version does not support compiled regex, using non-compiled mode");
            }
        }

        public static RegexOptions RegexCompiledSupportedFlag => _regexCompiledSupportedFlag;
    }
}
