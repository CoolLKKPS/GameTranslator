using System;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons.Manipulator
{
    internal class UguiNovelTextComponentManipulator : ITextComponentManipulator
    {
        public UguiNovelTextComponentManipulator(Type type)
        {
            this._type = type;
            this._property = type.CachedProperty(UguiNovelTextComponentManipulator.TextPropertyName);
        }

        public string GetText(object ui)
        {
            return (string)this._property.Get(ui);
        }

        public void SetText(object ui, string text)
        {
            this._property.Set(ui, text);
            UnityTypes.UguiNovelText_Methods.SetAllDirty.Invoke(ui);
            object obj = UnityTypes.UguiNovelText_Properties.TextGenerator.Get(ui);
            UnityTypes.UguiNovelTextGenerator_Methods.Refresh.Invoke(obj);
        }

        private static readonly string TextPropertyName = "text";

        private readonly Type _type;

        private readonly CachedProperty _property;
    }
}
