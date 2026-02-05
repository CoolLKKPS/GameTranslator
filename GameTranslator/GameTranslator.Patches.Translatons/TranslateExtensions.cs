using GameTranslator.Patches.Translatons.Manipulator;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Extensions;
using XUnity.Common.Harmony;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons
{
    internal static class TranslateExtensions
    {
        public static Type GetUnityType(this object obj)
        {
            return obj.GetType();
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        public static bool Contains(this string s, string value, bool ignoreCase = false)
        {
            if (!ignoreCase)
            {
                return s.IndexOf(value, StringComparison.Ordinal) >= 0;
            }
            return s.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string ToString(this char[] value, int startIndex, int count)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (startIndex < 0 || startIndex > value.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (count < 0 || startIndex + count > value.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return new string(value, startIndex, count);
        }

        public static StringBuilder ReplaceFull(this StringBuilder builder, string oldValue, string newValue)
        {
            return builder.ReplaceFull(oldValue, newValue, 0, builder.Length);
        }

        private static StringBuilder ReplaceFull(this StringBuilder builder, string oldValue, string newValue, int startIndex, int count)
        {
            int length = builder.Length;
            if (startIndex > length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (count < 0 || startIndex > length - count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (oldValue == null)
            {
                throw new ArgumentNullException("oldValue");
            }
            if (oldValue.Length == 0)
            {
                throw new ArgumentException("oldValue");
            }
            if (newValue == null)
            {
                newValue = "";
            }
            int length2 = newValue.Length;
            int length3 = oldValue.Length;
            int[] array = null;
            int num = 0;
            StringBuilder stringBuilder = (StringBuilder)TranslateExtensions.FindChunkForIndex.Invoke(builder, new object[] { startIndex });
            int num2 = startIndex - (int)TranslateExtensions.m_ChunkOffset.GetValue(builder);
            while (count > 0)
            {
                if ((num2 == 0 || !TranslateExtensions.IsWordChar(stringBuilder[num2 - 1])) && (bool)TranslateExtensions.StartsWith.Invoke(builder, new object[] { stringBuilder, num2, count, oldValue }))
                {
                    int num3 = num2 + oldValue.Length;
                    if (num3 == length || !TranslateExtensions.IsWordChar(stringBuilder[num3]))
                    {
                        if (array == null)
                        {
                            array = new int[5];
                        }
                        else if (num >= array.Length)
                        {
                            int[] array2 = new int[array.Length * 3 / 2 + 4];
                            Array.Copy(array, array2, array.Length);
                            array = array2;
                        }
                        array[num++] = num2;
                    }
                    num2 += oldValue.Length;
                    count -= oldValue.Length;
                }
                else
                {
                    num2++;
                    count--;
                }
                if (num2 >= (int)TranslateExtensions.m_ChunkLength.GetValue(stringBuilder) || count == 0)
                {
                    int num4 = num2 + (int)TranslateExtensions.m_ChunkOffset.GetValue(stringBuilder);
                    TranslateExtensions.ReplaceAllInChunk.Invoke(builder, new object[] { array, num, stringBuilder, oldValue.Length, newValue });
                    num4 += (newValue.Length - oldValue.Length) * num;
                    num = 0;
                    stringBuilder = (StringBuilder)TranslateExtensions.FindChunkForIndex.Invoke(builder, new object[] { num4 });
                    num2 = num4 - (int)TranslateExtensions.m_ChunkOffset.GetValue(stringBuilder);
                }
            }
            return builder;
        }

        public static StringBuilder ReplaceFullWords(this StringBuilder s, string oldWord, string newWord, bool ignoreCase = false)
        {
            StringBuilder stringBuilder;
            if (s == null)
            {
                stringBuilder = null;
            }
            else
            {
                string text = s.ToString();
                string text2 = "(?<=^|\\W)" + Regex.Escape(oldWord) + "(?=$|\\W)";
                string text3 = Regex.Replace(text, text2, newWord, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                stringBuilder = s.Clear().Append(text3);
            }
            return stringBuilder;
        }

        public static string ReplaceFullWords(this string s, string oldWord, string newWord, bool ignoreCase = false)
        {
            string text;
            if (s == null)
            {
                text = null;
            }
            else
            {
                string text2 = "(?<=^|\\W)" + Regex.Escape(oldWord) + "(?=$|\\W)";
                text = Regex.Replace(s, text2, newWord, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            }
            return text;
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

        public static Sprite SetTexture(this object ui, Texture2D texture, Sprite sprite, bool isPrefixHooked)
        {
            Sprite sprite2;
            if (ui == null)
            {
                sprite2 = null;
            }
            else
            {
                Texture2D texture2 = ui.GetTexture();
                if (!UnityObjectReferenceComparer.Default.Equals(texture2, texture))
                {
                    SpriteRenderer spriteRenderer = null;
                    if (ui.TryCastTo(out spriteRenderer))
                    {
                        if (isPrefixHooked)
                        {
                            return TranslateExtensions.SafeCreateSprite(spriteRenderer, sprite, texture);
                        }
                        return TranslateExtensions.SafeSetSprite(spriteRenderer, sprite, texture);
                    }
                    else
                    {
                        Type type = ui.GetType();
                        CachedProperty cachedProperty = type.CachedProperty(TranslateExtensions.MainTexturePropertyName);
                        if (cachedProperty != null)
                        {
                            cachedProperty.Set(ui, texture);
                        }
                        CachedProperty cachedProperty2 = type.CachedProperty(TranslateExtensions.TexturePropertyName);
                        if (cachedProperty2 != null)
                        {
                            cachedProperty2.Set(ui, texture);
                        }
                        CachedProperty cachedProperty3 = type.CachedProperty(TranslateExtensions.CapitalMainTexturePropertyName);
                        if (cachedProperty3 != null)
                        {
                            cachedProperty3.Set(ui, texture);
                        }
                        CachedProperty cachedProperty4 = type.CachedProperty("material");
                        object obj = ((cachedProperty4 != null) ? cachedProperty4.Get(ui) : null);
                        if (obj != null)
                        {
                            CachedProperty cachedProperty5 = obj.GetType().CachedProperty(TranslateExtensions.MainTexturePropertyName);
                            if ((Texture2D)((cachedProperty5 != null) ? cachedProperty5.Get(obj) : null) == texture2 && cachedProperty5 != null)
                            {
                                cachedProperty5.Set(obj, texture);
                            }
                        }
                    }
                }
                sprite2 = null;
            }
            return sprite2;
        }

        public static Sprite SafeSetSprite(SpriteRenderer sr, Sprite sprite, Texture2D texture)
        {
            Sprite sprite2 = Sprite.Create(texture, (sprite != null) ? sprite.rect : sr.sprite.rect, Vector2.zero);
            sr.sprite = sprite2;
            return sprite2;
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
            float realtimeSinceStartup = Time.realtimeSinceStartup;
            int width = texture.width;
            int height = texture.height;
            byte[] array = null;
            if (array == null)
            {
                RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
                GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
                Graphics.Blit(texture, temporary);
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = temporary;
                Texture2D texture2D = new Texture2D(width, height);
                texture2D.ReadPixels(new Rect(0f, 0f, (float)temporary.width, (float)temporary.height), 0, 0);
                array = TranslateExtensions.EncodeToPNGEx(texture2D);
                global::UnityEngine.Object.DestroyImmediate(texture2D);
                RenderTexture.active = ((active == temporary) ? null : active);
                RenderTexture.ReleaseTemporary(temporary);
            }
            float realtimeSinceStartup2 = Time.realtimeSinceStartup;
            return new TextureDataResult(array, false, realtimeSinceStartup2 - realtimeSinceStartup);
        }

        public static bool IsCompatible(this object texture, TranslateExtensions.ImageFormat dataType)
        {
            TextureFormat format = ((Texture2D)texture).format;
            return dataType == TranslateExtensions.ImageFormat.PNG || (dataType == TranslateExtensions.ImageFormat.TGA && (format == TextureFormat.ARGB32 || format == TextureFormat.RGBA32 || format == TextureFormat.RGB24));
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

        private static Sprite SafeCreateSprite(Sprite sprite, Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(sprite.rect.x, sprite.rect.y, (float)texture.width, (float)texture.height), sprite.pivot);
        }

        private static Sprite SafeCreateSprite(SpriteRenderer sr, Sprite sprite, Texture2D texture)
        {
            return Sprite.Create(texture, (sprite != null) ? sprite.rect : sr.sprite.rect, Vector2.zero);
        }

        public static void SetAllDirtyEx(this object ui)
        {
            if (ui == null) return;

            var type = ui.GetUnityType();

            if (UnityTypes.Graphic != null && UnityTypes.Graphic.IsAssignableFrom(type))
            {
                UnityTypes.Graphic.ClrType.CachedMethod(SetAllDirtyMethodName).Invoke(ui);
            }
            else if (!ui.TryCastTo<SpriteRenderer>(out _))
            {
                var clrType = ui.GetType();
                AccessToolsShim.Method(clrType, MarkAsChangedMethodName)?.Invoke(ui, null);
            }
        }

        public static bool ShouldIgnoreTextComponent(this object ui)
        {
            Component component = ui as Component;
            bool flag;
            if (component == null || !component)
            {
                flag = false;
            }
            else
            {
                GameObject gameObject = component.gameObject;
                Component component2;
                if (UnityTypes.InputField != null)
                {
                    GameObject gameObject2 = component.gameObject;
                    TypeContainer inputField = UnityTypes.InputField;
                    component2 = gameObject2.GetFirstComponentInSelfOrAncestor((inputField != null) ? inputField.UnityType : null);
                    if (component2 != null && UnityTypes.InputField_Properties.Placeholder != null)
                    {
                        Component component3 = (Component)UnityTypes.InputField_Properties.Placeholder.Get(component2);
                        return !UnityObjectReferenceComparer.Default.Equals(component3, component);
                    }
                }
                if (UnityTypes.TMP_InputField != null)
                {
                    GameObject gameObject3 = component.gameObject;
                    TypeContainer tmp_InputField = UnityTypes.TMP_InputField;
                    component2 = gameObject3.GetFirstComponentInSelfOrAncestor((tmp_InputField != null) ? tmp_InputField.UnityType : null);
                    if (component2 != null && UnityTypes.TMP_InputField_Properties.Placeholder != null)
                    {
                        Component component4 = (Component)UnityTypes.TMP_InputField_Properties.Placeholder.Get(component2);
                        return !UnityObjectReferenceComparer.Default.Equals(component4, component);
                    }
                }
                GameObject gameObject4 = gameObject;
                TypeContainer uiinput = UnityTypes.UIInput;
                component2 = gameObject4.GetFirstComponentInSelfOrAncestor((uiinput != null) ? uiinput.UnityType : null);
                flag = component2 != null;
            }
            return flag;
        }

        public static bool IsKnownTextType(this object ui)
        {
            if (ui == null) return false;

            var type = ui.GetUnityType();
            return (UnityTypes.Text != null && UnityTypes.Text.IsAssignableFrom(type)) ||
                   (UnityTypes.TextMeshPro != null && UnityTypes.TextMeshPro.IsAssignableFrom(type)) ||
                   (UnityTypes.TextMeshProUGUI != null && UnityTypes.TextMeshProUGUI.IsAssignableFrom(type)) ||
                   (UnityTypes.TextField != null && UnityTypes.TextField.IsAssignableFrom(type)) ||
                   (UnityTypes.TextArea2D != null && UnityTypes.TextArea2D.IsAssignableFrom(type)) ||
                   (UnityTypes.UguiNovelText != null && UnityTypes.UguiNovelText.IsAssignableFrom(type)) ||
                   (UnityTypes.UILabel != null && UnityTypes.UILabel.IsAssignableFrom(type)) ||
                   (UnityTypes.TextMesh != null && UnityTypes.TextMesh.IsAssignableFrom(type));
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

        public static ImageTranslationInfo GetOrCreateImageTranslationInfo(this object obj, Texture2D originalTexture)
        {
            ImageTranslationInfo imageTranslationInfo;
            if (obj == null)
            {
                imageTranslationInfo = null;
            }
            else
            {
                ImageTranslationInfo orCreateExtensionData = obj.GetOrCreateExtensionData<ImageTranslationInfo>();
                if (orCreateExtensionData.Original == null)
                {
                    orCreateExtensionData.Initialize(originalTexture);
                }
                imageTranslationInfo = orCreateExtensionData;
            }
            return imageTranslationInfo;
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

        public static void TryRemoveCharacters(this TMP_FontAsset fontAsset, uint[] unicodes)
        {
            for (int i = 0; i < unicodes.Length; i++)
            {
                fontAsset.TryRemoveCharacter(unicodes[i]);
            }
        }

        public static bool IsCompatible(this Texture2D texture, TranslateExtensions.ImageFormat dataType)
        {
            var format = texture.format;
            return dataType == TranslateExtensions.ImageFormat.PNG
                || (dataType == TranslateExtensions.ImageFormat.TGA && (format == TextureFormat.ARGB32 || format == TextureFormat.RGBA32 || format == TextureFormat.RGB24));
        }

        private static readonly Dictionary<Type, ITextComponentManipulator> Manipulators = new Dictionary<Type, ITextComponentManipulator>();

        public static FieldInfo m_ChunkOffset = typeof(StringBuilder).GetField("m_ChunkOffset", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FieldInfo m_ChunkLength = typeof(StringBuilder).GetField("m_ChunkLength", BindingFlags.Instance | BindingFlags.NonPublic);

        public static MethodInfo FindChunkForIndex = typeof(StringBuilder).GetMethod("FindChunkForIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        public static MethodInfo ReplaceAllInChunk = typeof(StringBuilder).GetMethod("ReplaceAllInChunk", BindingFlags.Instance | BindingFlags.NonPublic);

        public static MethodInfo StartsWith = typeof(StringBuilder).GetMethod("StartsWith", BindingFlags.Instance | BindingFlags.NonPublic);

        private static List<TranslateExtensions.IPropertyMover> TexturePropertyMovers;

        private static readonly string SetAllDirtyMethodName = "SetAllDirty";

        private static readonly string TexturePropertyName = "texture";

        private static readonly string MainTexturePropertyName = "mainTexture";

        private static readonly string CapitalMainTexturePropertyName = "MainTexture";

        private static readonly string MarkAsChangedMethodName = "MarkAsChanged";

        public static FieldInfo m_AtlasPopulationMode = typeof(TMP_FontAsset).GetField("m_AtlasPopulationMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo m_SourceFontFile = typeof(TMP_FontAsset).GetField("m_SourceFontFile", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        public static FieldInfo m_FaceInfo = typeof(TMP_FontAsset).GetField("m_FaceInfo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

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
