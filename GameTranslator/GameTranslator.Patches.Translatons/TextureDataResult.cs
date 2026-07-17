namespace GameTranslator.Patches.Translatons
{
    internal class TextureDataResult
    {
        public TextureDataResult(byte[] data)
        {
            this.Data = data;
            /*
            this.NonReadable = nonReadable;
            this.CalculationTime = calculationTime;
            */
        }

        public byte[] Data { get; }

        /*
        public bool NonReadable { get; }

        public float CalculationTime { get; set; }
        */
    }
}
