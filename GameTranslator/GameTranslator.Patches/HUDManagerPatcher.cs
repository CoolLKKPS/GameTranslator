using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace GameTranslator.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void start(ref HUDManager __instance)
        {
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void update(HUDManager __instance)
        {
            if (TranslatePlugin.shouldTranslateHUD.Value)
            {
                string currentChatText = __instance.chatText.text;

                if (HUDManagerPatcher.lastChat.Length != currentChatText.Length)
                {
                    string translatedText = TranslateConfig.hudText.TryTranslate(currentChatText);
                    if (!string.IsNullOrEmpty(translatedText) && translatedText != currentChatText)
                    {
                        __instance.chatText.text = translatedText;
                    }
                }

                HUDManagerPatcher.lastChat = currentChatText;
            }
        }

        private static void fadeText(TextMeshProUGUI text, ref bool fade, float duration, float newAlpha)
        {
            float a = text.color.a;
            float num = 0f;
            fade = true;
            while (num < duration)
            {
                num += Time.deltaTime;
                float num2 = Mathf.Lerp(a, newAlpha, num / duration);
                text.color = new Color(text.color.r, text.color.g, text.color.b, num2);
            }
            text.enabled = false;
            fade = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("AddChatMessage")]
        private static void changeChatMessage(HUDManager __instance, string chatMessage, string nameOfUserWhoTyped)
        {
            if (!TranslatePlugin.shouldTranslateHUD.Value)
            {
                return;
            }
            if (__instance == null || __instance.Equals(null))
            {
                return;
            }
            TMP_Text chatText = __instance.chatText;
            if (chatText == null || chatText.Equals(null) || !chatText.gameObject.activeInHierarchy)
            {
                return;
            }
            try
            {
                string text = chatText.text;
                if (!string.IsNullOrEmpty(text))
                {
                    string text2 = TranslateConfig.hudText.TryTranslate(text);
                    if (!string.IsNullOrEmpty(text2) && !text2.Equals(text))
                    {
                        chatText.text = text2;
                    }
                }
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error in changeChatMessage: " + ex.Message);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("DisplayTip")]
        [HarmonyPriority(2147483647)]
        private static bool changeTip(HUDManager __instance, ref string headerText, ref string bodyText, bool isWarning = false, bool useSave = false, string prefsKey = "LC_Tip1")
        {
            if (TranslatePlugin.shouldTranslateHUD.Value)
            {
                headerText = TranslateConfig.hudText.TryTranslate(headerText);
                bodyText = TranslateConfig.hudText.TryTranslate(bodyText);
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DisplayGlobalNotification")]
        [HarmonyPriority(2147483647)]
        private static bool changeNotification(HUDManager __instance, ref string displayText)
        {
            if (TranslatePlugin.shouldTranslateHUD.Value)
            {
                displayText = TranslateConfig.hudText.TryTranslate(displayText);
            }
            return true;
        }

        public static FieldInfo scanNodes = typeof(HUDManager).GetField("scanNodes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo nodesOnScreen = typeof(HUDManager).GetField("nodesOnScreen", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static Dictionary<string, string> originToTranslated = new Dictionary<string, string>();

        public static HashSet<string> translatedItems = new HashSet<string>();

        private static string lastChat = "";

        private static readonly BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static MethodInfo CanTipDisplay = typeof(HUDManager).GetMethod("CanTipDisplay", HUDManagerPatcher.All);

        private static MethodInfo StopCoroutine = typeof(HUDManager).GetMethod("StopCoroutine", HUDManagerPatcher.All, null, new Type[] { typeof(IEnumerator) }, null);

        private static MethodInfo StartCoroutine = typeof(HUDManager).GetMethod("StartCoroutine", HUDManagerPatcher.All, null, new Type[] { typeof(IEnumerator) }, null);

        private static MethodInfo TipsPanelTimer = typeof(HUDManager).GetMethod("TipsPanelTimer", HUDManagerPatcher.All);

        private static FieldInfo tipsPanelCoroutine = typeof(HUDManager).GetField("tipsPanelCoroutine", HUDManagerPatcher.All);
    }
}
