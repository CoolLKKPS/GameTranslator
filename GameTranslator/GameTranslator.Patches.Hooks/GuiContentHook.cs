using GameTranslator.Patches.Translatons;
using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(GUIContent))]
    internal class GuiContentHook
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, new Type[]
        {
            typeof(string),
            typeof(Texture),
            typeof(string)
        })]
        public static void Init(ref GUIContent __instance, ref string text, Texture image, string tooltip)
        {
            GuiContentHook.Hook_TextChanged(TextTranslate.Instance, __instance, ref text, TranslateConfig.guiText, TranslateConfig.gui);
        }

        [HarmonyPrefix]
        [HarmonyPatch("text", MethodType.Setter)]
        public static void Change(ref GUIContent __instance, ref string value)
        {
            GuiContentHook.Hook_TextChanged(TextTranslate.Instance, __instance, ref value, TranslateConfig.guiText, TranslateConfig.gui);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Temp", new Type[] { typeof(string) })]
        public static void Temp(ref string t)
        {
            GuiContentHook.Hook_TextChanged(TextTranslate.Instance, (GUIContent)GuiContentHook.s_Text.GetValue(typeof(GUIContent)), ref t, TranslateConfig.guiText, TranslateConfig.gui);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Temp", new Type[]
        {
            typeof(string),
            typeof(string)
        })]
        public static void Temp(ref string t, string tooltip)
        {
            GuiContentHook.Hook_TextChanged(TextTranslate.Instance, (GUIContent)GuiContentHook.s_Text.GetValue(typeof(GUIContent)), ref t, TranslateConfig.guiText, TranslateConfig.gui);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Temp", new Type[]
        {
            typeof(string),
            typeof(Texture)
        })]
        public static void Temp(ref string t, Texture i)
        {
            GuiContentHook.Hook_TextChanged(TextTranslate.Instance, (GUIContent)GuiContentHook.s_TextImage.GetValue(typeof(GUIContent)), ref t, TranslateConfig.guiText, TranslateConfig.gui);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Temp", new Type[] { typeof(string[]) })]
        public static void Temp(ref string[] texts)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                GuiContentHook.Hook_TextChanged(TextTranslate.Instance, (GUIContent)GuiContentHook.s_Text.GetValue(typeof(GUIContent)), ref texts[i], TranslateConfig.guiText, TranslateConfig.gui);
            }
        }

        internal static void Hook_TextChanged(TextTranslate textTranslate, object ui, ref string value, NormalTextTranslator normalText, TranslateConfig.TranslateConfigFile config)
        {
            if (TranslatePlugin.shouldTranslateGui.Value)
            {
                if (value != null && value.Length <= TranslatePlugin.syncTranslationThreshold.Value)
                {
                    TextTranslationInfo orCreateTextTranslationInfo = ui.GetOrCreateTextTranslationInfo();
                    bool flag = textTranslate.DiscoverComponent(ui, orCreateTextTranslationInfo);
                    var translatedText = textTranslate.TranslateImmediate(ui, value, orCreateTextTranslationInfo, normalText, config, flag);
                    if (translatedText != null)
                    {
                        value = translatedText;
                    }
                }
                else
                {
                    TextTranslationInfo orCreateTextTranslationInfo = ui.GetOrCreateTextTranslationInfo();
                    bool flag = textTranslate.DiscoverComponent(ui, orCreateTextTranslationInfo);
                    var translatedText = textTranslate.TranslateOrQueue(ui, value, orCreateTextTranslationInfo, normalText, config, flag);
                    if (translatedText != null)
                    {
                        value = translatedText;
                    }
                }
            }
        }

        public static FieldInfo s_Text = typeof(GUIContent).GetField("s_Text", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo s_TextImage = typeof(GUIContent).GetField("s_TextImage", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
    }
}
