using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using TMPro;

namespace GameTranslator.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("AddChatMessage")]
        private static void changeChatMessage(HUDManager __instance, string chatMessage, string nameOfUserWhoTyped)
        {
            if (!TranslatePlugin.shouldTranslateHUD.Value)
            {
                return;
            }
            if (__instance == null)
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
                TextTranslate.Instance.OnComponentTextChanged(chatText, TranslateConfig.hudText, TranslateConfig.hud);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error in changeChatMessage: " + ex.Message);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DisplayTip")]
        private static void changeTip(HUDManager __instance)
        {
            if (!TranslatePlugin.shouldTranslateHUD.Value)
            {
                return;
            }
            if (__instance == null)
            {
                return;
            }
            try
            {
                TextTranslate.Instance.OnComponentTextChanged(__instance.tipsPanelHeader, TranslateConfig.hudText, TranslateConfig.hud);
                TextTranslate.Instance.OnComponentTextChanged(__instance.tipsPanelBody, TranslateConfig.hudText, TranslateConfig.hud);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error in changeTip: " + ex.Message);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DisplayGlobalNotification")]
        private static void changeNotification(HUDManager __instance)
        {
            if (!TranslatePlugin.shouldTranslateHUD.Value)
            {
                return;
            }
            if (__instance == null)
            {
                return;
            }
            try
            {
                TextTranslate.Instance.OnComponentTextChanged(__instance.globalNotificationText, TranslateConfig.hudText, TranslateConfig.hud);
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger.LogError("Error in changeNotification: " + ex.Message);
            }
        }
    }
}
