using GameTranslator.Patches.Translatons;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using XUnity.Common.Constants;

namespace GameTranslator.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        private static int CheckForPlayerNameCommand(string firstWord, string secondWord)
        {
            int num;
            if (firstWord == "radar")
            {
                num = -1;
            }
            else if (secondWord.Length <= 2)
            {
                num = -1;
            }
            else
            {
                Debug.Log("first word: " + firstWord + "; second word: " + secondWord);
                List<string> list = new List<string>();
                for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
                {
                    list.Add(StartOfRound.Instance.mapScreen.radarTargets[i].name);
                    Debug.Log(string.Format("name {0}: {1}", i, list[i]));
                }
                string secondWordLower = secondWord.ToLower();
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].ToLower() == secondWordLower)
                    {
                        return j;
                    }
                }
                Debug.Log(string.Format("Target names length: {0}", list.Count));
                for (int k = 0; k < list.Count; k++)
                {
                    Debug.Log("A");
                    string text = list[k].ToLower();
                    Debug.Log(string.Format("Word #{0}: {1}; length: {2}", k, text, text.Length));
                    for (int l = secondWord.Length; l > 2; l--)
                    {
                        Debug.Log(string.Format("c: {0}", l));
                        Debug.Log(secondWord.Substring(0, l));
                        if (text.StartsWith(secondWord.Substring(0, l)))
                        {
                            return k;
                        }
                    }
                }
                num = -1;
            }
            return num;
        }

        [HarmonyPostfix]
        [HarmonyPatch("ParseWordOverrideOptions")]
        private static void ParseWordOverrideOptions(string playerWord, CompatibleNoun[] options, ref TerminalNode __result)
        {
            for (int i = 0; i < options.Length; i++)
            {
                for (int j = playerWord.Length; j > 0; j--)
                {
                    if (TerminalPatch.getChineseCMD(options[i].noun.word).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()))
                    {
                        __result = options[i].result;
                        return;
                    }
                    if (TerminalPatch.getPinyinCMD(options[i].noun.word).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()))
                    {
                        __result = options[i].result;
                        return;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("CheckForExactSentences")]
        private static void CheckForExactSentences(Terminal __instance, string playerWord, ref TerminalKeyword __result)
        {
            for (int i = 0; i < __instance.terminalNodes.allKeywords.Length; i++)
            {
                if (TerminalPatch.getChineseCMD(__instance.terminalNodes.allKeywords[i].word).EqualsIgnoreCase(playerWord))
                {
                    __result = __instance.terminalNodes.allKeywords[i];
                    return;
                }
                if (TerminalPatch.getPinyinCMD(__instance.terminalNodes.allKeywords[i].word).EqualsIgnoreCase(playerWord))
                {
                    __result = __instance.terminalNodes.allKeywords[i];
                    return;
                }
            }
        }

        private static string RemovePunctuation(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c))
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().ToLower();
        }

        [HarmonyPostfix]
        [HarmonyPatch("CallFunctionInAccessibleTerminalObject")]
        private static void CallFunctionInAccessibleTerminalObject(Terminal __instance, string word)
        {
            TerminalAccessibleObject[] array = global::UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (TerminalPatch.getChineseCMD(array[i].objectCode).EqualsIgnoreCase(word) || TerminalPatch.getPinyinCMD(array[i].objectCode).EqualsIgnoreCase(word))
                {
                    Debug.Log("Found accessible terminal object with corresponding string, calling function");
                    __instance.GetType().GetField("broadcastedCodeThisFrame", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(TranslateConfig.terminal, true);
                    array[i].CallFunctionFromTerminal();
                    return;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ParseWord")]
        private static void ParseWord(Terminal __instance, string playerWord, int specificityRequired, ref TerminalKeyword __result)
        {
            if (TranslatePlugin.TerimalCanUseChinese.Value || TranslatePlugin.TerimalCanUsePinyinAbbreviation.Value)
            {
                if (playerWord.Length < specificityRequired)
                {
                    __result = null;
                    return;
                }
                TerminalKeyword terminalKeyword = null;
                for (int i = 0; i < __instance.terminalNodes.allKeywords.Length; i++)
                {
                    if (!__instance.terminalNodes.allKeywords[i].isVerb || !(bool)TerminalPatch.hasGottenVerb.GetValue(__instance))
                    {
                        bool accessTerminalObjects = __instance.terminalNodes.allKeywords[i].accessTerminalObjects;
                        if (TerminalPatch.getChineseCMD(__instance.terminalNodes.allKeywords[i].word).EqualsIgnoreCase(playerWord))
                        {
                            __result = __instance.terminalNodes.allKeywords[i];
                            return;
                        }
                        if (TerminalPatch.getPinyinCMD(__instance.terminalNodes.allKeywords[i].word).EqualsIgnoreCase(playerWord))
                        {
                            __result = __instance.terminalNodes.allKeywords[i];
                            return;
                        }
                        if (terminalKeyword == null)
                        {
                            for (int j = playerWord.Length; j > specificityRequired; j--)
                            {
                                if (TerminalPatch.getChineseCMD(__instance.terminalNodes.allKeywords[i].word).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()))
                                {
                                    terminalKeyword = __instance.terminalNodes.allKeywords[i];
                                }
                                if (TerminalPatch.getPinyinCMD(__instance.terminalNodes.allKeywords[i].word).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()))
                                {
                                    terminalKeyword = __instance.terminalNodes.allKeywords[i];
                                }
                            }
                        }
                    }
                }
                if (terminalKeyword != null)
                {
                    __result = terminalKeyword;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        private static void customParser(Terminal __instance, ref TerminalNode __result)
        {
            string[] array = TerminalPatch.RemovePunctuation(__instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded)).Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseChinese.Value && TranslateConfig.cmd_zh.normal.ContainsKey("transmit") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["transmit"])) || (TranslatePlugin.TerimalCanUsePinyinAbbreviation.Value && TranslateConfig.cmd_py.normal.ContainsKey("transmit") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["transmit"]))))
            {
                try
                {
                    string text = array[1];
                    SignalTranslator signalTranslator = global::UnityEngine.Object.FindObjectOfType<SignalTranslator>();
                    if (signalTranslator != null && Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator > 8f && text.Length > 1)
                    {
                        if (!__instance.IsServer)
                        {
                            signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
                        }
                        __result = __instance.terminalNodes.specialNodes[22];
                        HUDManager.Instance.UseSignalTranslatorServerRpc(text.Substring(0, Mathf.Min(text.Length, 10)));
                    }
                    return;
                }
                catch (Exception ex)
                {
                    TranslatePlugin.logger.LogError(ex.Message);
                    return;
                }
            }
            if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseChinese.Value && TranslateConfig.cmd_zh.normal.ContainsKey("switch") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["switch"])) || (TranslatePlugin.TerimalCanUsePinyinAbbreviation.Value && TranslateConfig.cmd_py.normal.ContainsKey("switch") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["switch"]))))
            {
                int num = TerminalPatch.CheckForPlayerNameCommand(array[0], array[1]);
                if (num != -1)
                {
                    StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(num);
                    __result = __instance.terminalNodes.specialNodes[20];
                    return;
                }
            }
            else if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseChinese.Value && TranslateConfig.cmd_zh.normal.ContainsKey("ping") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["ping"])) || (TranslatePlugin.TerimalCanUsePinyinAbbreviation.Value && TranslateConfig.cmd_py.normal.ContainsKey("ping") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["ping"]))))
            {
                int num2 = TerminalPatch.CheckForPlayerNameCommand(array[0], array[1]);
                if (num2 != -1)
                {
                    StartOfRound.Instance.mapScreen.PingRadarBooster(num2);
                    __result = __instance.terminalNodes.specialNodes[21];
                    return;
                }
            }
            else if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseChinese.Value && TranslateConfig.cmd_zh.normal.ContainsKey("flash") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["flash"])) || (TranslatePlugin.TerimalCanUsePinyinAbbreviation.Value && TranslateConfig.cmd_py.normal.ContainsKey("flash") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["flash"]))))
            {
                int num3 = TerminalPatch.CheckForPlayerNameCommand(array[0], array[1]);
                if (num3 != -1)
                {
                    StartOfRound.Instance.mapScreen.FlashRadarBooster(num3);
                    __result = __instance.terminalNodes.specialNodes[23];
                    return;
                }
                if (StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                {
                    StartOfRound.Instance.mapScreen.FlashRadarBooster(StartOfRound.Instance.mapScreen.targetTransformIndex);
                    __result = __instance.terminalNodes.specialNodes[23];
                }
            }
        }

        private static bool HaveChineseCMD(string name)
        {
            return TranslateConfig.cmd_zh.normal.ContainsKey(name);
        }

        private static bool HavePinyinCMD(string name)
        {
            return TranslateConfig.cmd_py.normal.ContainsKey(name);
        }

        private static string getChineseCMD(string name)
        {
            string text;
            if (TranslatePlugin.TerimalCanUseChinese.Value && TranslateConfig.cmd_zh.normal.ContainsKey(name))
            {
                text = TranslateConfig.cmd_zh.normal[name];
            }
            else
            {
                text = "";
            }
            return text;
        }

        private static string getPinyinCMD(string name)
        {
            string text;
            if (TranslatePlugin.TerimalCanUsePinyinAbbreviation.Value && TranslateConfig.cmd_py.normal.ContainsKey(name))
            {
                text = TranslateConfig.cmd_py.normal[name];
            }
            else
            {
                text = "";
            }
            return text;
        }

        private static string getOrginByChineseCMD(string name)
        {
            string text;
            if (TranslateConfig.cmd_zh.special.ContainsKey(name))
            {
                text = TranslateConfig.cmd_zh.special[name];
            }
            else
            {
                text = name;
            }
            return text;
        }

        private static string getOrginByPinyinCMD(string name)
        {
            string text;
            if (TranslateConfig.cmd_py.special.ContainsKey(name))
            {
                text = TranslateConfig.cmd_py.special[name];
            }
            else
            {
                text = name;
            }
            return text;
        }

        private static T TransReflection<T>(T tIn)
        {
            T t = Activator.CreateInstance<T>();
            Type type = typeof(T);
            foreach (FieldInfo fieldInfo in type.GetRuntimeFields())
            {
                type.GetRuntimeField(fieldInfo.Name).SetValue(t, fieldInfo.GetValue(tIn));
            }
            return t;
        }

        public static TerminalKeyword Copy(TerminalKeyword oldOne)
        {
            return TerminalPatch.TransReflection<TerminalKeyword>(oldOne);
        }

        [HarmonyPostfix]
        [HarmonyPatch("LoadNewNode")]
        private static void changeNewNodeText(Terminal __instance, TerminalNode node)
        {
            if (TerminalPatch.info != null)
            {
                TerminalPatch.info.Reset(__instance.screenText.text);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnSubmit")]
        private static void changeSubmit(Terminal __instance)
        {
            if (TerminalPatch.info != null)
            {
                TerminalPatch.texdAdded = __instance.currentText.Length - TerminalPatch.info.OriginalText.Length;
                if (TerminalPatch.texdAdded != 0)
                {
                    TerminalPatch.info.Reset(__instance.currentText);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void changeUpdateText(Terminal __instance)
        {
            try
            {
                if ((TerminalPatch.info == null || !TerminalPatch.info.IsTranslated) && TerminalPatch.info != null && !TerminalPatch.info.IsTranslated && TranslatePlugin.shouldTranslateTerimal.Value)
                {
                    if (TranslatePlugin.showAvailableText.Value && !string.IsNullOrEmpty(__instance.currentText) &&
                        GameTranslator.Patches.Utils.TextTranslate.ShouldOutputDebug($"terminal:{__instance.currentText}"))
                    {
                        TranslatePlugin.LogInfo($"[Debug] Terminal available text: '{__instance.currentText}'");
                    }

                    string text = TranslateConfig.replaceByMap(__instance.currentText, TranslateConfig.terminal);
                    TerminalPatch.info.SetTranslatedText(text);
                    TerminalPatch.SetText(TerminalPatch.info.TranslatedText, __instance);
                    TerminalPatch.info.OriginalText = __instance.currentText;
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogWarning(ex);
            }
        }

        public static void SetText(string text, Terminal Instance)
        {
            if (!(Instance == null))
            {
                TerminalPatch.modifyingText.SetValue(Instance, true);
                Instance.screenText.interactable = true;
                Instance.screenText.text = text;
                Instance.currentText = Instance.screenText.text;
                if (Instance.screenText.verticalScrollbar != null)
                {
                    Instance.screenText.verticalScrollbar.value = 0f;
                }
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void startTerminal(Terminal __instance)
        {
            TerminalPatch.info = __instance.screenText.GetOrCreateTextTranslationInfo();
            TerminalPatch.ig.Clear();
            foreach (FieldInfo fieldInfo in __instance.GetType().GetRuntimeFields())
            {
                if (fieldInfo.GetValue(__instance) != null && UnityTypes.TMP_Text.IsAssignableFrom(fieldInfo.GetValue(__instance).GetType()))
                {
                    TerminalPatch.ig.Add(fieldInfo.GetValue(__instance));
                }
            }
            TerminalPatch.info.MustIgnore = true;
        }

        public static FieldInfo hasGottenVerb = typeof(Terminal).GetField("hasGottenVerb", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo hasGottenNoun = typeof(Terminal).GetField("hasGottenNoun", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static bool shouldTranslate = false;

        public static bool noText = false;

        public static int texdAdded = 0;

        public static FieldInfo modifyingText = typeof(Terminal).GetField("modifyingText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

        public static TMP_InputField screenText = null;

        public static TextTranslationInfo info;

        public static HashSet<object> ig = new HashSet<object>();

        public class Translator
        {
            public TerminalKeyword getTranslateKey(TerminalKeyword old, bool useC)
            {
                TerminalKeyword terminalKeyword;
                if (!this.keyValuePairs.ContainsKey(old.name + old.word))
                {
                    if ((useC && !TerminalPatch.HaveChineseCMD(old.word)) || (!useC && !TerminalPatch.HavePinyinCMD(old.word)))
                    {
                        this.keyValuePairs.Add(old.name + old.word, old);
                        terminalKeyword = old;
                    }
                    else
                    {
                        string text = (useC ? TerminalPatch.getChineseCMD(old.word) : TerminalPatch.getPinyinCMD(old.word));
                        if (text == old.word)
                        {
                            this.keyValuePairs.Add(old.name + old.word, old);
                            terminalKeyword = old;
                        }
                        else
                        {
                            TerminalKeyword terminalKeyword2 = TerminalPatch.Copy(old);
                            terminalKeyword2.word = text;
                            this.keyValuePairs.Add(old.name + old.word, terminalKeyword2);
                            if (terminalKeyword2.compatibleNouns != null)
                            {
                                List<CompatibleNoun> list = terminalKeyword2.compatibleNouns.ToList<CompatibleNoun>();
                                foreach (CompatibleNoun compatibleNoun in terminalKeyword2.compatibleNouns)
                                {
                                    if (!this.getTranslateKey(compatibleNoun.noun, useC).word.Equals(compatibleNoun.noun.word))
                                    {
                                        CompatibleNoun compatibleNoun2 = TerminalPatch.TransReflection<CompatibleNoun>(compatibleNoun);
                                        compatibleNoun2.noun = this.getTranslateKey(compatibleNoun.noun, useC);
                                        list.Add(compatibleNoun2);
                                    }
                                }
                                terminalKeyword2.compatibleNouns = list.ToArray();
                            }
                            if (terminalKeyword2.defaultVerb != null && terminalKeyword2.defaultVerb.compatibleNouns != null)
                            {
                                List<CompatibleNoun> list2 = terminalKeyword2.defaultVerb.compatibleNouns.ToList<CompatibleNoun>();
                                foreach (CompatibleNoun compatibleNoun3 in terminalKeyword2.defaultVerb.compatibleNouns)
                                {
                                    if (!this.getTranslateKey(compatibleNoun3.noun, useC).word.Equals(compatibleNoun3.noun.word))
                                    {
                                        CompatibleNoun compatibleNoun4 = TerminalPatch.TransReflection<CompatibleNoun>(compatibleNoun3);
                                        compatibleNoun4.noun = this.getTranslateKey(compatibleNoun3.noun, useC);
                                        list2.Add(compatibleNoun4);
                                    }
                                }
                                terminalKeyword2.defaultVerb.compatibleNouns = list2.ToArray();
                            }
                            terminalKeyword = terminalKeyword2;
                        }
                    }
                }
                else
                {
                    terminalKeyword = this.keyValuePairs[old.name + old.word];
                }
                return terminalKeyword;
            }

            public void getTranslateKey(TerminalKeyword old)
            {
                if (!this.keys.ContainsKey(old.word))
                {
                    this.keys.Add(old.word, old.word);
                    if (old.compatibleNouns != null)
                    {
                        old.compatibleNouns.ToList<CompatibleNoun>();
                        foreach (CompatibleNoun compatibleNoun in old.compatibleNouns)
                        {
                            this.getTranslateKey(compatibleNoun.noun);
                        }
                    }
                    if (old.defaultVerb != null && old.defaultVerb.compatibleNouns != null)
                    {
                        old.defaultVerb.compatibleNouns.ToList<CompatibleNoun>();
                        foreach (CompatibleNoun compatibleNoun2 in old.defaultVerb.compatibleNouns)
                        {
                            this.getTranslateKey(compatibleNoun2.noun);
                        }
                    }
                }
            }

            public Dictionary<string, TerminalKeyword> keyValuePairs = new Dictionary<string, TerminalKeyword>();

            public Dictionary<string, string> keys = new Dictionary<string, string>();
        }
    }
}
