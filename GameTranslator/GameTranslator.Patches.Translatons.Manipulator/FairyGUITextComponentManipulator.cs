using System.Reflection;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons.Manipulator
{
    internal class FairyGUITextComponentManipulator : ITextComponentManipulator
    {
        public FairyGUITextComponentManipulator()
        {
            this._html = UnityTypes.TextField.ClrType.CachedField("html") ?? UnityTypes.TextField.ClrType.CachedFieldByIndex(3, typeof(bool), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            this._text = UnityTypes.TextField.ClrType.CachedProperty("text");
            this._htmlText = UnityTypes.TextField.ClrType.CachedProperty("htmlText");
        }

        public string GetText(object ui)
        {
            string text;
            if ((bool)this._html.Get(ui))
            {
                text = (string)this._htmlText.Get(ui);
            }
            else
            {
                text = (string)this._text.Get(ui);
            }
            return text;
        }

        public void SetText(object ui, string text)
        {
            if ((bool)this._html.Get(ui))
            {
                this._htmlText.Set(ui, text);
                return;
            }
            this._text.Set(ui, text);
        }

        private readonly CachedField _html;

        private readonly CachedProperty _htmlText;

        private readonly CachedProperty _text;
    }
}
