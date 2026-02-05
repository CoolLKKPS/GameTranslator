using UnityEngine;

namespace GameTranslator.Patches.Translatons
{
    internal class ImageTranslationInfo
    {
        public bool IsTranslated { get; set; }

        public XUnity.Common.Utilities.WeakReference<Texture2D> Original { get; private set; }

        public void Initialize(Texture2D texture)
        {
            this.Original = XUnity.Common.Utilities.WeakReference<Texture2D>.Create(texture);
        }

        public void Reset(Texture2D newTexture)
        {
            this.IsTranslated = false;
            this.Original = XUnity.Common.Utilities.WeakReference<Texture2D>.Create(newTexture);
        }
    }
}
