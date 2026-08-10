using UnityEngine;
using XUnity.Common.Constants;

namespace GameTranslator.Patches.Utils.Textures
// AutoTranslator Codes, under MIT license
{
    internal class LoadImageImageLoader : ITextureLoader
    {
        public void Load(Texture2D texture, byte[] data)
        {
            if (UnityTypes.ImageConversion_Methods.LoadImage is { } icLoader)
                icLoader(texture, data, false);
            else if (UnityTypes.Texture2D_Methods.LoadImage is { } tLoader)
                tLoader(texture, data);
        }

        public bool Verify()
        {
            return UnityTypes.Texture2D_Methods.LoadImage != null || UnityTypes.ImageConversion_Methods.LoadImage != null;
        }
    }
}