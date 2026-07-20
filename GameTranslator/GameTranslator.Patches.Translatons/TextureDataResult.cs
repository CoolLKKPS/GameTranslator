namespace GameTranslator.Patches.Translatons
{
    internal class TextureDataResult
    {
        public TextureDataResult(byte[] data)
        {
            this.Data = data;
        }

        public byte[] Data { get; }
    }
}
