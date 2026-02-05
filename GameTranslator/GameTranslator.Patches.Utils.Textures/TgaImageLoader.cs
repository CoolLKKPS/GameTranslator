using System.IO;
using UnityEngine;

namespace GameTranslator.Patches.Utils.Textures
// AutoTranslator Codes, under MIT license
{
    class TgaImageLoader : ITextureLoader
    {
        public void Load(Texture2D texture, byte[] data)
        {
            if (texture == null && data == null) return;

            var format = texture.format;

            using (var stream = new MemoryStream(data))
            using (var binaryReader = new BinaryReader(stream))
            {
                binaryReader.BaseStream.Seek(12L, SeekOrigin.Begin);
                short num1 = binaryReader.ReadInt16();
                short num2 = binaryReader.ReadInt16();
                int num3 = (int)binaryReader.ReadByte();
                binaryReader.BaseStream.Seek(1L, SeekOrigin.Current);
                Color32[] colors = new Color32[(int)num1 * (int)num2];
                if (format == TextureFormat.RGB24)
                {
                    if (num3 == 32)
                    {
                        for (int index = 0; index < (int)num1 * (int)num2; ++index)
                        {
                            byte b = binaryReader.ReadByte();
                            byte g = binaryReader.ReadByte();
                            byte r = binaryReader.ReadByte();
                            binaryReader.ReadByte();
                            colors[index] = new Color32(r, g, b, byte.MaxValue);
                        }
                    }
                    else
                    {
                        for (int index = 0; index < (int)num1 * (int)num2; ++index)
                        {
                            byte b = binaryReader.ReadByte();
                            byte g = binaryReader.ReadByte();
                            byte r = binaryReader.ReadByte();
                            colors[index] = new Color32(r, g, b, byte.MaxValue);
                        }
                    }
                }
                else
                {
                    if (num3 == 32)
                    {
                        for (int index = 0; index < (int)num1 * (int)num2; ++index)
                        {
                            byte b = binaryReader.ReadByte();
                            byte g = binaryReader.ReadByte();
                            byte r = binaryReader.ReadByte();
                            byte a = binaryReader.ReadByte();
                            colors[index] = new Color32(r, g, b, a);
                        }
                    }
                    else
                    {
                        for (int index = 0; index < (int)num1 * (int)num2; ++index)
                        {
                            byte b = binaryReader.ReadByte();
                            byte g = binaryReader.ReadByte();
                            byte r = binaryReader.ReadByte();
                            colors[index] = new Color32(r, g, b, byte.MaxValue);
                        }
                    }
                }
                texture.SetPixels32(colors);
                texture.Apply();
            }
        }

        public bool Verify()
        {
            Load(null, null);
            return true;
        }
    }
}