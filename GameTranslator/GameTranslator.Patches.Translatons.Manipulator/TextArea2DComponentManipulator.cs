using System;
using System.Reflection;
using XUnity.Common.Constants;
using XUnity.Common.Utilities;

namespace GameTranslator.Patches.Translatons.Manipulator
{
    internal class TextArea2DComponentManipulator : ITextComponentManipulator
    {
        public TextArea2DComponentManipulator()
        {
            FieldInfo field = UnityTypes.AdvPage.ClrType.GetField("textData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            this.set_textData = CustomFastReflectionHelper.CreateFastFieldSetter<object, object>(field);
            FieldInfo field2 = UnityTypes.AdvPage.ClrType.GetField("status", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            this.set_status = CustomFastReflectionHelper.CreateFastFieldSetter<object, object>(field2);
            FieldInfo field3 = UnityTypes.AdvPage.ClrType.GetField("isInputSendMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            this.set_isInputSendMessage = CustomFastReflectionHelper.CreateFastFieldSetter<object, bool>(field3);
            FieldInfo field4 = UnityTypes.AdvPage.ClrType.GetField("nameText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            this.set_nameText = CustomFastReflectionHelper.CreateFastFieldSetter<object, object>(field4);
            this._text = UnityTypes.TextArea2D.ClrType.CachedProperty("text");
            this._TextData = UnityTypes.TextArea2D.ClrType.CachedProperty("TextData");
        }

        public string GetText(object ui)
        {
            object obj = this._TextData.Get(ui);
            string text;
            if (obj != null)
            {
                text = obj.GetExtensionData<string>();
            }
            else
            {
                text = (string)this._text.Get(ui);
            }
            return text;
        }

        public void SetText(object ui, string text)
        {
            if (UnityTypes.AdvUiMessageWindow != null && UnityTypes.AdvPage != null)
            {
                global::UnityEngine.Object @object = global::UnityEngine.Object.FindObjectOfType(UnityTypes.AdvUiMessageWindow.UnityType);
                object obj = UnityTypes.AdvUiMessageWindow_Fields.text.Get(@object);
                object obj2 = UnityTypes.AdvUiMessageWindow_Fields.nameText.Get(@object);
                if (object.Equals(obj, ui))
                {
                    global::UnityEngine.Object object2 = global::UnityEngine.Object.FindObjectOfType(UnityTypes.AdvPage.UnityType);
                    object obj3 = Activator.CreateInstance(UnityTypes.TextData.ClrType, new object[] { text });
                    this._TextData.Set(ui, obj3);
                    this.set_textData(object2, obj3);
                    this.set_status(object2, 0);
                    this.set_isInputSendMessage(object2, false);
                    return;
                }
                if (object.Equals(obj2, ui))
                {
                    global::UnityEngine.Object object3 = global::UnityEngine.Object.FindObjectOfType(UnityTypes.AdvPage.UnityType);
                    object obj4 = Activator.CreateInstance(UnityTypes.TextData.ClrType, new object[] { text });
                    this._TextData.Set(ui, obj4);
                    this.set_nameText(object3, text);
                    return;
                }
            }
            object obj5 = Activator.CreateInstance(UnityTypes.TextData.ClrType, new object[] { text });
            this._text.Set(ui, text);
            this._TextData.Set(ui, obj5);
        }

        private readonly Action<object, object> set_status;

        private readonly Action<object, object> set_textData;

        private readonly Action<object, object> set_nameText;

        private readonly Action<object, bool> set_isInputSendMessage;

        private readonly CachedProperty _text;

        private readonly CachedProperty _TextData;
    }
}
