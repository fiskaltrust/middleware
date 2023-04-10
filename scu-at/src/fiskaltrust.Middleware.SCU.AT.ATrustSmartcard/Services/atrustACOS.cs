using PCSC;
using PCSC.Iso7816;
using System.Security.Cryptography;
using System.Text;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    internal class atrustACOS : ICardService
    {
        public byte[] GetPin() => Encoding.ASCII.GetBytes("123456");

        public bool checkApplication(SCardReader cardReader, IsoReader reader)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {
                    var AID_DEC = new byte[] { 0xA0, 0x00, 0x00, 0x01, 0x18, 0x45, 0x4E };
                    var apdu = new CommandApdu(IsoCase.Case4Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x04,
                        P2 = 0x0,
                        Data = AID_DEC,
                        Le = 0x0
                    };
                    var response = reader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return false;
                    };
                }

                {
                    var AID_SIG = new byte[] { 0xA0, 0x00, 0x00, 0x01, 0x18, 0x45, 0x43 };
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
               cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return true;
        }
        public byte[] readCertificates(SCardReader cardReader, IsoReader isoReader, bool onlyFirst = false, bool verify = false)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {isoReader.ReaderName}  BeginTransaction failed");
                }


                {
                    //sendSelectFID(DF_SIG);
                    var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x00,
                        P2 = 0x0c,
                        Data = new byte[] { 0xdf, 0x70 }
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }
                {
                    //sendSelectFID(EF_C_CH_DS);
                    var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x00,
                        P2 = 0x0c,
                        Data = new byte[] { 0xc0, 0x02 }
                    };
                    var response = isoReader.Transmit(apdu);
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
                        var apdu = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0xb0,
                            P1 = (byte) (offset >> 8),
                            P2 = (byte) offset,
                            Le = 0x00
                        };
                        var response = isoReader.Transmit(apdu);
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

        }
        public byte[] readCIN(SCardReader cardReader, IsoReader isoReader)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    //sendSelectFID(DF_DEC);
                    var DF_DEC = new byte[] { 0xDF, 0x71 };
                    var apdu = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = DF_DEC,
                        Le = 0x0
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return null;
                    };
                }

                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xb0,
                        P1 = 0x86,
                        P2 = 0x0,
                        Le = 0x0
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        return response.GetData();
                    }
                }
            }
            finally
            {
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return null;
        }

        public byte[] sign(SCardReader cardReader, IsoReader isoReader, byte[] pin, byte[] data, bool selectCard = true)
        {
            using var sha256 = SHA256.Create();
            var sha256hash = sha256.ComputeHash(data);

            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {isoReader.ReaderName}  BeginTransaction failed");
                }

                if (selectCard)
                {
                    {
                        // sendSelectFID(DF_SIG);
                        var DF_SIG = new byte[] { 0xDF, 0x70 };
                        var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0xa4,
                            P1 = 0x0,
                            P2 = 0x0c,
                            Data = DF_SIG,
                        };
                        var response = isoReader.Transmit(apdu);
                        if (response.SW1 != 0x90 || response.SW2 != 0x00)
                        {
                            throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                        }
                    }

                    {
                        // MANAGE SECURITY ENVIRONMENT (page 67)
                        var TLV = new byte[] {  0x84,  0x01,  0x88,  0x80,  0x01,  0x44 };
                        var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                        {
                            CLA = 0x0,
                            INS = 0x22,
                            P1 = 0x41,
                            P2 = 0xb6,
                            Data = TLV
                        };
                        var response = isoReader.Transmit(apdu);
                        if (response.SW1 != 0x90 || response.SW2 != 0x00)
                        {
                            throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                        }
                    }
                }

                {
                    // VERIFY (page 100)
                    Array.Resize(ref pin, 8);
                    var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x20,
                        P1 = 0x00,
                        P2 = 0x81,
                        Data = pin
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }

                {
                    // PSO – PUT HASH (page 79)
                    var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x2a,
                        P1 = 0x90,
                        P2 = 0x81,
                        Data = sha256hash
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }

                {
                    // PSO – COMPUTE DIGITAL SIGNATURE (page 73)
                    var apdu = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x2a,
                        P1 = 0x9e,
                        P2 = 0x9a,
                        Le = 0x0
                    };
                    var response = isoReader.Transmit(apdu);
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

        }

        public bool selectApplication(SCardReader cardReader, IsoReader isoReader)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {isoReader.ReaderName}  BeginTransaction failed");
                }

                {
                    // sendSelectFID(DF_SIG);
                    var DF_SIG = new byte[] { 0xDF, 0x70 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0xa4,
                        P1 = 0x0,
                        P2 = 0x0c,
                        Data = DF_SIG,
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }

                {
                    // MANAGE SECURITY ENVIRONMENT (page 67)
                    var TLV = new byte[] {  0x84,  0x01,  0x88,  0x80,  0x01,  0x44 };
                    var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x22,
                        P1 = 0x41,
                        P2 = 0xb6,
                        Data = TLV
                    };
                    var response = isoReader.Transmit(apdu);
                    if (response.SW1 != 0x90 || response.SW2 != 0x00)
                    {
                        throw new Exception("APDU-Fehler: " + BitConverter.ToString(apdu.ToArray()));
                    }
                }
            }
            catch (Exception x)
            {
                return false;
            }
            finally
            {
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return true;
        }
    }
}
