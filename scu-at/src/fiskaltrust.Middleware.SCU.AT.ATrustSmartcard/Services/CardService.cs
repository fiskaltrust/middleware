using System.Net.NetworkInformation;
using System.Text;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    public abstract class CardService
    {
        protected ISCardReader _cardReader;
        protected IIsoReader _isoReader;

        public CardService(ISCardReader cardReader, IIsoReader isoReader)
        {
            _cardReader = cardReader;
            _isoReader = isoReader;
        }
        protected byte[] GetPin() => Encoding.ASCII.GetBytes("123456");

        public virtual bool CheckApplication()
        {
            try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    var AID_SIG = new byte[] { 0xD0, 0x40, 0x00, 0x00, 0x22, 0x00, 0x01 };
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

        public virtual byte[] ReadCertificates(bool onlyFirst = false, bool verify = false)
        {
            try
            {
                if (_cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                var DF_SIG = new byte[] { 0xDF, 0x01 };
                var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                {
                    CLA = 0x0,
                    INS = 0xa4,
                    P1 = 0x0,
                    P2 = 0x0c,
                    Data = DF_SIG,
                };
                _isoReader.Transmit(apdu);

                var EF_C_CH_DS = new byte[] { 0xc0, 0x00 };
                FID(EF_C_CH_DS);

                return GetCertificates(onlyFirst, verify);
            }
            finally
            {
                _cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }
        }

        public virtual byte[]? ReadCIN()
        {
            try
            {
                if (_cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    var MASTER_FILE = new byte[] { 0x3F, 0x00 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = MASTER_FILE
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return null;
                    };
                }

                {
                    var EF_CIN_CSN = new byte[] { 0xD0, 0x01 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = EF_CIN_CSN,
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
                        P1 = 0x0,
                        P2 = 0x0,
                        Le = 0x0
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return response.GetData();
                    }
                }
            }
            finally
            {
                _cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

            return null;
        }

        public virtual bool SelectApplication()
        {
            try
            {
                if (_cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {_isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    //sendSelectFID(DF_SIG);
                    var DF_SIG = new byte[] { 0xDF, 0x01 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x00,
                        P2 = 0x0c,
                        Data = DF_SIG
                    };
                    var resse = _isoReader.Transmit(apdu);
                }
            }
            catch (Exception x)
            {
                return false;
            }
            finally
            {
                _cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

            return true;
        }

        public abstract byte[] Sign(byte[] data, bool selectCard = true);

        protected void FID(byte[] data)
        {
            //sendSelectFID;
            var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
            {
                CLA = 0x0,
                INS = 0xa4,
                P1 = 0x0,
                P2 = 0x0c,
                Data = data,
            };
            var response = _isoReader.Transmit(apdu);
            if (response.SW1 != 0x90 || response.SW2 != 0x00)
            {
                throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
            }
        }

        protected void VeryfyPin(byte[] data, byte p2)
        {
            // VERIFY (page 100)
            var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
            {
                CLA = 0x0,
                INS = 0x20,
                P1 = 0x00,
                P2 = p2,
                Data = data
            };
            var response = _isoReader.Transmit(apdu);
            if (response.SW1 != 0x90 || response.SW2 != 0x00)
            {
                throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
            }
        }

        protected void ManageSecurityEnvironment()
        {
            // MANAGE SECURITY ENVIRONMENT (page 67)
            var TLV = new byte[] { 0x84, 0x01, 0x88, 0x80, 0x01, 0x44 };
            var apdu = new CommandApdu(IsoCase.Case3Short, _isoReader.ActiveProtocol)
            {
                CLA = 0x0,
                INS = 0x22,
                P1 = 0x41,
                P2 = 0xb6,
                Data = TLV
            };
            var response = _isoReader.Transmit(apdu);
            if (response.SW1 != 0x90 || response.SW2 != 0x00)
            {
                throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
            }
        }
        protected byte[] ComputeDigitalSignatute(IsoCase isoCase, byte[]? data = null, int p3 = 0x0)
        {
            // PSO – COMPUTE DIGITAL SIGNATURE (page 73)
            var apdu = new CommandApdu(isoCase, _isoReader.ActiveProtocol)
            {
                CLA = 0x0,
                INS = 0x2a,
                P1 = 0x9e,
                P2 = 0x9a,
                Le = p3
            };

            if (isoCase == IsoCase.Case4Short && data != null)
            {
                apdu.Data = data;
            }
            var response = _isoReader.Transmit(apdu);
            if (response.SW1 != 0x90 || response.SW2 != 0x00)
            {
                throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
            }
            else if (!response.HasData)
            {
                throw new Exception("APDU-Fehler bei COMPUTE DIGITAL SIGNATURE: keine Daten empfangen");
            }

            return response.GetData();
        }

        protected byte[] GetCertificates(bool onlyFirst = false, bool verify = false)
        {
            using (var ms = new MemoryStream())
            {
                var offset = 0;
                while (true)
                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, _isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xb0,
                        P1 = (byte) (offset >> 8),
                        P2 = (byte) offset,
                        Le = 0x00
                    };
                    var response = _isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        break;
                    }

                    var data = response.GetData();
                    ms.Write(data, 0, data.Length);
                    if (onlyFirst)
                    {
                        break;
                    }
                    offset += 256;
                }


                if (verify)
                {
                    ms.Position = 0;
                    var certificates = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificates(ms);
                    foreach (Org.BouncyCastle.X509.X509Certificate certificate in certificates)
                    {
                        certificate.CheckValidity();
                    }
                }

                return ms.ToArray();
            }
        }
    }

}
