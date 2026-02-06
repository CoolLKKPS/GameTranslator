using GameTranslator.Patches.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using XUnity.Common.Extensions;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Translatons
{
    public class TextureTranslationCache
    {
        private SafeFileWatcher _textureFileWatcher;

        private readonly ConcurrentDictionary<string, DateTime> _textureFileLastModifiedTimes = new ConcurrentDictionary<string, DateTime>();

        private Timer _texturePollingTimer;

        private readonly object _loadLock = new object();

        public TextureTranslationCache()
        {
            try
            {
                Directory.CreateDirectory(TranslatePlugin.TexturesPath);
                string fullPath = Path.GetFullPath(TranslatePlugin.TexturesPath);
                _textureFileWatcher = new SafeFileWatcher(fullPath);
                _textureFileWatcher.DirectoryUpdated += TextureFileWatcher_DirectoryUpdated;
                TranslatePlugin.logger.LogInfo("Tracking texture path: " + fullPath);
                if (TranslatePlugin.enablePollingCheck?.Value ?? false)
                {
                    _texturePollingTimer = new Timer(_ => TextureFileWatcher_DirectoryUpdated(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while initializing translation file watching for textures.");
            }
        }

        public IEnumerable<string> GetTextureFiles()
        {
            return from x in Directory.GetFiles(TranslatePlugin.TexturesPath, "*.*", SearchOption.AllDirectories)
                   where x.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                   select x;
        }

        public void LoadTranslationFiles()
        {
            lock (_loadLock)
            {
                try
                {
                    float realtimeSinceStartup = Time.realtimeSinceStartup;
                    this._translatedImages.Clear();
                    this._untranslatedImages.Clear();
                    this._keyToFileName.Clear();
                    Directory.CreateDirectory(TranslatePlugin.TexturesPath);
                    foreach (string text in this.GetTextureFiles())
                    {
                        this.RegisterImageFromFile(text);
                    }
                    this.CleanupInvalidEntries();

                    float realtimeSinceStartup2 = Time.realtimeSinceStartup;
                    XuaLogger.AutoTranslator.Debug(string.Format("Loaded texture files (took {0} seconds)", Math.Round((double)(realtimeSinceStartup2 - realtimeSinceStartup), 2)));
                }
                catch (Exception ex)
                {
                    XuaLogger.AutoTranslator.Error(ex, "An error occurred while loading translations.");
                }
            }
        }

        private void RegisterImageFromStream(string fullFileName, TranslatedImage.ITranslatedImageSource source)
        {
            try
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullFileName);
                int num = fileNameWithoutExtension.LastIndexOf("[");
                int num2 = fileNameWithoutExtension.LastIndexOf("]");
                if (num2 > -1 && num > -1 && num2 > num)
                {
                    int num3 = num + 1;
                    string[] array = fileNameWithoutExtension.Substring(num3, num2 - num3).Split(new char[] { '-' });
                    string text;
                    string text2;
                    if (array.Length == 1)
                    {
                        text = array[0];
                        text2 = array[0];
                    }
                    else
                    {
                        if (array.Length != 2)
                        {
                            XuaLogger.AutoTranslator.Warn("Image not loaded (Unknown hash): " + fullFileName + ".");
                            return;
                        }
                        text = array[0];
                        text2 = array[1];
                    }
                    byte[] data = source.GetData();
                    string text3 = TextureTranslationCache.HashHelper.Compute(data);
                    bool flag = StringComparer.InvariantCultureIgnoreCase.Compare(text2, text3) != 0;
                    this._keyToFileName[text] = fullFileName;
                    if (flag || TranslatePlugin.cacheUnmodifiedTextures.Value)
                    {
                        this.RegisterTranslatedImage(fullFileName, text, data);
                        if (!flag)
                        {
                            XuaLogger.AutoTranslator.Debug("Image loaded (Unmodified): " + fullFileName + ".");
                        }
                        else
                        {
                            XuaLogger.AutoTranslator.Debug("Image loaded (Modified): " + fullFileName + ".");
                        }
                    }
                    else
                    {
                        this.RegisterUntranslatedImage(text);
                        XuaLogger.AutoTranslator.Debug("Image not loaded (Unmodified): " + fullFileName + ".");
                    }
                }
                else
                {
                    XuaLogger.AutoTranslator.Warn("Image not loaded (No hash): " + fullFileName + ".");
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while loading texture file: " + fullFileName);
            }
        }

        private void RegisterImageFromFile(string fullFileName)
        {
            if (File.Exists(fullFileName))
            {
                TextureTranslationCache.FileSystemTranslatedImageSource fileSystemTranslatedImageSource = new TextureTranslationCache.FileSystemTranslatedImageSource(fullFileName);
                this.RegisterImageFromStream(fullFileName, fileSystemTranslatedImageSource);
            }
        }

        public void RenameFileWithKey(string name, string key, string newKey)
        {
            try
            {
                string text;
                if (this._keyToFileName.TryGetValue(key, out text))
                {
                    this._keyToFileName.TryRemove(key, out _);
                    if (!this.IsImageRegistered(newKey))
                    {
                        byte[] array = File.ReadAllBytes(text);
                        this.RegisterImageFromData(name, newKey, array);
                        File.Delete(text);
                        XuaLogger.AutoTranslator.Warn(string.Concat(new string[] { "Replaced old file with name '", name, "' registered with key old '", key, "'." }));
                    }
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "An error occurred while trying to rename file with key '" + key + "'.");
            }
        }

        internal void RegisterImageFromData(string textureName, string key, byte[] data)
        {
            string text = textureName.SanitizeForFileSystem();
            string text2 = TextureTranslationCache.HashHelper.Compute(data);
            string text3;
            if (key == text2)
            {
                text3 = text + " [" + key + "].png";
            }
            else
            {
                text3 = string.Concat(new string[] { text, " [", key, "-", text2, "].png" });
            }
            string text4 = Path.Combine(TranslatePlugin.TexturesPath, text3);
            File.WriteAllBytes(text4, data);
            XuaLogger.AutoTranslator.Info("Dumped texture file: " + text3);
            this._keyToFileName[key] = text4;

            if (TranslatePlugin.cacheUnmodifiedTextures.Value)
            {
                this.RegisterTranslatedImage(text4, key, data);
            }
            else
            {
                this.RegisterUntranslatedImage(key);
            }
        }

        private void RegisterTranslatedImage(string fileName, string key, byte[] data)
        {
            if (TranslatePlugin.cacheTexturesInMemory.Value)
            {
                this._translatedImages[key] = new TranslatedImage(fileName, data, null);
            }
            else
            {
                TranslatedImage.ITranslatedImageSource source = new FileSystemTranslatedImageSource(fileName);
                this._translatedImages[key] = new TranslatedImage(fileName, null, source);
            }
        }

        private void RegisterUntranslatedImage(string key)
        {
            this._untranslatedImages.TryAdd(key, 0);
        }

        internal bool IsImageRegistered(string key)
        {
            return this._translatedImages.ContainsKey(key) || this._untranslatedImages.ContainsKey(key);
        }

        internal bool TryGetTranslatedImage(string key, out byte[] data, out TranslatedImage image)
        {
            this.PeriodicCleanup();
            if (this._translatedImages.TryGetValue(key, out image))
            {
                try
                {
                    data = image.GetData();
                    if (data != null)
                    {
                        this._textureAccessTime.AddOrUpdate(key, DateTime.Now, (string k, DateTime v) => DateTime.Now);
                    }
                    return data != null;
                }
                catch (Exception ex4)
                {
                    XuaLogger autoTranslator = XuaLogger.AutoTranslator;
                    Exception ex2 = ex4;
                    string text = "Error loading cached image: ";
                    TranslatedImage translatedImage = image;
                    autoTranslator.Error(ex2, text + ((translatedImage != null) ? translatedImage.FileName : null));
                    TranslatedImage translatedImage2;
                    this._translatedImages.TryRemove(key, out translatedImage2);
                    DateTime dateTime;
                    this._textureAccessTime.TryRemove(key, out dateTime);
                }
            }
            data = null;
            image = null;
            string text2;
            if (this._keyToFileName.TryGetValue(key, out text2) && File.Exists(text2))
            {
                try
                {
                    this.RegisterImageFromFile(text2);
                    if (this._translatedImages.TryGetValue(key, out image))
                    {
                        data = image.GetData();
                        if (data != null)
                        {
                            this._textureAccessTime.AddOrUpdate(key, DateTime.Now, (string k, DateTime v) => DateTime.Now);
                        }
                        return data != null;
                    }
                }
                catch (Exception ex3)
                {
                    XuaLogger.AutoTranslator.Error(ex3, "Error reloading image: " + text2);
                }
            }
            return false;
        }

        private void TextureFileWatcher_DirectoryUpdated()
        {
            try
            {
                LoadTranslationFiles();
                XuaLogger.AutoTranslator.Info("Texture files reloaded due to file changes.");
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "Error reloading texture files: " + ex.Message);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _textureFileWatcher?.Dispose();
                    _textureFileWatcher = null;
                    _texturePollingTimer?.Dispose();
                    _texturePollingTimer = null;
                }
                this._disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void CleanupInvalidEntries()
        {
            foreach (string text in this._translatedImages.Where(delegate (KeyValuePair<string, TranslatedImage> kv)
            {
                KeyValuePair<string, TranslatedImage> keyValuePair2 = kv;
                if (keyValuePair2.Value != null)
                {
                    var texture = keyValuePair2.Value.GetTexture();
                    return texture == null;
                }
                return true;
            }).Select(delegate (KeyValuePair<string, TranslatedImage> kv)
            {
                KeyValuePair<string, TranslatedImage> keyValuePair3 = kv;
                return keyValuePair3.Key;
            }).ToList<string>())
            {
                TranslatedImage translatedImage;
                this._translatedImages.TryRemove(text, out translatedImage);
                DateTime dateTime;
                this._textureAccessTime.TryRemove(text, out dateTime);
            }
            foreach (KeyValuePair<string, DateTime> keyValuePair in this._textureAccessTime.ToList<KeyValuePair<string, DateTime>>())
            {
                if (DateTime.Now - keyValuePair.Value > TextureTranslationCache.CLEANUP_INTERVAL)
                {
                    TranslatedImage translatedImage2;
                    this._translatedImages.TryRemove(keyValuePair.Key, out translatedImage2);
                    DateTime dateTime2;
                    this._textureAccessTime.TryRemove(keyValuePair.Key, out dateTime2);
                }
            }
            foreach (var key in this._untranslatedImages.Keys.ToArray())
            {
                if (!this._keyToFileName.ContainsKey(key))
                {
                    byte removed;
                    this._untranslatedImages.TryRemove(key, out removed);
                }
            }
            foreach (string text2 in this._keyToFileName.Where(delegate (KeyValuePair<string, string> kv)
            {
                KeyValuePair<string, string> keyValuePair4 = kv;
                return !File.Exists(keyValuePair4.Value);
            }).Select(delegate (KeyValuePair<string, string> kv)
            {
                KeyValuePair<string, string> keyValuePair5 = kv;
                return keyValuePair5.Key;
            }).ToList<string>())
            {
                this._keyToFileName.TryRemove(text2, out _);
            }
        }

        public void PeriodicCleanup()
        {
            if (DateTime.Now - this._lastCleanupTime > TextureTranslationCache.CLEANUP_INTERVAL)
            {
                this.CleanupInvalidEntries();
                this._lastCleanupTime = DateTime.Now;
            }
        }

        public void PreloadCommonTextures()
        {
            try
            {
                foreach (string key in (from kvp in this._textureAccessTime.OrderByDescending((KeyValuePair<string, DateTime> kvp) => kvp.Value).Take(10)
                                        select kvp.Key).ToList<string>())
                {
                    string fileName;
                    if (!this._translatedImages.ContainsKey(key) && this._keyToFileName.TryGetValue(key, out fileName) && File.Exists(fileName))
                    {
                        this.RegisterImageFromFile(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                XuaLogger.AutoTranslator.Error(ex, "Error during texture preloading");
            }
        }

        public void UpdateTextureStatistics(string key)
        {
            this._textureAccessTime.AddOrUpdate(key, DateTime.Now, (string k, DateTime v) => DateTime.Now);
        }

        public ConcurrentDictionary<string, byte> _untranslatedImages = new ConcurrentDictionary<string, byte>();

        private ConcurrentDictionary<string, string> _keyToFileName = new ConcurrentDictionary<string, string>();

        private bool _disposed;

        public ConcurrentDictionary<string, TranslatedImage> _translatedImages = new ConcurrentDictionary<string, TranslatedImage>(StringComparer.InvariantCultureIgnoreCase);

        private static readonly TimeSpan CLEANUP_INTERVAL = TimeSpan.FromMinutes(15.0);

        private DateTime _lastCleanupTime = DateTime.Now;

        private readonly ConcurrentDictionary<string, DateTime> _textureAccessTime = new ConcurrentDictionary<string, DateTime>();

        internal class FileSystemTranslatedImageSource : TranslatedImage.ITranslatedImageSource
        {
            public FileSystemTranslatedImageSource(string fileName)
            {
                this._fileName = fileName;
            }

            public byte[] GetData()
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        using (FileStream fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return fileStream.ReadFully(16384);
                        }
                    }
                    catch (IOException) when (i < 2)
                    {
                        Thread.Sleep(100 * (i + 1));
                    }
                }
                throw new IOException($"Unable to read file '{_fileName}' after 3 attempts due to sharing violation.");
            }

            private readonly string _fileName;
        }

        internal static class HashHelper
        {
            public static string Compute(byte[] data)
            {
                return TextureTranslationCache.HashHelper.ByteArrayToHexViaLookup32(TextureTranslationCache.HashHelper.SHA1.ComputeHash(data)).Substring(0, 10);
            }

            private static uint[] CreateLookup32()
            {
                uint[] array = new uint[256];
                for (int i = 0; i < 256; i++)
                {
                    string text = i.ToString("X2");
                    array[i] = (uint)(text[0] + ((uint)text[1] << 16));
                }
                return array;
            }

            private static string ByteArrayToHexViaLookup32(byte[] bytes)
            {
                uint[] lookup = TextureTranslationCache.HashHelper.Lookup32;
                char[] array = new char[bytes.Length * 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    uint num = lookup[(int)bytes[i]];
                    array[2 * i] = (char)num;
                    array[2 * i + 1] = (char)(num >> 16);
                }
                return new string(array);
            }

            private static readonly SHA1Managed SHA1 = new SHA1Managed();

            private static readonly uint[] Lookup32 = TextureTranslationCache.HashHelper.CreateLookup32();
        }
    }
}
