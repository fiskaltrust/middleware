using PCSC;
using PCSC.Iso7816;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    internal class AtrustACOS : CardService
    {
        public AtrustACOS(ISCardReader cardReader, IIsoReader isoReader) : base(cardReader, isoReader) { }

        public override bool CheckApplication()
        {
           try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    var AID_DEC = new byte[] { 0xA0, 0x00, 0x00, 0x01, 0x18, 0x45, 0x4E };
                    var apdu = new CommandApdu(IsoCase.Case4Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x04,
                        P2 = 0x0,
                        Data = AID_DEC,
                        Le = 0x0
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return false;
                    };
                }

                {
                    var AID_SIG = new byte[] { 0xA0, 0x00, 0x00, 0x01, 0x18, 0x45, 0x43 };
                    var apdu = new CommandApdu(IsoCase.Case4Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x04,
                        P2 = 0x0,
                        Data = AID_SIG,
                        Le = 0x0
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return false;
                    };
                }
            }
            finally
            {
               _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return true;
        }

        public override byte[] ReadCertificates(bool onlyFirst = false, bool verify = false)
        {
            try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                var DF_SIG = new byte[] { 0xdf, 0x70 };
                FID(DF_SIG);
                
                var EF_C_CH_DS = new byte[] { 0xc0, 0x00 };
                FID(EF_C_CH_DS);

                return GetCertificates(onlyFirst, verify);
            }
            finally
            {
                _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }
        }
        public override byte[]? ReadCIN()
        {
            try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    //sendSelectFID(DF_DEC);
                    var DF_DEC = new byte[] { 0xDF, 0x71 };
                    var apdu = new CommandApdu(IsoCase.Case4Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = DF_DEC,
                        Le = 0x0
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return null;
                    };
                }

                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xb0,
                        P1 = 0x86,
                        P2 = 0x0,
                        Le = 0x0
                    };
                    var response =_isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return response.GetData();
                    }
                }
            }
            finally
            {
                _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return null;
        }
        public override bool SelectApplication()
        {
            try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                // sendSelectFID(DF_SIG);
                var DF_SIG = new byte[] { 0xDF, 0x70 };
                FID(DF_SIG);
                ManageSecurityEnvironment();
                
            }
            catch (Exception x)
            {
                return false;
            }
            finally
            {
                _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return true;
        }

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
                    FID(new byte[] { 0xDF, 0x70 });
                    ManageSecurityEnvironment();
                }

                {
                    var pin = GetPin();
                    Array.Resize(ref pin, 8);
                    VeryfyPin(pin, 0x81);
                }

                {
                    // PSO – PUT HASH (page 79)
                    var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x2a,
                        P1 = 0x90,
                        P2 = 0x81,
                        Data = sha256hash
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }

                return ComputeDigitalSignatute(IsoCase.Case2Short);
                
            }
            finally
            {
                _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

        }

        
    }
}
