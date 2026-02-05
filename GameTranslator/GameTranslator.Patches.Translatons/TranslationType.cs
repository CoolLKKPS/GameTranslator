using System;

namespace GameTranslator.Patches.Translatons
{
    [Flags]
    public enum TranslationType
    {
        None = 0x00,
        Full = 0x01,
        Token = 0x02
    }
}