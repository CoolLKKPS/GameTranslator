using GameTranslator.Patches.Translatons;
using GameTranslator.Patches.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace GameTranslator
{
    public class TranslateConfig
    {
        private static SafeFileWatcher _fileWatcher;

        private static Dictionary<string, DateTime> _fileLastModifiedTimes = new Dictionary<string, DateTime>();

        public static void Load()
        {
            TranslateConfig.hud = TranslateConfig.CreateNewConfig("HUD-Translate", false);
            TranslateConfig.hud.shouldTranslate = TranslatePlugin.shouldTranslateHUD.Value;
            TranslateConfig.hudText = new NormalTextTranslator(TranslateConfig.hud.ConfigFileName + ".cfg");
            TranslateConfig.hudText.Load();
            TranslateConfig.items = TranslateConfig.CreateNewConfig("Item-Translate");
            TranslateConfig.items.shouldTranslate = TranslatePlugin.shouldTranslateItems.Value;
            TranslateConfig.itemsText = new NormalTextTranslator(TranslateConfig.items.ConfigFileName + ".cfg");
            TranslateConfig.itemsText.Load();
            TranslateConfig.terminal = TranslateConfig.CreateNewConfig("Terminal-Translate");
            TranslateConfig.terminal.shouldTranslate = TranslatePlugin.shouldTranslateTerimal.Value;
            TranslateConfig.terminalText = new NormalTextTranslator(TranslateConfig.terminal.ConfigFileName + ".cfg");
            TranslateConfig.terminalText.Load();
            TranslateConfig.cmd_py = TranslateConfig.CreateNewConfig("CMD-PY-Translate");
            TranslateConfig.cmdPyText = new NormalTextTranslator(TranslateConfig.cmd_py.ConfigFileName + ".cfg");
            TranslateConfig.cmdPyText.Load();
            TranslateConfig.cmd_zh = TranslateConfig.CreateNewConfig("CMD-ZH-Translate");
            TranslateConfig.cmdZhText = new NormalTextTranslator(TranslateConfig.cmd_zh.ConfigFileName + ".cfg");
            TranslateConfig.cmdZhText.Load();
            TranslateConfig.text = TranslateConfig.CreateNewConfig("SpecialText-Translate");
            TranslateConfig.text.shouldTranslate = TranslatePlugin.shouldTranslateSpecialText.Value;
            TranslateConfig.textText = new NormalTextTranslator(TranslateConfig.text.ConfigFileName + ".cfg");
            TranslateConfig.textText.Load();
            TranslateConfig.gui = TranslateConfig.CreateNewConfig("GuiText-Translate");
            TranslateConfig.gui.shouldTranslate = TranslatePlugin.shouldTranslateGui.Value;
            TranslateConfig.guiText = new NormalTextTranslator(TranslateConfig.gui.ConfigFileName + ".cfg");
            TranslateConfig.guiText.Load();
            TranslateConfig.interactiveTerminalAPI = TranslateConfig.CreateNewConfig("InteractiveTerminalAPI-Translate");
            TranslateConfig.interactiveTerminalAPI.shouldTranslate = TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value;
            TranslateConfig.interactiveTerminalAPIText = new NormalTextTranslator(TranslateConfig.interactiveTerminalAPI.ConfigFileName + ".cfg");
            TranslateConfig.interactiveTerminalAPIText.Load();
            TranslateConfig.normal = TranslateConfig.CreateNewConfig("Normal-Translate", false);
            TranslateConfig.normal.shouldTranslate = TranslatePlugin.shouldTranslateNormalText.Value;
            TranslateConfig.normalText = new NormalTextTranslator(TranslateConfig.normal.ConfigFileName + ".cfg");
            TranslateConfig.normalText.Load();
            TranslateConfig.cache = new TextureTranslationCache();
            TranslateConfig.cache.LoadTranslationFiles();
            string fullPath = Path.GetFullPath(TranslatePlugin.DefaultPath);
            _fileWatcher = new SafeFileWatcher(fullPath);
            _fileWatcher.DirectoryUpdated += OnDirectoryUpdated;
            TranslatePlugin.logger.LogInfo("Tracking path " + fullPath);
            foreach (TranslateConfig.TranslateConfigFile config in TranslateConfig.TranslateConfigFile.configs)
            {
                if (File.Exists(config.ConfigFilePath))
                {
                    _fileLastModifiedTimes[config.ConfigFilePath] = File.GetLastWriteTime(config.ConfigFilePath);
                }
            }
            GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.ClearCache();
        }

        private static void OnDirectoryUpdated()
        {
            try
            {
                bool hasChanges = false;
                foreach (TranslateConfig.TranslateConfigFile config in TranslateConfig.TranslateConfigFile.configs)
                {
                    if (!config.shouldLoad || !File.Exists(config.ConfigFilePath))
                        continue;
                    DateTime currentModifiedTime = File.GetLastWriteTime(config.ConfigFilePath);
                    DateTime recordedTime;
                    if (_fileLastModifiedTimes.TryGetValue(config.ConfigFilePath, out recordedTime))
                    {
                        if (currentModifiedTime > recordedTime)
                        {
                            _fileLastModifiedTimes[config.ConfigFilePath] = currentModifiedTime;
                            for (int i = 0; i < 3; i++)
                            {
                                try
                                {
                                    config.Reload();
                                    NormalTextTranslator moduleTranslator = TranslateConfig.GetModuleTranslator(config);
                                    if (moduleTranslator != null)
                                    {
                                        moduleTranslator.Load();
                                    }
                                    TextTranslate.ChangeTime += 1L;
                                    hasChanges = true;
                                    break;
                                }
                                catch (IOException) when (i < 2)
                                {
                                    Thread.Sleep(100 * (i + 1));
                                }
                                catch (Exception ex)
                                {
                                    TranslatePlugin.logger.LogError($"Unexpected error reloading config {config.ConfigFileName}: {ex.Message}");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        _fileLastModifiedTimes[config.ConfigFilePath] = currentModifiedTime;
                    }
                }
                if (hasChanges)
                {
                    TranslatePlugin.logger.LogInfo("Translate files reloaded due to file changes.");
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error in OnDirectoryUpdated: " + ex.Message);
            }
        }

        public static void show(TranslateConfig.TranslateConfigFile file)
        {
            foreach (string text in file.normal.Keys)
            {
                TranslatePlugin.LogInfo(text + "=" + file.normal[text]);
            }
        }

        private static TranslateConfig.TranslateConfigFile CreateNewConfig(string fileName, bool should)
        {
            TranslatePlugin.logger.LogInfo("GameTranslator is loading config file call " + fileName);
            return new TranslateConfig.TranslateConfigFile(fileName, true, should);
        }

        public static bool IsStringContainsEnglish(string input)
        {
            return !string.IsNullOrEmpty(input) && new Regex("[a-zA-Z]").IsMatch(input);
        }

        public static bool IsStringContainsChinese(string input)
        {
            return !string.IsNullOrEmpty(input) && new Regex("[\\u4e00-\\u9fa5]").IsMatch(input);
        }

        public static string useRegularExpression(string raw, string pattern, string result)
        {
            return Regex.Replace(raw, pattern, result);
        }

        public static string replaceByMap(string text, TranslateConfig.TranslateConfigFile file)
        {
            if (file.normal.Count == 0 && file.regexTranslations.Count == 0 && file.splitterRegexTranslations.Count == 0)
            {
                return text;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            string text5;
            try
            {
                if (DateTime.Now - TranslateConfig._lastCleanupTime > TranslateConfig.CLEANUP_INTERVAL)
                {
                    TranslateConfig.CleanupTranslatePairs();
                    TranslateConfig._lastCleanupTime = DateTime.Now;
                }
                if (!file.shouldTranslate)
                {
                    return text;
                }
                if (file.translatePairs.ContainsKey(text))
                {
                    file._translatePairLastAccess.TryAdd(text, DateTime.Now);
                    file._translatePairLastAccess[text] = DateTime.Now;
                    return file.translatePairs[text];
                }
                StringBuffer stringBuffer = new StringBuffer(text);
                NormalTextTranslator moduleTranslator = TranslateConfig.GetModuleTranslator(file);
                if (moduleTranslator != null)
                {
                    foreach (RegexTranslationSplitter regexTranslationSplitter in moduleTranslator._splitterRegexes)
                    {
                        string text2 = moduleTranslator.SplitterTranslate(stringBuffer.ToString(), regexTranslationSplitter, false);
                        stringBuffer.Clear().Append(text2);
                    }
                    foreach (RegexTranslation regexTranslation in moduleTranslator._defaultRegexes)
                    {
                        if (regexTranslation.CompiledRegex != null && regexTranslation.CompiledRegex.IsMatch(stringBuffer.ToString()))
                        {
                            string text3 = regexTranslation.CompiledRegex.Replace(stringBuffer.ToString(), regexTranslation.Translation);
                            stringBuffer.Clear().Append(text3);
                        }
                    }
                }
                foreach (KeyValuePair<string, string> keyValuePair in file.normal.OrderByDescending((KeyValuePair<string, string> kv) => kv.Key.Length))
                {
                    stringBuffer.ReplaceFull(keyValuePair.Key, keyValuePair.Value);
                }
                string text4 = stringBuffer.ToString();
                file.translatePairs[text] = text4;
                file._translatePairLastAccess.TryAdd(text, DateTime.Now);
                text5 = text4;
            }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 500L)
                {
                    string text11 = ((text.Length > 50) ? (text.Substring(0, 50) + "...") : text);
                    TranslatePlugin.logger.LogWarning(string.Format("replaceByMap took {0}ms for text: {1}", stopwatch.ElapsedMilliseconds, text11));
                }
            }
            return text5;
        }

        private static NormalTextTranslator GetModuleTranslator(TranslateConfig.TranslateConfigFile file)
        {
            if (file == TranslateConfig.normal)
            {
                return TranslateConfig.normalText;
            }
            if (file == TranslateConfig.hud)
            {
                return TranslateConfig.hudText;
            }
            if (file == TranslateConfig.items)
            {
                return TranslateConfig.itemsText;
            }
            if (file == TranslateConfig.terminal)
            {
                return TranslateConfig.terminalText;
            }
            if (file == TranslateConfig.text)
            {
                return TranslateConfig.textText;
            }
            if (file == TranslateConfig.cmd_py)
            {
                return TranslateConfig.cmdPyText;
            }
            if (file == TranslateConfig.cmd_zh)
            {
                return TranslateConfig.cmdZhText;
            }
            if (file == TranslateConfig.gui)
            {
                return TranslateConfig.guiText;
            }
            if (file == TranslateConfig.interactiveTerminalAPI)
            {
                return TranslateConfig.interactiveTerminalAPIText;
            }
            return null;
        }

        private static void CleanupTranslatePairs()
        {
            bool flag = GC.GetTotalMemory(false) > 104857600L;
            foreach (TranslateConfig.TranslateConfigFile translateConfigFile in TranslateConfig.TranslateConfigFile.configs)
            {
                int num = 0;
                if (flag)
                {
                    num = (int)((float)translateConfigFile.translatePairs.Count * 0.2f);
                    num = Math.Max(1, Math.Min(num, translateConfigFile.translatePairs.Count));
                }
                else if (translateConfigFile.translatePairs.Count > 6000)
                {
                    num = translateConfigFile.translatePairs.Count - 6000;
                }
                if (num > 0)
                {
                    List<string> list = translateConfigFile._translatePairLastAccess.OrderBy((KeyValuePair<string, DateTime> kv) => kv.Value).Take(num).Select(delegate (KeyValuePair<string, DateTime> kv)
                    {
                        KeyValuePair<string, DateTime> keyValuePair = kv;
                        return keyValuePair.Key;
                    })
                        .ToList<string>();
                    foreach (string text in list)
                    {
                        translateConfigFile.translatePairs.Remove(text);
                        DateTime dateTime;
                        translateConfigFile._translatePairLastAccess.TryRemove(text, out dateTime);
                    }
                    TranslatePlugin.logger.LogInfo(string.Format("Cleaned {0} translate pairs from {1}. Remaining: {2}", list.Count, translateConfigFile.ConfigFileName, translateConfigFile.translatePairs.Count));
                }
            }
        }

        private static TranslateConfig.TranslateConfigFile CreateNewConfig(string fileName)
        {
            return TranslateConfig.CreateNewConfig(fileName, true);
        }

        public static TranslateConfig.TranslateConfigFile normal;

        public static TranslateConfig.TranslateConfigFile hud;

        public static TranslateConfig.TranslateConfigFile items;

        public static TranslateConfig.TranslateConfigFile terminal;

        public static TranslateConfig.TranslateConfigFile text;

        public static TranslateConfig.TranslateConfigFile cmd_py;

        public static TranslateConfig.TranslateConfigFile cmd_zh;

        public static TranslateConfig.TranslateConfigFile gui;

        public static TranslateConfig.TranslateConfigFile interactiveTerminalAPI;

        public static NormalTextTranslator normalText;

        public static NormalTextTranslator hudText;

        public static NormalTextTranslator guiText;

        public static TextureTranslationCache cache;

        public static NormalTextTranslator itemsText;

        public static NormalTextTranslator terminalText;

        public static NormalTextTranslator textText;

        public static NormalTextTranslator cmdPyText;

        public static NormalTextTranslator cmdZhText;

        public static NormalTextTranslator interactiveTerminalAPIText;

        private static DateTime _lastCleanupTime = DateTime.Now;

        private static readonly TimeSpan CLEANUP_INTERVAL = TimeSpan.FromMinutes(30.0);

        public class TranslateConfigFile
        {
            public TranslateConfigFile(string configName, bool saveOnInit, bool shouldLoad)
            {
                this.ConfigFileName = configName;
                this.ConfigFilePath = TranslatePlugin.DefaultPath + configName + ".cfg";
                this.shouldLoad = shouldLoad;
                if (this.ConfigFilePath == null)
                {
                    throw new ArgumentNullException("configPath");
                }
                this.ConfigFilePath = Path.GetFullPath(this.ConfigFilePath);
                if (this.shouldLoad && File.Exists(this.ConfigFilePath))
                {
                    this.Reload();
                }
                else if (saveOnInit)
                {
                    this.Save();
                }
                TranslateConfig.TranslateConfigFile.configs.Add(this);
            }

            public void Reload()
            {
                this.translatePairs.Clear();
                this._translatePairLastAccess.Clear();
                this.normal.Clear();
                this.regexTranslations.Clear();
                this.splitterRegexTranslations.Clear();
                List<string> list = new List<string>();
                foreach (string text in File.ReadAllLines(this.ConfigFilePath))
                {
                    if (!text.StartsWith("#") && text.Contains("="))
                    {
                        string[] array2 = TranslateConfig.TranslateConfigFile.regex.Split(text);
                        if (array2.Length == 2)
                        {
                            string text2 = array2[0].Replace("\\=", "=");
                            string text3 = array2[1].Replace("\\=", "=");
                            if (text2.StartsWith("r:"))
                            {
                                try
                                {
                                    RegexTranslation regexTranslation = new RegexTranslation(text2, text3);
                                    this.regexTranslations.Add(regexTranslation);
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    string text4 = text2 + "=" + text3;
                                    list.Add("Invalid regex: " + text4 + " - " + ex.Message);
                                    TranslatePlugin.logger.LogWarning("Failed to parse regex: " + text4 + ". Error: " + ex.Message);
                                    continue;
                                }
                            }
                            if (text2.StartsWith("sr:"))
                            {
                                try
                                {
                                    RegexTranslationSplitter regexTranslationSplitter = new RegexTranslationSplitter(text2, text3);
                                    this.splitterRegexTranslations.Add(regexTranslationSplitter);
                                    continue;
                                }
                                catch (Exception ex2)
                                {
                                    string text5 = text2 + "=" + text3;
                                    list.Add("Invalid splitter regex: " + text5 + " - " + ex2.Message);
                                    TranslatePlugin.logger.LogWarning("Failed to parse splitter regex: " + text5 + ". Error: " + ex2.Message);
                                    continue;
                                }
                            }
                            if (this.normal.ContainsKey(text2))
                            {
                                this.normal[text2] = text3;
                                this.special[text3] = text2;
                            }
                            else
                            {
                                this.normal.Add(text2, text3);
                                if (!this.special.ContainsKey(text3))
                                {
                                    this.special.Add(text3, text2);
                                }
                            }
                            if (text2.Length < this.shouldTranslateMinLength)
                            {
                                this.shouldTranslateMinLength = text2.Length;
                            }
                            if (text2.Length > this.shouldTranslateMaxLength)
                            {
                                this.shouldTranslateMaxLength = text2.Length;
                            }
                        }
                    }
                }
                if (list.Count > 0)
                {
                    File.AppendAllLines(Path.Combine(Path.GetDirectoryName(this.ConfigFilePath), this.ConfigFileName + "_errors.log"), list);
                }
            }

            public void Log(string text)
            {
                this.logs.Add(text);
                string directoryName = Path.GetDirectoryName(this.ConfigFilePath);
                if (directoryName == null)
                {
                    Directory.CreateDirectory(directoryName);
                }
                new List<string>().Add("##" + this.ConfigFileName);
                File.WriteAllLines(this.ConfigFilePath, this.logs);
            }

            public void Save()
            {
                string directoryName = Path.GetDirectoryName(this.ConfigFilePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                if (!File.Exists(this.ConfigFilePath))
                {
                    File.Create(this.ConfigFilePath).Close();
                    return;
                }
                if (this.shouldLoad)
                {
                    List<string> list = new List<string>();
                    list.Add("##" + this.ConfigFileName);
                    foreach (KeyValuePair<string, string> keyValuePair in this.normal)
                    {
                        string text = keyValuePair.Key.Replace("=", "\\=");
                        string text2 = keyValuePair.Value.Replace("=", "\\=");
                        list.Add(text + "=" + text2);
                    }
                    list.Add("##RegularExpression");
                    foreach (RegexTranslation regexTranslation in this.regexTranslations)
                    {
                        list.Add("r:" + regexTranslation.Key + "=" + regexTranslation.Value);
                    }
                    list.Add("##SplitterRegularExpression");
                    foreach (RegexTranslationSplitter regexTranslationSplitter in this.splitterRegexTranslations)
                    {
                        list.Add("sr:" + regexTranslationSplitter.Key + "=" + regexTranslationSplitter.Value);
                    }
                    File.WriteAllLines(this.ConfigFilePath, list.ToArray());
                }
            }

            public static Regex regex
            {
                get
                {
                    if (TranslateConfig.TranslateConfigFile._regex == null)
                    {
                        Type typeFromHandle = typeof(TranslateConfig.TranslateConfigFile);
                        lock (typeFromHandle)
                        {
                            if (TranslateConfig.TranslateConfigFile._regex == null)
                            {
                                TranslateConfig.TranslateConfigFile._regex = new Regex("(?<!\\\\)=", NormalTextTranslator.RegexCompiledSupportedFlag);
                            }
                        }
                    }
                    return TranslateConfig.TranslateConfigFile._regex;
                }
            }

            public string ConfigFilePath;

            public string ConfigFileName;

            public bool shouldTranslate;

            public int shouldTranslateMinLength = 200;

            public int shouldTranslateMaxLength;

            public bool shouldLoad = true;

            public IDictionary<string, string> normal = new Dictionary<string, string>();

            public IDictionary<string, string> special = new Dictionary<string, string>();

            public static HashSet<TranslateConfig.TranslateConfigFile> configs = new HashSet<TranslateConfig.TranslateConfigFile>();

            public Dictionary<string, string> translatePairs = new Dictionary<string, string>();

            private List<string> logs = new List<string>();

            public List<RegexTranslation> regexTranslations = new List<RegexTranslation>();

            public List<RegexTranslationSplitter> splitterRegexTranslations = new List<RegexTranslationSplitter>();

            private static Regex _regex;

            internal readonly ConcurrentDictionary<string, DateTime> _translatePairLastAccess = new ConcurrentDictionary<string, DateTime>();
        }
    }
}
