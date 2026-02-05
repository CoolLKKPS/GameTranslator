using GameTranslator.Patches.Translatons;
using System;
using System.Collections.Generic;
using UnityEngine;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Utils.Textures
// AutoTranslator Codes, under MIT license
{
    internal static class TextureLoader
    {
        private static readonly Dictionary<TranslateExtensions.ImageFormat, ITextureLoader> Loaders
            = new Dictionary<TranslateExtensions.ImageFormat, ITextureLoader>();

        static TextureLoader()
        {
            Register(TranslateExtensions.ImageFormat.PNG, new LoadImageImageLoader());
            Register(TranslateExtensions.ImageFormat.TGA, new TgaImageLoader());
        }

        public static bool Register(TranslateExtensions.ImageFormat format, ITextureLoader loader)
        {
            try
            {
                var verified = loader.Verify();
                if (verified)
                {
                    Loaders[format] = loader;
                    return true;
                }
            }
            catch (Exception e)
            {
                XuaLogger.AutoTranslator.Warn(e, "An image loader could not be registered.");
            }

            return false;
        }

        public static void Load(Texture2D texture, byte[] data, TranslateExtensions.ImageFormat imageFormat)
        {
            if (Loaders.TryGetValue(imageFormat, out var loader))
            {
                loader.Load(texture, data);
            }
        }
    }
}