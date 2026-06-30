using System;
using System.Linq;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    public class SignaturItemFactoryTests
    {
        private static ReceiptResponse BuildResponse(string lotteryCode)
        {
            var data = new POSReceiptSignatureData
            {
                RTSerialNumber = "96SRT001239",
                RTZNumber = 3,
                RTDocNumber = 539,
                RTDocMoment = new DateTime(2026, 6, 29, 10, 9, 0),
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = lotteryCode
            };
            return new ReceiptResponse
            {
                ftCashBoxIdentification = "00040005",
                ftSignatures = POSReceiptSignatureData.CreateDocumentoCommercialeSignatures(data).ToArray()
            };
        }

        [Fact]
        public void CreatePOSReceiptFormatSignatures_WithLotteryCode_IncludesCodiceLotteriaInFooter()
        {
            var signatures = SignaturItemFactory.CreatePOSReceiptFormatSignatures(BuildResponse("DT1MV66K"));

            var footer = signatures.Single(x => x.Caption == "[www.fiskaltrust.it]");
            footer.Data.Should().Contain("Codice Lotteria: DT1MV66K");
        }

        [Fact]
        public void CreatePOSReceiptFormatSignatures_WithoutLotteryCode_OmitsCodiceLotteria()
        {
            var signatures = SignaturItemFactory.CreatePOSReceiptFormatSignatures(BuildResponse(null));

            var footer = signatures.Single(x => x.Caption == "[www.fiskaltrust.it]");
            footer.Data.Should().NotContain("Codice Lotteria");
        }
    }
}
