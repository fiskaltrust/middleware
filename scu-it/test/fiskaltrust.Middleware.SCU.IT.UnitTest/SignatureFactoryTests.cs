using System;
using System.Linq;
using FluentAssertions;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Xunit;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class SignatureFactoryTests
    {
        [Fact]
        public void CreateDocumentoCommercialeSignatures_NullSerialNumber_EmitsEmptyDataInsteadOfNull()
        {
            var data = new POSReceiptSignatureData
            {
                RTSerialNumber = null!,
                RTZNumber = 5,
                RTDocNumber = 3,
                RTDocMoment = new DateTime(2026, 8, 5, 10, 30, 0),
                RTDocType = "Documento Gestionale",
                RTReferenceZNumber = 5,
                RTReferenceDocNumber = 2,
                RTReferenceDocMoment = new DateTime(2026, 8, 4, 15, 0, 0)
            };

            var signatures = SignatureFactory.CreateDocumentoCommercialeSignatures(data);

            var serialNumberSignature = signatures.Single(x => x.Caption == "<rt-serialnumber>");
            serialNumberSignature.Data.Should().NotBeNull();
            serialNumberSignature.Data.Should().Be("");
        }

        [Fact]
        public void CreateDocumentoCommercialeSignatures_SerialNumberSet_EmitsSerialNumber()
        {
            var data = new POSReceiptSignatureData
            {
                RTSerialNumber = "99IEB012345",
                RTZNumber = 5,
                RTDocNumber = 3,
                RTDocMoment = new DateTime(2026, 8, 5, 10, 30, 0),
                RTDocType = "POSRECEIPT"
            };

            var signatures = SignatureFactory.CreateDocumentoCommercialeSignatures(data);

            var serialNumberSignature = signatures.Single(x => x.Caption == "<rt-serialnumber>");
            serialNumberSignature.Data.Should().Be("99IEB012345");
        }
    }
}
