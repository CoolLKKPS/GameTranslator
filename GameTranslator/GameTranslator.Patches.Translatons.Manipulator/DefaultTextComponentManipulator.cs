using System;
using System.Reflection;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons.Manipulator
{
    internal class DefaultTextComponentManipulator : ITextComponentManipulator
    {
        public DefaultTextComponentManipulator(Type type)
        {
            this._type = type;
            if (type.GetProperty(DefaultTextComponentManipulator.TextPropertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
            {
                this._property = type.CachedProperty(DefaultTextComponentManipulator.TextPropertyName);
                return;
            }
            this._property = type.CachedProperty("Text");
        }

        public string GetText(object ui)
        {
            CachedProperty property = this._property;
            return (string)((property != null) ? property.Get(ui) : null);
        }

        public void SetText(object ui, string text)
        {
            try
            {
                Type type = this._type;
                if (UnityTypes.TextWindow != null)
                {
                    TypeContainer textMeshPro = UnityTypes.TextMeshPro;
                    if (textMeshPro != null && textMeshPro.ClrType.IsAssignableFrom(type))
                    {
                        global::UnityEngine.Object textWindow = global::UnityEngine.Object.FindObjectOfType(UnityTypes.TextWindow.ClrType);
                        if (textWindow != null)
                        {
                            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                            FieldInfo field = textWindow.GetType().GetField("TextMesh", flags);
                            object obj = ((field != null) ? field.GetValue(textWindow) : null);
                            if (obj != null && object.Equals(obj, ui))
                            {
                                CachedProperty cachedProperty = type.CachedProperty(DefaultTextComponentManipulator.TextPropertyName);
                                if (cachedProperty != null)
                                {
                                    cachedProperty.Set(ui, text);
                                }
                                object value = textWindow.GetType().GetField("curText", flags).GetValue(textWindow);
                                textWindow.GetType().GetField("curText", flags).SetValue(textWindow, text);
                                textWindow.GetType().GetMethod("FinishTyping", flags).Invoke(textWindow, null);
                                textWindow.GetType().GetField("curText", flags).SetValue(textWindow, value);
                                object value2 = textWindow.GetType().GetField("Keyword", flags).GetValue(textWindow);
                                value2.GetType().GetMethod("UpdateTextMesh", flags).Invoke(value2, new object[] { obj, true });
                                return;
                            }
                        }
                    }
                }
                CachedProperty property = this._property;
                if (property != null)
                {
                    property.Set(ui, text);
                }
                CachedProperty cachedProperty2 = type.CachedProperty("maxVisibleCharacters");
                if (cachedProperty2 != null && cachedProperty2.PropertyType == typeof(int))
                {
                    int num = (int)cachedProperty2.Get(ui);
                    if (0 < num && num < 99999)
                    {
                        cachedProperty2.Set(ui, 99999);
                    }
                }
                if (UnityTypes.TextExpansion_Methods.SetMessageType != null && UnityTypes.TextExpansion_Methods.SkipTypeWriter != null && UnityTypes.TextExpansion.ClrType.IsAssignableFrom(type))
                {
                    UnityTypes.TextExpansion_Methods.SetMessageType.Invoke(ui, 1);
                    UnityTypes.TextExpansion_Methods.SkipTypeWriter.Invoke(ui);
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                TranslatePlugin.logger.LogError($"IndexOutOfRangeException in DefaultTextComponentManipulator.SetText: {ex.Message}");
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.Exception ex)
            {
                TranslatePlugin.logger.LogError($"Exception in DefaultTextComponentManipulator.SetText: {ex.Message}");
            }
        }

        private static readonly string TextPropertyName = "text";

        private readonly Type _type;

        private readonly CachedProperty _property;
    }
}
