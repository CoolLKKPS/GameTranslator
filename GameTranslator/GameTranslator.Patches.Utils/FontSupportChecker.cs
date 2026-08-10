using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace GameTranslator.Patches.Utils
{
    internal static class FontSupportChecker
    {
        private static readonly ConcurrentDictionary<TMP_FontAsset, bool> _availableFonts = new ConcurrentDictionary<TMP_FontAsset, bool>();
        private static readonly Dictionary<char, bool> _characterSupportCache = new Dictionary<char, bool>();
        private static readonly LRUCache<string, string> _textCache = new LRUCache<string, string>(1000);
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();

        internal static void InitializeFonts()
        {
            if (_isInitialized) return;
            lock (_lockObject)
            {
                if (_isInitialized) return;
                _availableFonts.Clear();
                _characterSupportCache.Clear();
                _textCache.Clear();
                if (TranslatePlugin.changeFont.Value)
                {
                    var fallbackFonts = FontCache.GetOrCreateFallbackFontTextMeshPro();
                    foreach (var fontObj in fallbackFonts)
                    {
                        var mainFont = fontObj as TMP_FontAsset;
                        if (mainFont != null)
                        {
                            AddFont(mainFont);
                        }
                    }
                }
                var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var font in allFonts)
                {
                    if (font != null && !_availableFonts.ContainsKey(font))
                    {
                        AddFont(font);
                    }
                }
                if (_availableFonts.Count > 0)
                {
                    _isInitialized = true;
                }
                TranslatePlugin.logger.LogInfo($"FontSupportChecker initialized with {_availableFonts.Count} fonts");
            }
        }

        private static void AddFont(TMP_FontAsset font)
        {
            if (font == null || _availableFonts.ContainsKey(font)) return;
            _availableFonts.TryAdd(font, true);
            if (font.fallbackFontAssetTable != null)
            {
                foreach (var fallbackFont in font.fallbackFontAssetTable)
                {
                    if (fallbackFont != null && !_availableFonts.ContainsKey(fallbackFont))
                    {
                        _availableFonts.TryAdd(fallbackFont, true);
                    }
                }
            }
        }

        internal static void RegisterFont(TMP_FontAsset font)
        {
            if (font == null) return;
            lock (_lockObject)
            {
                AddFont(font);
                _isInitialized = true;
                _characterSupportCache.Clear();
                _textCache.Clear();
                TranslatePlugin.logger.LogDebug(string.IsNullOrEmpty(font.name) ? "Registered new font (unnamed)" : $"Registered new font: {font.name}");
            }
        }

        private static bool IsCharacterSupported(char character)
        {
            if (!_isInitialized) return true;
            if (_characterSupportCache.TryGetValue(character, out var isSupported))
            {
                return isSupported;
            }
            isSupported = _availableFonts.Keys.Any(font => font != null && font.HasCharacter(character, true, true));
            _characterSupportCache[character] = isSupported;
            return isSupported;
        }

        internal static string ReplaceUnsupportedCharacters(string text, TMPro.TMP_Text textComponent = null)
        {
            if (string.IsNullOrEmpty(text) || !TranslatePlugin.replaceUnsupportedCharacters.Value)
                return text;
            if (!_isInitialized)
                InitializeFonts();
            if (_textCache.TryGetValue(text, out var cachedResult))
            {
                return cachedResult;
            }
            bool allCharactersSupported = true;
            foreach (char c in text)
            {
                if (!IsCharacterSupported(c))
                {
                    allCharactersSupported = false;
                    break;
                }
            }
            if (allCharactersSupported)
            {
                _textCache.Add(text, text);
                return text;
            }
            StringBuilder stringBuilder = new StringBuilder();
            bool hasReplacement = false;
            foreach (char c in text)
            {
                if (char.IsControl(c))
                {
                    stringBuilder.Append(c);
                }
                else if (IsCharacterSupported(c))
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    stringBuilder.Append('□');
                    hasReplacement = true;
                }
            }
            string result = stringBuilder.ToString();
            if (hasReplacement && TranslatePlugin.showOtherDebug.Value)
            {
                try { TranslatePlugin.logger.LogInfo($"[FontSupport] Replaced unsupported characters for text: '{text}' -> '{result}'"); }
                catch (IndexOutOfRangeException) { }
            }
            _textCache.Add(text, result);
            return result;
        }

        // Still using for other purposes
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _characterSupportCache.Clear();
                _textCache.Clear();
                TranslatePlugin.logger.LogDebug("FontSupportChecker cache cleared");
            }
        }

        // Still using for other purposes
        public static string GetStats()
        {
            return $"Fonts: {_availableFonts.Count}, CharacterCache: {_characterSupportCache.Count}, TextCache: {_textCache.Count}";
        }
    }

    internal class LRUCache<TKey, TValue>(int capacity)
    {
        private readonly int _capacity = capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cacheMap = new(capacity);
        private readonly LinkedList<CacheItem> _lruList = new();

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                value = node.Value.Value;
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return true;
            }
            value = default;
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            if (_cacheMap.TryGetValue(key, out var existingNode))
            {
                _lruList.Remove(existingNode);
            }
            else if (_cacheMap.Count >= _capacity)
            {
                RemoveLeastRecentlyUsed();
            }
            var newNode = new LinkedListNode<CacheItem>(new CacheItem { Key = key, Value = value });
            _lruList.AddFirst(newNode);
            _cacheMap[key] = newNode;
        }

        public void Clear()
        {
            _cacheMap.Clear();
            _lruList.Clear();
        }

        private void RemoveLeastRecentlyUsed()
        {
            var lastNode = _lruList.Last;
            if (lastNode != null)
            {
                _cacheMap.Remove(lastNode.Value.Key);
                _lruList.RemoveLast();
            }
        }

        private class CacheItem
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
        }

        public int Count => _cacheMap.Count;
    }
}
