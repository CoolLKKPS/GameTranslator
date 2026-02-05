namespace GameTranslator.Patches.Translatons
{
    internal class TextureDataResult
    {
        public TextureDataResult(byte[] data, bool nonReadable, float calculationTime)
        {
            this.Data = data;
            this.NonReadable = nonReadable;
            this.CalculationTime = calculationTime;
        }

        public byte[] Data { get; }

        public bool NonReadable { get; }

        public float CalculationTime { get; set; }
    }
}
