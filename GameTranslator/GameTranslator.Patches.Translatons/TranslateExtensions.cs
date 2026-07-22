using GameTranslator.Patches.Translatons.Manipulator;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Extensions;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons
{
    internal static class TranslateExtensions
    {
        public static Type GetUnityType(this object obj)
        {
            return obj.GetType();
        }

        public static bool EqualsIgnoreCase(this string value, string other)
        {
            return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
        }

        public static Texture2D GetTexture(this object ui)
        {
            Texture2D texture2D;
            SpriteRenderer spriteRenderer;
            if (ui == null)
            {
                texture2D = null;
            }
            else if (!ui.TryCastTo(out spriteRenderer))
            {
                Type type = ui.GetType();
                CachedProperty cachedProperty = type.CachedProperty(TranslateExtensions.MainTexturePropertyName);
                object obj;
                if ((obj = ((cachedProperty != null) ? cachedProperty.Get(ui) : null)) == null)
                {
                    CachedProperty cachedProperty2 = type.CachedProperty(TranslateExtensions.TexturePropertyName);
                    if ((obj = ((cachedProperty2 != null) ? cachedProperty2.Get(ui) : null)) == null)
                    {
                        CachedProperty cachedProperty3 = type.CachedProperty(TranslateExtensions.CapitalMainTexturePropertyName);
                        obj = ((cachedProperty3 != null) ? cachedProperty3.Get(ui) : null);
                    }
                }
                texture2D = obj as Texture2D;
            }
            else
            {
                Sprite sprite = spriteRenderer.sprite;
                if (sprite == null)
                {
                    texture2D = null;
                }
                else
                {
                    texture2D = sprite.texture;
                }
            }
            return texture2D;
        }

        public static string GetTextureName(this object texture, string fallbackName)
        {
            Texture2D texture2D;
            if (texture.TryCastTo(out texture2D))
            {
                string name = texture2D.name;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            return fallbackName;
        }

        private static byte[] EncodeToPNGEx(Texture2D texture)
        {
            byte[] array;
            if (UnityTypes.ImageConversion_Methods.EncodeToPNG != null)
            {
                array = UnityTypes.ImageConversion_Methods.EncodeToPNG(texture);
            }
            else
            {
                if (UnityTypes.Texture2D_Methods.EncodeToPNG == null)
                {
                    throw new NotSupportedException("No way to encode the texture to PNG.");
                }
                array = UnityTypes.Texture2D_Methods.EncodeToPNG(texture);
            }
            return array;
        }

        public static bool IsComponentActive(this object ui)
        {
            Component component = ui as Component;
            if (component != null && component)
            {
                GameObject gameObject = component.gameObject;
                if (gameObject)
                {
                    Behaviour behaviour = component as Behaviour;
                    if (behaviour != null)
                    {
                        return gameObject.activeInHierarchy && behaviour.enabled;
                    }
                    return gameObject.activeInHierarchy;
                }
            }
            return true;
        }

        public static TextureDataResult GetTextureData(this Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
            Graphics.Blit(texture, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D texture2D = new Texture2D(width, height);
            texture2D.ReadPixels(new Rect(0f, 0f, (float)temporary.width, (float)temporary.height), 0, 0);
            byte[] array = TranslateExtensions.EncodeToPNGEx(texture2D);
            global::UnityEngine.Object.DestroyImmediate(texture2D);
            RenderTexture.active = ((active == temporary) ? null : active);
            RenderTexture.ReleaseTemporary(temporary);
            return new TextureDataResult(array);
        }

        public static bool IsCompatible(this Texture2D texture, TranslateExtensions.ImageFormat dataType)
        {
            var format = texture.format;
            return dataType == TranslateExtensions.ImageFormat.PNG
                || (dataType == TranslateExtensions.ImageFormat.TGA && (format == TextureFormat.ARGB32 || format == TextureFormat.RGBA32 || format == TextureFormat.RGB24));
        }

        public static bool IsKnownImageType(this object ui)
        {
            bool flag;
            if (ui == null)
            {
                flag = false;
            }
            else
            {
                Type unityType = ui.GetUnityType();
                Material material;
                SpriteRenderer spriteRenderer;
                if (!ui.TryCastTo(out material) && !ui.TryCastTo(out spriteRenderer) && (UnityTypes.Image == null || !UnityTypes.Image.IsAssignableFrom(unityType)) && (UnityTypes.RawImage == null || !UnityTypes.RawImage.IsAssignableFrom(unityType)) && (UnityTypes.CubismRenderer == null || !UnityTypes.CubismRenderer.IsAssignableFrom(unityType)))
                {
                    if (UnityTypes.UIWidget != null)
                    {
                        object obj = unityType;
                        TypeContainer uilabel = UnityTypes.UILabel;
                        if (!object.Equals(obj, (uilabel != null) ? uilabel.UnityType : null) && UnityTypes.UIWidget.IsAssignableFrom(unityType))
                        {
                            return true;
                        }
                    }
                    if ((UnityTypes.UIAtlas == null || !UnityTypes.UIAtlas.IsAssignableFrom(unityType)) && (UnityTypes.UITexture == null || !UnityTypes.UITexture.IsAssignableFrom(unityType)))
                    {
                        return UnityTypes.UIPanel != null && UnityTypes.UIPanel.IsAssignableFrom(unityType);
                    }
                }
                flag = true;
            }
            return flag;
        }

        public static TextureTranslationInfo GetOrCreateTextureTranslationInfo(this Texture2D texture)
        {
            TextureTranslationInfo orCreateExtensionData = texture.GetOrCreateExtensionData<TextureTranslationInfo>();
            orCreateExtensionData.Initialize(texture);
            return orCreateExtensionData;
        }

        public static ITextComponentManipulator GetTextManipulator(this object ui)
        {
            ITextComponentManipulator textComponentManipulator;
            if (ui == null)
            {
                textComponentManipulator = null;
            }
            else
            {
                Type unityType = ui.GetUnityType();
                ITextComponentManipulator textComponentManipulator2;
                if (!TranslateExtensions.Manipulators.TryGetValue(unityType, out textComponentManipulator2))
                {
                    if (UnityTypes.TextField != null && UnityTypes.TextField.IsAssignableFrom(unityType))
                    {
                        textComponentManipulator2 = new FairyGUITextComponentManipulator();
                    }
                    else if (UnityTypes.TextArea2D != null && UnityTypes.TextArea2D.IsAssignableFrom(unityType))
                    {
                        textComponentManipulator2 = new TextArea2DComponentManipulator();
                    }
                    else if (UnityTypes.UguiNovelText != null && UnityTypes.UguiNovelText.IsAssignableFrom(unityType))
                    {
                        textComponentManipulator2 = new UguiNovelTextComponentManipulator(ui.GetType());
                    }
                    else
                    {
                        textComponentManipulator2 = new DefaultTextComponentManipulator(ui.GetType());
                    }
                    TranslateExtensions.Manipulators[unityType] = textComponentManipulator2;
                }
                textComponentManipulator = textComponentManipulator2;
            }
            return textComponentManipulator;
        }

        public static bool ShouldIgnoreTextComponent(this object ui)
        {
            Component component = ui as Component;
            if (component == null || !component)
                return false;

            var tr = component.transform;
            if (tr.name.IndexOf("XUAIGNORE", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            while (tr.parent != null)
            {
                tr = tr.parent;
                if (tr.name.IndexOf("XUAIGNORETREE", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            GameObject gameObject = component.gameObject;
            Component component2;
            if (UnityTypes.InputField != null)
            {
                component2 = gameObject.GetFirstComponentInSelfOrAncestor(UnityTypes.InputField.UnityType);
                if (component2 != null && UnityTypes.InputField_Properties.Placeholder != null)
                {
                    Component component3 = (Component)UnityTypes.InputField_Properties.Placeholder.Get(component2);
                    return !UnityObjectReferenceComparer.Default.Equals(component3, component);
                }
            }
            if (UnityTypes.TMP_InputField != null)
            {
                component2 = gameObject.GetFirstComponentInSelfOrAncestor(UnityTypes.TMP_InputField.UnityType);
                if (component2 != null && UnityTypes.TMP_InputField_Properties.Placeholder != null)
                {
                    Component component4 = (Component)UnityTypes.TMP_InputField_Properties.Placeholder.Get(component2);
                    return !UnityObjectReferenceComparer.Default.Equals(component4, component);
                }
            }
            component2 = gameObject.GetFirstComponentInSelfOrAncestor(UnityTypes.UIInput?.UnityType);
            return component2 != null;
        }

        public static Component GetFirstComponentInSelfOrAncestor(this GameObject go, Type type)
        {
            Component component;
            if (type == null)
            {
                component = null;
            }
            else
            {
                GameObject gameObject = go;
                while (gameObject != null)
                {
                    Component component2 = gameObject.GetComponent(type);
                    if (component2 != null)
                    {
                        return component2;
                    }
                    Transform transform = gameObject.transform;
                    GameObject gameObject2;
                    if (transform == null)
                    {
                        gameObject2 = null;
                    }
                    else
                    {
                        Transform parent = transform.parent;
                        gameObject2 = ((parent != null) ? parent.gameObject : null);
                    }
                    gameObject = gameObject2;
                }
                component = null;
            }
            return component;
        }

        public static void Load()
        {
            TranslateExtensions.TexturePropertyMovers = new List<TranslateExtensions.IPropertyMover>();
            TranslateExtensions.LoadProperty<global::UnityEngine.Object, string>("name");
            TranslateExtensions.LoadProperty<Texture, int>("anisoLevel");
            TranslateExtensions.LoadProperty<Texture, FilterMode>("filterMode");
            TranslateExtensions.LoadProperty<Texture, float>("mipMapBias");
            TranslateExtensions.LoadProperty<Texture, TextureWrapMode>("wrapMode");
        }

        private static void LoadProperty<TObject, TPropertyType>(string propertyName)
        {
            PropertyInfo property = typeof(TObject).GetProperty(propertyName);
            if (property != null && property.CanWrite && property.CanRead)
            {
                TranslateExtensions.TexturePropertyMovers.Add(new TranslateExtensions.PropertyMover<TObject, TPropertyType>(property));
            }
        }

        public static void LoadImageEx(this Texture2D texture, byte[] data, TranslateExtensions.ImageFormat format, Texture2D originalTexture)
        {
            GameTranslator.Patches.Utils.Textures.TextureLoader.Load(texture, data, format);

            if (originalTexture != null)
            {
                foreach (var prop in TranslateExtensions.TexturePropertyMovers)
                {
                    prop.MoveProperty(originalTexture, texture);
                }
            }
        }

        public static TextTranslationInfo GetOrCreateTextTranslationInfo(this object ui)
        {
            TextTranslationInfo orCreateExtensionData = ui.GetOrCreateExtensionData<TextTranslationInfo>();
            orCreateExtensionData.Init(ui);
            return orCreateExtensionData;
        }

        public static void SetText(this object ui, string text, TextTranslationInfo info)
        {
            if (ui != null && info != null)
            {
                info.TextManipulator.SetText(ui, text);
            }
        }

        public static string GetText(this object ui, TextTranslationInfo info)
        {
            string text;
            if (ui == null)
            {
                text = null;
            }
            else
            {
                text = ((info != null) ? info.TextManipulator.GetText(ui) : null);
            }
            return text;
        }

        public static void TryRemoveCharacter(this TMP_FontAsset fontAsset, uint unicode)
        {
            ((List<uint>)TranslateExtensions.s_MissingCharacterList.GetValue(fontAsset)).Add(unicode);
            ((HashSet<uint>)TranslateExtensions.m_MissingUnicodesFromFontFile.GetValue(fontAsset)).Add(unicode);
            ((HashSet<uint>)TranslateExtensions.m_CharactersToAddLookup.GetValue(fontAsset)).Remove(unicode);
            ((Dictionary<uint, TMP_Character>)TranslateExtensions.m_CharacterLookupDictionary.GetValue(fontAsset)).Remove(unicode);
            ((List<TMP_Character>)TranslateExtensions.m_CharacterTable.GetValue(fontAsset)).RemoveAll((TMP_Character character) => character.unicode == unicode);
        }

        private static readonly Dictionary<Type, ITextComponentManipulator> Manipulators = new Dictionary<Type, ITextComponentManipulator>();

        private static List<TranslateExtensions.IPropertyMover> TexturePropertyMovers;

        private static readonly string TexturePropertyName = "texture";

        private static readonly string MainTexturePropertyName = "mainTexture";

        private static readonly string CapitalMainTexturePropertyName = "MainTexture";

        public static FieldInfo s_MissingCharacterList = typeof(TMP_FontAsset).GetField("s_MissingCharacterList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo m_MissingUnicodesFromFontFile = typeof(TMP_FontAsset).GetField("m_MissingUnicodesFromFontFile", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo m_CharacterLookupDictionary = typeof(TMP_FontAsset).GetField("m_CharacterLookupDictionary", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo m_CharacterTable = typeof(TMP_FontAsset).GetField("m_CharacterTable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo m_CharactersToAddLookup = typeof(TMP_FontAsset).GetField("m_CharactersToAddLookup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public enum ImageFormat
        {
            PNG,
            TGA
        }

        private interface IPropertyMover
        {
            void MoveProperty(object source, object destination);
        }

        private class PropertyMover<T, TPropertyType> : TranslateExtensions.IPropertyMover
        {
            public PropertyMover(PropertyInfo propertyInfo)
            {
                MethodInfo getMethod = propertyInfo.GetGetMethod();
                MethodInfo setMethod = propertyInfo.GetSetMethod();
                this._get = (Func<T, TPropertyType>)ExpressionHelper.CreateTypedFastInvoke(getMethod);
                this._set = (Action<T, TPropertyType>)ExpressionHelper.CreateTypedFastInvoke(setMethod);
            }

            public void MoveProperty(object source, object destination)
            {
                TPropertyType tpropertyType = this._get((T)((object)source));
                this._set((T)((object)destination), tpropertyType);
            }

            private readonly Func<T, TPropertyType> _get;

            private readonly Action<T, TPropertyType> _set;
        }
    }
}
