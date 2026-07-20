using GameTranslator.Patches.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using XUnity.Common.Logging;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons
{
    internal class TextureTranslationInfo
    {
        public XUnity.Common.Utilities.WeakReference<Texture2D> Original { get; private set; }

        public Texture2D Translated { get; private set; }

        public bool IsTranslated { get; set; }

        public bool UsingReplacedTexture { get; set; }

        public long ChangeTime { get; set; }

        public void Reset()
        {
            this.IsTranslated = false;
            this.Translated = null;
            this.UsingReplacedTexture = false;
            this.ChangeTime = TextureTranslate.ChangeTime;
        }

        // Still using for other purposes
        public static void ClearNameToHash()
        {
            NameToHash.Clear();
        }

        public void Initialize(Texture2D texture)
        {
            if (!this._initialized)
            {
                this._initialized = true;
                this._textureFormat = texture.format;
                this.SetOriginal(texture);
            }
        }

        public void SetOriginal(Texture2D texture)
        {
            this.Original = XUnity.Common.Utilities.WeakReference<Texture2D>.Create(texture);
        }

        public void SetTranslated(Texture2D texture)
        {
            this.Translated = texture;
        }

        public void CreateTranslatedTexture(byte[] newData, TranslateExtensions.ImageFormat format)
        {
            if (this.Translated == null)
            {
                var orig = this.Original.Target;

                var texture = TextureTranslationInfo.CreateEmptyTexture2D(this._textureFormat);
                texture.LoadImageEx(newData, format, orig);

                this.SetTranslated(texture);
                texture.SetExtensionData(this);
                this.UsingReplacedTexture = true;
            }
        }

        public string GetKey()
        {
            if (this.Original.Target == null)
            {
                return null;
            }
            this.SetupHashAndData(this.Original.Target);
            return this._key;
        }

        private TextureDataResult SetupKeyForNameWithFallback(string name, Texture2D texture)
        {
            if (TranslatePlugin.disableDuplicateTextureCheck.Value)
            {
                this._key = TextureTranslationCache.HashHelper.Compute(TextureTranslationInfo.UTF8.GetBytes(name));
                return null;
            }
            if (!string.IsNullOrEmpty(TranslatePlugin.ignoredTextureNames.Value))
            {
                var ignoredNames = TranslatePlugin.ignoredTextureNames.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (ignoredNames.Contains(name))
                {
                    this._key = TextureTranslationCache.HashHelper.Compute(TextureTranslationInfo.UTF8.GetBytes(name));
                    return null;
                }
            }
            bool flag = false;
            string text = null;
            TextureDataResult textureData = texture.GetTextureData();
            string text2 = TextureTranslationCache.HashHelper.Compute(textureData.Data);
            if (TextureTranslationInfo.NameToHash.TryGetValue(name, out text))
            {
                if (text != text2)
                {
                    XuaLogger.AutoTranslator.Warn("Detected duplicate image name: " + name);
                    flag = true;
                }
            }
            else
            {
                TextureTranslationInfo.NameToHash[name] = text2;
            }
            this._key = TextureTranslationCache.HashHelper.Compute(TextureTranslationInfo.UTF8.GetBytes(name));
            if (flag)
            {
                string text3 = TextureTranslationCache.HashHelper.Compute(TextureTranslationInfo.UTF8.GetBytes(name));
                TranslateConfig.cache.RenameFileWithKey(name, text3, text);
            }
            return textureData;
        }

        private void SetupHashAndData(Texture2D texture)
        {
            if (this._key == null)
            {
                string textureName = texture.GetTextureName(null);
                if (textureName != null)
                {
                    TextureDataResult textureDataResult = this.SetupKeyForNameWithFallback(textureName, texture);
                }
            }
        }

        public static Texture2D CreateEmptyTexture2D(TextureFormat format)
        {
            TextureFormat newFormat;
            switch (format)
            {
                case TextureFormat.RGB24:
                    newFormat = TextureFormat.RGB24;
                    break;
                case TextureFormat.DXT1:
                    newFormat = TextureFormat.RGB24;
                    break;
                case TextureFormat.DXT5:
                    newFormat = TextureFormat.ARGB32;
                    break;
                default:
                    newFormat = TextureFormat.ARGB32;
                    break;
            }
            return new Texture2D(2, 2, newFormat, false);
        }

        private static Dictionary<string, string> NameToHash = new Dictionary<string, string>();

        private static readonly Encoding UTF8 = new UTF8Encoding(false);

        private string _key;

        private bool _initialized;

        private TextureFormat _textureFormat;
    }
}
