using PCSC;
using PCSC.Iso7816;
using System.Security.Cryptography;
using System.Text;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    public class atrustCardOS53 : ICardService
    {
        public byte[] GetPin() => Encoding.ASCII.GetBytes("123456");

        public bool checkApplication(SCardReader cardReader, IsoReader reader)
        {
            try
            {
                if (cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {

                    var AID_SIG = new byte[] { 0xD0, 0x40, 0x00, 0x00, 0x22, 0x00, 0x01 };
                    var apdu = new CommandApdu(IsoCase.Case4Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x04,
                        P2 = 0x0,
                        Data = AID_SIG,
                        Le = 0x0
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return false;
                    };
                }
            }
            finally
            {
                cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

            return true;
        }

        public byte[] readCertificates(SCardReader cardReader, IsoReader reader, bool onlyFirst = false, bool verify = false)
        {
            try
            {
                if (cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {
                    //sendSelectFID(DF_SIG);
                    var DF_SIG = new byte[] { 0xDF, 0x01 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x00,
                        P2 = 0x0c,
                        Data = DF_SIG
                    };
                    var response = reader.Transmit(apdu);
                }
                {
                    //sendSelectFID(EF_C_CH_DS);
                    var EF_C_CH_DS = new byte[] { 0xc0, 0x00 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x00,
                        P2 = 0x0c,
                        Data = EF_C_CH_DS
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }

                using (var ms = new MemoryStream())
                {
                    var offset = 0;
                    while (true)
                    {
                        var apdu = new CommandApdu(IsoCase.Case2Short, reader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0xb0,
                            P1 = (byte) (offset >> 8),
                            P2 = (byte) offset,
                            Le = 0x00
                        };
                        var response = reader.Transmit(apdu);
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
            finally
            {
                cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

        }

        public byte[] readCIN(SCardReader cardReader, IsoReader reader)
        {
            try
            {
                if (cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {
                    var MASTER_FILE = new byte[] { 0x3F, 0x00 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = MASTER_FILE
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return null;
                    };
                }

                {
                    var EF_CIN_CSN = new byte[] { 0xD0, 0x01 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = EF_CIN_CSN,
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return null;
                    };
                }

                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xb0,
                        P1 = 0x0,
                        P2 = 0x0,
                        Le = 0x0
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return response.GetData();
                    }
                }
            }
            finally
            {
                cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

            return null;
        }

        public byte[] sign(SCardReader cardReader, IsoReader reader, byte[] pin, byte[] data, bool selectCard = true)
        {
            using var sha256 = SHA256.Create();
            var sha256hash = sha256.ComputeHash(data);

            try
            {

                if (cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                if (selectCard)
                {
                    {
                        // sendSelectFID(DF_SIG);
                        var DF_SIG = new byte[] { 0xDF, 0x70 };
                        var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0xa4,
                            P1 = 0x00,
                            P2 = 0x0c,
                            Data = DF_SIG
                        };
                        var response = reader.Transmit(apdu);
                    }
                }

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
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x20,
                        P1 = 0x00,
                        P2 = 0x81,
                        Data = _pin_bytes
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }

                {
                    var apdu = new CommandApdu(IsoCase.Case4Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x2a,
                        P1 = 0x9e,
                        P2 = 0x9a,
                        Data = sha256hash,
                        Le = 64
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                    else if (!response.HasData)
                    {
                        throw new Exception("APDU-Fehler bei COMPUTE DIGITAL SIGNATURE: keine Daten empfangen");
                    }
                    else
                    {
                        var signatur = response.GetData();
                        return signatur;
                    }
                }
            }
            finally
            {
                cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

        }

        public bool selectApplication(SCardReader cardReader, IsoReader reader)
        {
            try
            {
                if (cardReader.BeginTransaction() != PCSC.SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {
                    //sendSelectFID(DF_SIG);
                    var DF_SIG = new byte[] { 0xDF, 0x01 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x00,
                        P2 = 0x0c,
                        Data = DF_SIG
                    };
                    var resse = reader.Transmit(apdu);
                }
            }
            catch (Exception x)
            {
                return false;
            }
            finally
            {
                cardReader.EndTransaction(PCSC.SCardReaderDisposition.Leave);
            }

            return true;
        }
    }
}
