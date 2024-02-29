using Microsoft.Extensions.Logging;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services
{
    public class CardServiceFactory
    {
        private readonly ILogger<CardServiceFactory> _logger;

        public CardServiceFactory(ILogger<CardServiceFactory> logger) => _logger = logger;

        public CardService CreateCardService(SCardReader cardReader, IsoReader isoReader)
        {
            var cardState = cardReader.CurrentContext.GetReaderStatus(isoReader.ReaderName);
            if (cardState.Atr == null)
            {
                throw new Exception($"The card in .the specified reader with the name '{isoReader.ReaderName}' was not found.");
            }

            var atrHex = BitConverter.ToString(cardState.Atr);
            if (atrHex.StartsWith("3B-BF-11-00-81-31-FE-45-45-50-41", StringComparison.OrdinalIgnoreCase) || atrHex.StartsWith("3B-BF-11-00-81-31-FE-45-4D-43-41", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Detected an A-Trust ACOS card");
                return new atrustACOS(cardReader,isoReader);


            }
            else if (atrHex.StartsWith("3B-DF-18-00-81-31-FE-58-80-31-B0-52-02-04-64-05-C9-03-AC-73-B7-B1-D4-22", StringComparison.OrdinalIgnoreCase) || atrHex.StartsWith("3B-DF-18-00-81-31-FE-58-80-31-90", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Detected an A-Trust CardOS53 card");
                return new atrustCardOS53(cardReader, isoReader);

            }
            else if (new string[]
            {
                    "3BDF96FF910131FE4680319052410264050200AC73D622C017",
                    "3BDF18FF910131FE4680319052410264050200AC73D622C099",
                    "3BDF97008131FE4680319052410364050201AC73D622C0F8",
                    "3BDF97008131FE468031905241026405C903AC73D622C030",
                    "3BDF96FF918131FE468031905241026405C903AC73D622C05F",
                    "3BDF18FF918131FE468031905241026405C903AC73D622C0D1",
            }.Any(id => atrHex.Replace("-", "").StartsWith(id, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Detected an A-Trust AcosID card");
                
                return new atrustAcosID(cardReader, isoReader);
            }
            else
            {
                throw new Exception($"The ATR value '{atrHex}' did not match any supported card.");
            }
        }
    }
}
