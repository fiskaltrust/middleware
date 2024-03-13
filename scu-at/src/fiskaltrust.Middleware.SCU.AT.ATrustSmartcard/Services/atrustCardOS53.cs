using PCSC;
using PCSC.Iso7816;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    internal class AtrustCardOS53 : CardService
    {
        public AtrustCardOS53(ISCardReader cardReader, IIsoReader isoReader) : base(cardReader, isoReader){ }
        
        public override byte[] Sign(byte[] data, bool selectCard = true)
        {
            using var sha256 = SHA256.Create();
            var sha256hash = sha256.ComputeHash(data);

            try
            {

                if (_cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                if (selectCard)
                {
                    {
                        // sendSelectFID(DF_SIG);
                        var DF_SIG = new byte[] { 0xDF, 0x70 };
                        var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0xa4,
                            P1 = 0x00,
                            P2 = 0x0c,
                            Data = DF_SIG
                        };
                        var response = _isoReader.Transmit(apdu);
                        //TODO returns error, but wors
                        //if (response.SW1 != 0x90 || response.SW2 != 0x00)
                        //{
                        //    throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                        //}
                    }
                }

                VeryfyPin(GetPinBytes(GetPin()), 0x81);

                return ComputeDigitalSignatute(IsoCase.Case4Short, sha256hash, 64);
               
            }
            finally
            {
                _cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

        }
        private byte[] GetPinBytes(byte[] pin)
        {
            var _pin_bytes = new byte[8] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            _pin_bytes[0] = (byte) (0x20 + pin.Length);
            var _pin_text = Encoding.ASCII.GetString(pin);
            for (var i = 0; i < _pin_text.Length; i++)
            {
                if (i % 2 == 0)
                {
                    var x = Convert.ToByte("0x" + _pin_text.Substring(i, 1) + "F", 16);
                    _pin_bytes[1 + (i / 2)] &= x;
                }
                else
                {
                    var x = Convert.ToByte("0xF" + _pin_text.Substring(i, 1), 16);
                    _pin_bytes[1 + (i / 2)] &= x;
                }
            }

            return _pin_bytes;
        }
    }
    
}
