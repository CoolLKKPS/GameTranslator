using GameTranslator.Patches.Translatons;
using System;
using System.IO;
using UnityEngine;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Utils
{
    public class TextureTranslate
    {
        internal void Hook_ImageChangedOnComponent(object source, ref Texture2D texture, bool isPrefixHooked, bool onEnable = false)
        {
            if (TextureTranslate.ImageHooksEnabled && TranslatePlugin.changeTexture.Value && source.IsKnownImageType())
            {
                Sprite sprite = null;
                this.HandleImage(source, ref sprite, ref texture, isPrefixHooked);
            }
        }

        internal void Hook_ImageChangedOnComponent(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked, bool onEnable)
        {
            if (TextureTranslate.ImageHooksEnabled && TranslatePlugin.changeTexture.Value && source.IsKnownImageType())
            {
                this.HandleImage(source, ref sprite, ref texture, isPrefixHooked);
            }
        }

        internal void Hook_ImageChanged(ref Texture2D texture, bool isPrefixHooked)
        {
            if (TextureTranslate.ImageHooksEnabled && TranslatePlugin.changeTexture.Value && !(texture == null))
            {
                Sprite sprite = null;
                this.HandleImage(null, ref sprite, ref texture, isPrefixHooked);
            }
        }

        public void HandleImage(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked)
        {
            try
            {
                if (this.ShouldProcessTexture(source, texture))
                {
                    this.TranslateTexture(source, ref sprite, ref texture, isPrefixHooked);
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while translating texture.");
            }
        }

        private void TranslateTexture(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked)
        {
            try
            {
                TextureTranslate.ImageHooksEnabled = false;

                var previousTextureValue = texture;
                texture = texture ?? source.GetTexture();
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

                    if (source != null)
                    {
                        if (!tti.IsTranslated)
                        {
                            try
                            {
                                if (!isCompatible)
                                {
                                    var newSprite = source.SetTexture(tti.Translated, sprite, isPrefixHooked);
                                    if (newSprite != null)
                                    {
                                        tti.TranslatedSprite = newSprite;
                                        if (isPrefixHooked && sprite != null)
                                        {
                                            sprite = newSprite;
                                        }
                                    }
                                }

                                if (!isPrefixHooked)
                                {
                                    source.SetAllDirtyEx();
                                }
                            }
                            finally
                            {
                            }
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
                    return false;
                }

                var format = (int)texture.format;
                if (format == 1 || format == 9 || format == 63)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsTextureFormatCompatible(Texture2D texture, TranslateExtensions.ImageFormat format)
        {
            var textureFormat = texture.format;
            return format == TranslateExtensions.ImageFormat.PNG
                || (format == TranslateExtensions.ImageFormat.TGA && (textureFormat == TextureFormat.ARGB32 || textureFormat == TextureFormat.RGBA32 || textureFormat == TextureFormat.RGB24));
        }

        public static TextureTranslate Instance = new TextureTranslate();

        public static bool ImageHooksEnabled = true;
    }
}
