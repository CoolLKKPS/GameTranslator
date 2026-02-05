using UnityEngine;

namespace GameTranslator.Patches.Utils.Textures
// AutoTranslator Codes, under MIT license
{
    internal interface ITextureLoader
    {
        void Load(Texture2D texture, byte[] data);

        bool Verify();
    }
}