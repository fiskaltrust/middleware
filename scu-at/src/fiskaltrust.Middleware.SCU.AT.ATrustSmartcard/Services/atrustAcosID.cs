using PCSC;
using PCSC.Iso7816;
using System.Security.Cryptography;
using System.Text;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    internal class atrustAcosID : ICardService
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return true;
        }

        public byte[] readCertificates(SCardReader cardReader, IsoReader reader, bool onlyFirst = false, bool verify = false)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }
        }

        public byte[] readCIN(SCardReader cardReader, IsoReader reader)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return null;
        }

        public byte[] sign(SCardReader cardReader, IsoReader reader, byte[] pin, byte[] data, bool selectCard = true)
        {
            using var sha256 = SHA256.Create();
            var sha256hash = sha256.ComputeHash(data);

            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                if (selectCard)
                {
                    {
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
                }

                {
                    var pinBytes = GetFormat2PIN(Encoding.ASCII.GetString(pin));
                    var apdu = new CommandApdu(IsoCase.Case3Short, reader.ActiveProtocol)
                    {
                        CLA = 0x0,
                        INS = 0x20,
                        P1 = 0x00,
                        P2 = 0x8A,
                        Data = pinBytes
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

        }

        public bool selectApplication(SCardReader cardReader, IsoReader reader)
        {
            try
            {
                if (cardReader.BeginTransaction() != SCardError.Success)
                {
                    throw new Exception($"Reader {reader.ReaderName}  BeginTransaction failed");
                }

                {
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
                cardReader.EndTransaction(SCardReaderDisposition.Leave);
            }

            return true;
        }


        private static byte[] GetFormat2PIN(string pin)
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
