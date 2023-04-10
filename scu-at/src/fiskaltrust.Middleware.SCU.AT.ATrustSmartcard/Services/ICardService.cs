using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    public interface ICardService
    {
        byte[] GetPin();

        bool checkApplication(SCardReader cardReader, IsoReader isoReader);

        bool selectApplication(SCardReader cardReader, IsoReader isoReader);

        byte[] readCIN(SCardReader cardReader, IsoReader isoReader);

        byte[] readCertificates(SCardReader cardReader, IsoReader isoReader, bool onlyFirst = false, bool verify = false);

        byte[] sign(SCardReader cardReader, IsoReader isoReader, byte[] pin, byte[] data, bool selectCard = true);
    }
}
