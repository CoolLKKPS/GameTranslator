using System;
using System.Collections.Generic;
using System.IO;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Translatons
{
    public class TranslatedImage
    {
        public TranslatedImage(string fileName, byte[] data, TranslatedImage.ITranslatedImageSource source)
        {
            this._source = source;
            this.FileName = fileName;
            this.Data = data;
            this.ImageFormat = TranslatedImage.Formats[Path.GetExtension(fileName)];
        }

        public string FileName { get; }

        internal TranslateExtensions.ImageFormat ImageFormat { get; }

        private byte[] Data
        {
            get
            {
                if (this._source == null)
                {
                    return this._data;
                }
                else
                {
                    var target = this._weakData.Target;
                    if (target != null)
                    {
                        return target;
                    }
                    return null;
                }
            }
            set
            {
                if (this._source == null)
                {
                    this._data = value;
                }
                else
                {
                    this._weakData = XUnity.Common.Utilities.WeakReference<byte[]>.Create(value);
                }
            }
        }

        public byte[] GetData()
        {
            var data = this.Data;
            if (data != null)
            {
                return data;
            }

            if (this._source != null)
            {
                data = this._source.GetData();
                this.Data = data;

                XuaLogger.AutoTranslator.Debug("Image loaded in GetData: " + this.FileName + ".");
            }

            return data;
        }

        private static readonly Dictionary<string, TranslateExtensions.ImageFormat> Formats = new Dictionary<string, TranslateExtensions.ImageFormat>(StringComparer.OrdinalIgnoreCase)
        {
            {
                ".png",
                TranslateExtensions.ImageFormat.PNG
            },
            {
                ".tga",
                TranslateExtensions.ImageFormat.TGA
            }
        };

        private readonly TranslatedImage.ITranslatedImageSource _source;

        private XUnity.Common.Utilities.WeakReference<byte[]> _weakData;

        private byte[] _data;

        public interface ITranslatedImageSource
        {
            byte[] GetData();
        }
    }
}
