using GameTranslator.Patches.Translatons;
using GameTranslator.Patches.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace GameTranslator
{
    internal class TranslateConfig
    {
        private static SafeFileWatcher _fileWatcher;

        private static ConcurrentDictionary<string, DateTime> _fileLastModifiedTimes = new ConcurrentDictionary<string, DateTime>();

        private static Timer _pollingTimer;

        private static readonly object _updateLock = new object();

        public static void Load()
        {
            if (TranslatePlugin.shouldTranslateNormalText.Value)
            {
                TranslateConfig.normal = TranslateConfig.CreateNewConfig("Normal-Translate", true, false);
                TranslateConfig.normal.shouldTranslate = true;
                TranslateConfig.normalText = new NormalTextTranslator(TranslateConfig.normal.ConfigFileName + ".cfg");
                TranslateConfig.normalText.Load(true);
            }
            if (TranslatePlugin.shouldTranslateTerimal.Value)
            {
                TranslateConfig.terminal = TranslateConfig.CreateNewConfig("Terminal-Translate", true);
                TranslateConfig.terminal.shouldTranslate = true;
            }
            if (TranslatePlugin.shouldTranslateInteractiveTerminalAPI.Value)
            {
                TranslateConfig.interactiveTerminalAPI = TranslateConfig.CreateNewConfig("InteractiveTerminalAPI-Translate", true);
                TranslateConfig.interactiveTerminalAPI.shouldTranslate = true;
            }
            if (TranslatePlugin.TerimalCanUseShortCutOne.Value)
            {
                TranslateConfig.cmd_zh = TranslateConfig.CreateNewConfig("CMD-ZH-Translate", true);
            }
            if (TranslatePlugin.TerimalCanUseShortCutTwo.Value)
            {
                TranslateConfig.cmd_py = TranslateConfig.CreateNewConfig("CMD-PY-Translate", true);
            }
            if (TranslatePlugin.shouldTranslateGui.Value)
            {
                TranslateConfig.gui = TranslateConfig.CreateNewConfig("GuiText-Translate", true, false);
                TranslateConfig.gui.shouldTranslate = true;
                TranslateConfig.guiText = new NormalTextTranslator(TranslateConfig.gui.ConfigFileName + ".cfg");
                TranslateConfig.guiText.Load(true);
            }
            if (TranslatePlugin.changeTexture.Value)
            {
                TranslateConfig.cache = new TextureTranslationCache();
                TranslateConfig.cache.LoadTranslationFiles();
            }
            string fullPath = Path.GetFullPath(TranslatePlugin.DefaultPath);
            if (TranslatePlugin.enableFileWatcher?.Value ?? false)
            {
                _fileWatcher = new SafeFileWatcher(fullPath);
                _fileWatcher.DirectoryUpdated += OnDirectoryUpdated;
                TranslatePlugin.logger.LogInfo("Tracking path " + fullPath);
            }
            foreach (TranslateConfig.TranslateConfigFile config in TranslateConfig.TranslateConfigFile.configs)
            {
                if (File.Exists(config.ConfigFilePath))
                {
                    _fileLastModifiedTimes[config.ConfigFilePath] = File.GetLastWriteTime(config.ConfigFilePath);
                }
            }
            GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.ClearCache();
            GameTranslator.Patches.Translatons.Manipulator.DefaultTextComponentManipulator.ClearCache();
            if (TranslatePlugin.enablePollingCheck?.Value ?? false)
            {
                _pollingTimer = new Timer(_ => OnDirectoryUpdated(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                TranslatePlugin.logger.LogInfo("Polling check tracking path " + fullPath);
            }
        }

        public static void Unload()
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;
            cache?.Dispose();
            cache = null;
            _pollingTimer?.Dispose();
            _pollingTimer = null;
            GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.ClearCache();
            GameTranslator.Patches.Translatons.Manipulator.DefaultTextComponentManipulator.ClearCache();
        }

        private static void OnDirectoryUpdated()
        {
            lock (_updateLock)
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
                                            moduleTranslator.Load(false);
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
                        GameTranslator.Patches.Translatons.AsyncTranslationManager.Instance.ClearCache();
                        GameTranslator.Patches.Translatons.Manipulator.DefaultTextComponentManipulator.ClearCache();
                        TranslatePlugin.logger.LogInfo("Translate files reloaded due to file changes.");
                    }
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError("Error in OnDirectoryUpdated: " + ex.Message);
                }
            }
        }

        private static TranslateConfig.TranslateConfigFile CreateNewConfig(string fileName, bool should, bool needsParseFile = true)
        {
            TranslatePlugin.logger.LogInfo(">>> Loading " + fileName + " file");
            return new TranslateConfig.TranslateConfigFile(fileName, should, needsParseFile);
        }

        public static void show(TranslateConfig.TranslateConfigFile file)
        {
            foreach (string text in file.normal.Keys)
            {
                TranslatePlugin.logger.LogInfo(text + "=" + file.normal[text]);
            }
        }

        public static string replaceByMap(string text, TranslateConfig.TranslateConfigFile file)
        {
            if (file == null) return text;
            if (file.normal.Count == 0 && file.regexTranslations.Count == 0)
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
                    file._translatePairLastAccess[text] = DateTime.Now;
                    return file.translatePairs[text];
                }
                StringBuffer stringBuffer = new StringBuffer(text);
                if (file.regexTranslations.Count > 0)
                {
                    RegexTranslation[] regexSnapshot;
                    lock (file._fileLock)
                    {
                        regexSnapshot = file.regexTranslations.ToArray();
                    }
                    foreach (RegexTranslation regexTranslation in regexSnapshot)
                    {
                        if (regexTranslation.CompiledRegex.IsMatch(stringBuffer.ToString()))
                        {
                            string text3 = regexTranslation.CompiledRegex.Replace(stringBuffer.ToString(), regexTranslation.Translation);
                            stringBuffer.Clear().Append(text3);
                        }
                    }
                }
                foreach (KeyValuePair<string, string> keyValuePair in file._normalOrdered)
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

        internal static NormalTextTranslator GetModuleTranslator(TranslateConfig.TranslateConfigFile file)
        {
            if (file == TranslateConfig.normal)
            {
                return TranslateConfig.normalText;
            }
            if (file == TranslateConfig.gui)
            {
                return TranslateConfig.guiText;
            }
            return null;
        }

        private static void CleanupTranslatePairs()
        {
            bool flag = GC.GetTotalMemory(false) > TRANSLATE_PAIR_MEMORY_PRESSURE;
            foreach (TranslateConfig.TranslateConfigFile translateConfigFile in TranslateConfig.TranslateConfigFile.configs)
            {
                if (!translateConfigFile.needsParseFile)
                    continue;
                int num = 0;
                string reason = null;
                if (flag)
                {
                    if (translateConfigFile.translatePairs.Count >= TRANSLATE_PAIR_EVICT_MIN)
                    {
                        num = (int)((float)translateConfigFile.translatePairs.Count * TRANSLATE_PAIR_EVICT_RATIO);
                        num = Math.Max(1, Math.Min(num, translateConfigFile.translatePairs.Count));
                    }
                    reason = "memory pressure";
                }
                else if (translateConfigFile.translatePairs.Count > TRANSLATE_PAIR_MAX)
                {
                    num = translateConfigFile.translatePairs.Count - TRANSLATE_PAIR_MAX;
                    reason = "over limit";
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
                        translateConfigFile.translatePairs.TryRemove(text, out _);
                        DateTime dateTime;
                        translateConfigFile._translatePairLastAccess.TryRemove(text, out dateTime);
                    }
                    TranslatePlugin.logger.LogInfo(string.Format("Cleaned {0} translate pairs from {1}. Remaining: {2} (reason: {3})", list.Count, translateConfigFile.ConfigFileName, translateConfigFile.translatePairs.Count, reason));
                }
            }
        }

        public static TranslateConfig.TranslateConfigFile normal;

        public static TranslateConfig.TranslateConfigFile terminal;

        public static TranslateConfig.TranslateConfigFile interactiveTerminalAPI;

        public static TranslateConfig.TranslateConfigFile cmd_zh;

        public static TranslateConfig.TranslateConfigFile cmd_py;

        public static TranslateConfig.TranslateConfigFile gui;

        public static TextureTranslationCache cache;

        public static NormalTextTranslator normalText;

        public static NormalTextTranslator guiText;

        private static DateTime _lastCleanupTime = DateTime.Now;

        private static readonly TimeSpan CLEANUP_INTERVAL = TimeSpan.FromMinutes(30.0);

        private const long TRANSLATE_PAIR_MEMORY_PRESSURE = 536870912L;

        private const float TRANSLATE_PAIR_EVICT_RATIO = 0.2f;

        private const int TRANSLATE_PAIR_MAX = 6000;

        private const int TRANSLATE_PAIR_EVICT_MIN = 100;

        internal class TranslateConfigFile
        {
            public TranslateConfigFile(string configName, bool shouldLoad, bool needsParseFile = true)
            {
                this.ConfigFileName = configName;
                this.ConfigFilePath = Path.GetFullPath(TranslatePlugin.DefaultPath + configName + ".cfg");
                this.shouldLoad = shouldLoad;
                this.needsParseFile = needsParseFile;
                if (this.shouldLoad && this.needsParseFile && File.Exists(this.ConfigFilePath))
                {
                    this.Reload(true);
                }
                else if (!File.Exists(this.ConfigFilePath))
                {
                    this.Touch();
                }
                TranslateConfigFile.configs.Add(this);
            }

            public void Reload(bool isLoad = false)
            {
                List<string> errors = null;
                lock (this._fileLock)
                {
                    this.translatePairs.Clear();
                    this._translatePairLastAccess.Clear();
                    if (this.needsParseFile)
                    {
                        this.normal.Clear();
                        this.regexTranslations.Clear();
                        errors = ParseTranslationFile(this.ConfigFilePath, isLoad);
                    }
                }
                if (errors != null && errors.Count > 0)
                {
                    File.AppendAllLines(Path.Combine(Path.GetDirectoryName(this.ConfigFilePath), this.ConfigFileName + "_errors.log"), errors);
                }
            }

            private List<string> ParseTranslationFile(string filePath, bool isLoad = false)
            {
                if (isLoad)
                {
                    TranslatePlugin.logger.LogInfo("Loading text file: " + Path.GetFileNameWithoutExtension(filePath) + ".");
                }
                else
                {
                    TranslatePlugin.logger.LogInfo("Reloading text file: " + Path.GetFileNameWithoutExtension(filePath) + ".");
                }
                Dictionary<string, int> normalKeyLineOrder = new Dictionary<string, int>();
                List<string> errors = new List<string>();
                string[] lines = File.ReadAllLines(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string text = lines[i];
                    string[] array2 = TextHelper.ReadTranslationLineAndDecode(text);
                    if (array2 != null)
                    {
                        string text2 = array2[0];
                        string text3 = array2[1];
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
                                errors.Add("Invalid regex: " + text4 + " - " + ex.Message);
                                TranslatePlugin.logger.LogWarning("Failed to parse regex: " + text4 + ". Error: " + ex.Message);
                                continue;
                            }
                        }
                        if (this.normal.ContainsKey(text2))
                        {
                            this.normal[text2] = text3;
                        }
                        else
                        {
                            this.normal.Add(text2, text3);
                            normalKeyLineOrder[text2] = i;
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
                this.GetNormalOrderedByLength(normalKeyLineOrder);
                return errors;
            }

            public void Touch()
            {
                string directoryName = Path.GetDirectoryName(this.ConfigFilePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                if (!File.Exists(this.ConfigFilePath))
                {
                    File.Create(this.ConfigFilePath).Close();
                }
            }

            public string ConfigFilePath;

            public string ConfigFileName;

            public bool shouldTranslate;

            public bool shouldLoad = true;

            public bool needsParseFile = true;

            public IDictionary<string, string> normal = new ConcurrentDictionary<string, string>();

            public static HashSet<TranslateConfigFile> configs = new HashSet<TranslateConfigFile>();

            public ConcurrentDictionary<string, string> translatePairs = new ConcurrentDictionary<string, string>();

            internal readonly object _fileLock = new object();

            internal List<RegexTranslation> regexTranslations = new List<RegexTranslation>();

            internal readonly ConcurrentDictionary<string, DateTime> _translatePairLastAccess = new ConcurrentDictionary<string, DateTime>();

            internal KeyValuePair<string, string>[] _normalOrdered = Array.Empty<KeyValuePair<string, string>>();

            internal int shouldTranslateMinLength = 300;    // Still using for other purposes

            internal int shouldTranslateMaxLength;          // Still using for other purposes

            private void GetNormalOrderedByLength(Dictionary<string, int> lineOrder)
            {
                this._normalOrdered = this.normal
                    .OrderByDescending(kv => kv.Key.Length)
                    .ThenBy(kv => lineOrder.TryGetValue(kv.Key, out var line) ? line : int.MaxValue)
                    .ToArray();
            }
        }
    }
}
