#if WCF
using System.ServiceModel.Channels;
using System.Xml;

namespace fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf.Formatting
{
    public class RawDataWriter : BodyWriter
    {
        private readonly byte[] _data;

        public RawDataWriter(byte[] data) : base(true)
        {
            _data = data;
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Binary");
            writer.WriteBase64(_data, 0, _data.Length);
            writer.WriteEndElement();
        }
    }
}
#endif