using GameTranslator.Patches.Translatons;
using System;
using System.IO;
using UnityEngine;
using XUnity.Common.Extensions;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Utils
{
    internal class TextureTranslate
    {
        internal void Hook_ImageChangedOnComponent(object source, ref Texture2D texture, bool isPrefixHooked, bool onEnable = false)
        {
            if (TextureTranslate.ImageHooksEnabled && (TranslatePlugin.changeTexture.Value || TranslatePlugin.enableTextureDumping.Value) && source.IsKnownImageType())
            {
                Sprite sprite = null;
                this.HandleImage(source, ref sprite, ref texture, isPrefixHooked);
            }
        }

        internal void Hook_ImageChangedOnComponent(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked, bool onEnable)
        {
            if (TextureTranslate.ImageHooksEnabled && (TranslatePlugin.changeTexture.Value || TranslatePlugin.enableTextureDumping.Value) && source.IsKnownImageType())
            {
                this.HandleImage(source, ref sprite, ref texture, isPrefixHooked);
            }
        }

        internal void Hook_ImageChanged(ref Texture2D texture, bool isPrefixHooked)
        {
            if (TextureTranslate.ImageHooksEnabled && (TranslatePlugin.changeTexture.Value || TranslatePlugin.enableTextureDumping.Value) && !(texture == null))
            {
                Sprite sprite = null;
                this.HandleImage(null, ref sprite, ref texture, isPrefixHooked);
            }
        }

        private void HandleImage(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked)
        {
            try
            {
                if (TranslatePlugin.enableTextureDumping.Value)
                {
                    this.DumpTexture(source, texture);
                }
                if (TranslatePlugin.changeTexture.Value && this.ShouldProcessTexture(source, texture))
                {
                    this.TranslateTexture(source, ref sprite, ref texture, isPrefixHooked);
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while translating texture.");
            }
        }

        private void DumpTexture(object source, Texture2D texture)
        {
            try
            {
                TextureTranslate.ImageHooksEnabled = false;

                texture ??= source.GetTexture();
                if (texture == null) return;

                var format = (int)texture.format;
                if (format is 1 or 9 or 63) return;

                var tti = texture.GetOrCreateTextureTranslationInfo();
                if (tti.IsDumped) return;

                var key = tti.GetKey();
                if (string.IsNullOrEmpty(key)) return;

                var name = texture.GetTextureName("Unnamed");
                var originalData = tti.GetOrCreateOriginalData();
                DumpImageToDisk(name, key, originalData);
                tti.IsDumped = true;
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while dumping texture.");
            }
            finally
            {
                TextureTranslate.ImageHooksEnabled = true;
            }
        }

        private static void DumpImageToDisk(string textureName, string key, byte[] data)
        {
            Directory.CreateDirectory(TranslatePlugin.DumpPath);
            string sanitizedName = textureName.SanitizeForFileSystem();
            string dataHash = TextureTranslationCache.HashHelper.Compute(data);
            string fileName;
            if (key == dataHash)
            {
                fileName = sanitizedName + " [" + key + "].png";
            }
            else
            {
                fileName = sanitizedName + " [" + key + "-" + dataHash + "].png";
            }
            string fullPath = Path.Combine(TranslatePlugin.DumpPath, fileName);
            File.WriteAllBytes(fullPath, data);
            XuaLogger.AutoTranslator.Info("Dumped texture file: " + fileName);
        }

        private void TranslateTexture(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked)
        {
            try
            {
                TextureTranslate.ImageHooksEnabled = false;

                var previousTextureValue = texture;
                texture ??= source.GetTexture();
                if (texture == null) return;

                var tti = texture.GetOrCreateTextureTranslationInfo();
                var key = tti.GetKey();
                if (string.IsNullOrEmpty(key)) return;

                if (TranslateConfig.cache != null)
                {
                    TranslateConfig.cache.UpdateTextureStatistics(key);
                }

                if (TranslateConfig.cache.TryGetTranslatedImage(key, out var newData, out var translatedImage))
                {
                    var isCompatible = texture.IsCompatible(translatedImage.ImageFormat);

                    if (!tti.IsTranslated)
                    {
                        try
                        {
                            if (isCompatible)
                            {
                                texture.LoadImageEx(newData, translatedImage.ImageFormat, null);
                            }
                            else
                            {
                                tti.CreateTranslatedTexture(newData, translatedImage.ImageFormat);
                            }
                        }
                        finally
                        {
                            tti.IsTranslated = true;
                        }
                    }
                }

                if (previousTextureValue == null)
                {
                    texture = null;
                }
                else if (tti.UsingReplacedTexture)
                {
                    if (tti.IsTranslated)
                    {
                        var translated = tti.Translated;
                        if (translated != null)
                        {
                            texture = translated;
                        }
                    }
                    else
                    {
                        var original = tti.Original.Target;
                        if (original != null)
                        {
                            texture = original;
                        }
                    }
                }
                else
                {
                    texture = previousTextureValue;
                }
            }
            catch (FileNotFoundException ex)
            {
                XuaLogger.AutoTranslator.Warn("Texture file not found: " + ex.FileName);
            }
            catch (FormatException ex2)
            {
                XuaLogger.AutoTranslator.Error(ex2, "Invalid image format.");
            }
            catch (Exception ex3)
            {
                XuaLogger.AutoTranslator.Error(ex3, "An unexpected error occurred while translating texture.");
            }
            finally
            {
                TextureTranslate.ImageHooksEnabled = true;
            }
        }

        private bool ShouldProcessTexture(object source, Texture2D texture)
        {
            if (texture == null && source == null)
            {
                return false;
            }
            if (texture != null)
            {
                TextureTranslationInfo textureInfo = texture.GetOrCreateTextureTranslationInfo();
                if (textureInfo.IsTranslated && textureInfo.Translated != null)
                {
                    if (textureInfo.ChangeTime != TextureTranslate.ChangeTime)
                    {
                        textureInfo.Reset();
                    }
                    else
                    {
                        return false;
                    }
                }

                var format = (int)texture.format;
                if (format is 1 or 9 or 63)
                {
                    return false;
                }
            }
            return true;
        }

        public static TextureTranslate Instance = new TextureTranslate();

        public static bool ImageHooksEnabled = true;

        public static long ChangeTime = 0L;
    }
}
