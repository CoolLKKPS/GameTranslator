namespace GameTranslator.Patches.Translatons.Manipulator
{
    internal interface ITextComponentManipulator
    {
        string GetText(object ui);

        void SetText(object ui, string text);
    }
}
