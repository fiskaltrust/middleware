using PCSC;
using PCSC.Iso7816;
using System.Security.Cryptography;
using System.Text;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    internal class AtrustAcosID : CardService
    {
        public AtrustAcosID(ISCardReader cardReader, IIsoReader isoReader):base(cardReader, isoReader) { }
       
        public override byte[] Sign(byte[] data, bool selectCard = true)
        {
            using var sha256 = SHA256.Create();
            var sha256hash = sha256.ComputeHash(data);

            try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                if (selectCard)
                {
                    {
                        var DF_SIG = new byte[] { 0xDF, 0x01 };
                        var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0xa4,
                            P1 = 0x00,
                            P2 = 0x0c,
                            Data = DF_SIG
                        };
                        var response = _isoReader.Transmit(apdu);
                    }
                }

                {
                    var pinBytes = GetFormat2PIN(Encoding.ASCII.GetString(GetPin()));
                    VeryfyPin(pinBytes, 0x8A);
                }

                return ComputeDigitalSignatute(IsoCase.Case4Short, sha256hash, 64);
                
            }
            finally
            {
                _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

        }

        private byte[] GetFormat2PIN(string pin)
        {
            /*
            Format 2 PIN block
            The format 2 PIN block is constructed thus:
            1 nibble with the value of 2, which identifies this as a format 2 block
            1 nibble encoding the length N of the PIN
            N nibbles, each encoding one PIN digit
            14-N nibbles, each holding the "fill" value 15
            */
            if (pin.Length != 6 && pin.Length != 4)
            {
                throw new Exception("Wrong PIN length");
            }
            var ba = new byte[8];
            ba[0] = (byte) ((2 << 4) | pin.Length);
            var ca = pin.ToCharArray();
            ba[1] = (byte) (((ca[0] - 0x30) << 4) | (ca[1] - 0x30));
            ba[2] = (byte) (((ca[2] - 0x30) << 4) | (ca[3] - 0x30));
            ba[3] = pin.Length == 6 ? (byte) (((ca[4] - 0x30) << 4) | (ca[5] - 0x30)) : (byte) 0xFF;
            ba[4] = 0xFF;
            ba[5] = 0xFF;
            ba[6] = 0xFF;
            ba[7] = 0xFF;
            
            return ba;
        }
    }
}
