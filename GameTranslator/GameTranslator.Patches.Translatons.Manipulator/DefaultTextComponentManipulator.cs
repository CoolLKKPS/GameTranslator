using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                                if (new StackTrace().GetFrames().Any((StackFrame x) => x.GetMethod().DeclaringType == UnityTypes.TextWindow.ClrType))
                                {
                                    object previousCurText = textWindow.GetType().GetField("curText", flags).GetValue(textWindow);
                                    textWindow.GetType().GetField("curText", flags).SetValue(textWindow, text);
                                    DefaultTextComponentManipulator.SetCurText = delegate (object textWindowInner)
                                    {
                                        object value3 = textWindow.GetType().GetField("curText", flags).GetValue(textWindow);
                                        if (object.Equals(text, value3))
                                        {
                                            textWindowInner.GetType().GetMethod("FinishTyping", flags).Invoke(textWindowInner, null);
                                            textWindowInner.GetType().GetField("curText", flags).SetValue(textWindowInner, previousCurText);
                                            object value4 = textWindowInner.GetType().GetField("TextMesh", flags).GetValue(textWindowInner);
                                            object value5 = textWindowInner.GetType().GetField("Keyword", flags).GetValue(textWindowInner);
                                            value5.GetType().GetMethod("UpdateTextMesh", flags).Invoke(value5, new object[] { value4, true });
                                        }
                                        DefaultTextComponentManipulator.SetCurText = null;
                                    };
                                    return;
                                }
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
                BepInEx.Logging.ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("GameTranslator");
                logger.LogError($"IndexOutOfRangeException in DefaultTextComponentManipulator.SetText: {ex.Message}");
            }
            catch (System.NullReferenceException)
            {
            }
            catch (System.Exception ex)
            {
                BepInEx.Logging.ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("GameTranslator");
                logger.LogError($"Exception in DefaultTextComponentManipulator.SetText: {ex.Message}");
            }
        }

        private static DefaultTextComponentManipulator.TypeAndMethod GetTextPropertySetterInParent(Type type)
        {
            Type type2 = type.BaseType;
            while (type2 != null)
            {
                DefaultTextComponentManipulator.TypeAndMethod typeAndMethod;
                DefaultTextComponentManipulator.TypeAndMethod typeAndMethod2;
                if (DefaultTextComponentManipulator._textSetters.TryGetValue(type, out typeAndMethod))
                {
                    typeAndMethod2 = typeAndMethod;
                }
                else
                {
                    PropertyInfo property = type2.GetProperty(DefaultTextComponentManipulator.TextPropertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (!(property != null) || !property.CanWrite)
                    {
                        type2 = type2.BaseType;
                        continue;
                    }
                    DefaultTextComponentManipulator.TypeAndMethod typeAndMethod3 = new DefaultTextComponentManipulator.TypeAndMethod(type2, property.GetSetMethod());
                    DefaultTextComponentManipulator._textSetters[type2] = typeAndMethod3;
                    typeAndMethod2 = typeAndMethod3;
                }
                return typeAndMethod2;
            }
            return null;
        }

        public static Action<object> SetCurText = null;

        private static readonly string TextPropertyName = "text";

        private readonly Type _type;

        private readonly CachedProperty _property;

        private static Dictionary<Type, DefaultTextComponentManipulator.TypeAndMethod> _textSetters = new Dictionary<Type, DefaultTextComponentManipulator.TypeAndMethod>();

        private class TypeAndMethod
        {
            public TypeAndMethod(Type type, MethodBase method)
            {
                this.Type = type;
                this.SetterMethod = method;
            }

            public Type Type { get; }

            public MethodBase SetterMethod { get; }

            public FastReflectionDelegate SetterInvoker
            {
                get
                {
                    FastReflectionDelegate fastReflectionDelegate;
                    if ((fastReflectionDelegate = this._setterInvoker) == null)
                    {
                        fastReflectionDelegate = (this._setterInvoker = this.SetterMethod.CreateFastDelegate(true, true));
                    }
                    return fastReflectionDelegate;
                }
            }

            private FastReflectionDelegate _setterInvoker;
        }
    }
}
