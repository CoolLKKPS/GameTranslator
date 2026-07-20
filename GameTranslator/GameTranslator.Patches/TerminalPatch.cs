using GameTranslator.Patches.Translatons;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
                    if (TerminalPatch.GetCmd(options[i].noun.word, true).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()) ||
                        TerminalPatch.GetCmd(options[i].noun.word, false).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()))
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
                if (TerminalPatch.GetCmd(__instance.terminalNodes.allKeywords[i].word, true).EqualsIgnoreCase(playerWord) ||
                    TerminalPatch.GetCmd(__instance.terminalNodes.allKeywords[i].word, false).EqualsIgnoreCase(playerWord))
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
                if (TerminalPatch.GetCmd(array[i].objectCode, true).EqualsIgnoreCase(word) || TerminalPatch.GetCmd(array[i].objectCode, false).EqualsIgnoreCase(word))
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
            if (TranslatePlugin.TerimalCanUseShortCutOne.Value || TranslatePlugin.TerimalCanUseShortCutTwo.Value)
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
                        if (TerminalPatch.GetCmd(__instance.terminalNodes.allKeywords[i].word, true).EqualsIgnoreCase(playerWord) ||
                            TerminalPatch.GetCmd(__instance.terminalNodes.allKeywords[i].word, false).EqualsIgnoreCase(playerWord))
                        {
                            __result = __instance.terminalNodes.allKeywords[i];
                            return;
                        }
                        if (terminalKeyword == null)
                        {
                            for (int j = playerWord.Length; j > specificityRequired; j--)
                            {
                                if (TerminalPatch.GetCmd(__instance.terminalNodes.allKeywords[i].word, true).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()) ||
                                    TerminalPatch.GetCmd(__instance.terminalNodes.allKeywords[i].word, false).ToLower().StartsWith(playerWord.Substring(0, j).ToLower()))
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

        // Lethal Company Special Terminal Commands
        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        private static void customParser(Terminal __instance, ref TerminalNode __result)
        {
            string[] array = TerminalPatch.RemovePunctuation(__instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded)).Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseShortCutOne.Value && TranslateConfig.cmd_zh.normal.ContainsKey("transmit") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["transmit"])) || (TranslatePlugin.TerimalCanUseShortCutTwo.Value && TranslateConfig.cmd_py.normal.ContainsKey("transmit") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["transmit"]))))
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
            if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseShortCutOne.Value && TranslateConfig.cmd_zh.normal.ContainsKey("switch") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["switch"])) || (TranslatePlugin.TerimalCanUseShortCutTwo.Value && TranslateConfig.cmd_py.normal.ContainsKey("switch") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["switch"]))))
            {
                int num = TerminalPatch.CheckForPlayerNameCommand(array[0], array[1]);
                if (num != -1)
                {
                    StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(num);
                    __result = __instance.terminalNodes.specialNodes[20];
                    return;
                }
            }
            else if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseShortCutOne.Value && TranslateConfig.cmd_zh.normal.ContainsKey("ping") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["ping"])) || (TranslatePlugin.TerimalCanUseShortCutTwo.Value && TranslateConfig.cmd_py.normal.ContainsKey("ping") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["ping"]))))
            {
                int num2 = TerminalPatch.CheckForPlayerNameCommand(array[0], array[1]);
                if (num2 != -1)
                {
                    StartOfRound.Instance.mapScreen.PingRadarBooster(num2);
                    __result = __instance.terminalNodes.specialNodes[21];
                    return;
                }
            }
            else if (array.Length > 1 && ((TranslatePlugin.TerimalCanUseShortCutOne.Value && TranslateConfig.cmd_zh.normal.ContainsKey("flash") && array[0].ToLower().Equals(TranslateConfig.cmd_zh.normal["flash"])) || (TranslatePlugin.TerimalCanUseShortCutTwo.Value && TranslateConfig.cmd_py.normal.ContainsKey("flash") && array[0].ToLower().Equals(TranslateConfig.cmd_py.normal["flash"]))))
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

        private static string GetCmd(string name, bool useC)
        {
            if (useC)
            {
                if (TranslatePlugin.TerimalCanUseShortCutOne.Value && TranslateConfig.cmd_zh.normal.ContainsKey(name))
                    return TranslateConfig.cmd_zh.normal[name];
            }
            else
            {
                if (TranslatePlugin.TerimalCanUseShortCutTwo.Value && TranslateConfig.cmd_py.normal.ContainsKey(name))
                    return TranslateConfig.cmd_py.normal[name];
            }
            return "";
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
                if (TerminalPatch.info != null && TranslatePlugin.shouldTranslateTerimal.Value)
                {
                    if (!TerminalPatch.info.IsTranslated)
                    {
                        if (TranslatePlugin.showAvailableText.Value && !string.IsNullOrEmpty(__instance.currentText) &&
                            GameTranslator.Patches.Utils.TextTranslate.ShouldOutputDebug($"terminal:{__instance.currentText}"))
                        {
                            TranslatePlugin.logger.LogInfo($"[Debug] Terminal available text: '{__instance.currentText}'");
                        }

                        string text = TranslateConfig.replaceByMap(__instance.currentText, TranslateConfig.terminal);
                        TerminalPatch.info.SetTranslatedText(text);
                        TerminalPatch.SetText(TerminalPatch.info.TranslatedText, __instance);
                        TerminalPatch.info.OriginalText = __instance.currentText;
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogWarning(ex);
            }
        }

        private static void SetText(string text, Terminal Instance)
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

        private static TextTranslationInfo info;

        public static HashSet<object> ig = new HashSet<object>();

        private static int texdAdded = 0;

        private static FieldInfo hasGottenVerb = typeof(Terminal).GetField("hasGottenVerb", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        private static FieldInfo modifyingText = typeof(Terminal).GetField("modifyingText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
    }
}
