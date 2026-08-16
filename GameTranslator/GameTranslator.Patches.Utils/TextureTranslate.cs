using GameTranslator.Patches.Translatons;
using System;
using System.Collections.Generic;
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
                this.HandleImage(source, ref sprite, ref texture, isPrefixHooked, null);
            }
        }

        internal void Hook_ImageChangedOnComponent(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked, bool onEnable)
        {
            if (TextureTranslate.ImageHooksEnabled && (TranslatePlugin.changeTexture.Value || TranslatePlugin.enableTextureDumping.Value) && source.IsKnownImageType())
            {
                this.HandleImage(source, ref sprite, ref texture, isPrefixHooked, null);
            }
        }

        internal void Hook_ImageChanged(ref Texture2D texture, bool isPrefixHooked, string dumpDirectory)
        {
            if (TextureTranslate.ImageHooksEnabled && (TranslatePlugin.changeTexture.Value || TranslatePlugin.enableTextureDumping.Value) && !(texture == null))
            {
                Sprite sprite = null;
                this.HandleImage(null, ref sprite, ref texture, isPrefixHooked, dumpDirectory);
            }
        }

        private void HandleImage(object source, ref Sprite sprite, ref Texture2D texture, bool isPrefixHooked, string dumpDirectory)
        {
            try
            {
                if (dumpDirectory == null ? TranslatePlugin.enableTextureDumping.Value : TranslatePlugin.textureEnhancementDump)
                {
                    this.DumpTexture(source, texture, dumpDirectory);
                }
                if (TranslatePlugin.changeTexture.Value && (dumpDirectory == null || TranslatePlugin.textureEnhancement) && this.ShouldProcessTexture(source, texture))
                {
                    this.TranslateTexture(source, ref sprite, ref texture, isPrefixHooked);
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while translating texture.");
            }
        }

        private void DumpTexture(object source, Texture2D texture, string dumpDirectory)
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

                if (dumpDirectory != null)
                {
                    RecordEncounteredKey(texture.GetTextureName("Unnamed"), key);
                }
                else
                {
                    var name = texture.GetTextureName("Unnamed");
                    var originalData = tti.GetOrCreateOriginalData();
                    DumpImageToDisk(name, key, originalData, TranslatePlugin.DumpPath);
                }
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

        private static readonly HashSet<string> RecordedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<string> PendingKeys = [];

        private static bool _recordedKeysLoaded;

        private const int FlushThreshold = 32;

        private static void RecordEncounteredKey(string name, string key)
        {
            if (!_recordedKeysLoaded)
            {
                _recordedKeysLoaded = true;
                var recordPath = Path.Combine(TranslatePlugin.SceneDumpPath, "scene_textures.txt");
                if (File.Exists(recordPath))
                {
                    foreach (var line in File.ReadLines(recordPath))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.Length == 0)
                        {
                            continue;
                        }
                        var start = trimmed.IndexOf(" [", StringComparison.Ordinal);
                        var segment = start >= 0 ? trimmed.Substring(start + 2).TrimEnd(']') : trimmed;
                        var dash = segment.IndexOf('-');
                        RecordedKeys.Add(dash >= 0 ? segment.Substring(0, dash) : segment);
                    }
                }
            }
            if (!RecordedKeys.Add(key))
            {
                return;
            }
            PendingKeys.Add(name + " [" + key + "]");
            if (PendingKeys.Count >= FlushThreshold)
            {
                FlushPendingKeys();
            }
        }

        internal static void FlushPendingKeys()
        {
            if (PendingKeys.Count == 0)
            {
                return;
            }
            Directory.CreateDirectory(TranslatePlugin.SceneDumpPath);
            File.AppendAllLines(Path.Combine(TranslatePlugin.SceneDumpPath, "scene_textures.txt"), PendingKeys);
            PendingKeys.Clear();
        }

        private static void DumpImageToDisk(string textureName, string key, byte[] data, string dumpDirectory)
        {
            Directory.CreateDirectory(dumpDirectory);
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
            string fullPath = Path.Combine(dumpDirectory, fileName);
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
