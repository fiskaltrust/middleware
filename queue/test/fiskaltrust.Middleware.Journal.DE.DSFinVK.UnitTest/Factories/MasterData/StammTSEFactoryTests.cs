using System;
using AutoFixture;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    [Collection("Sequential")]
    public class StammTSEFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var tseCertBase64 = string.Join(string.Empty, CustomizedFixture.GetFixture().CreateMany<char>(1500));
            var dailyClosingResponse = GetDailyClosingResponse();

            var sut = new StammTSEFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification, TSECertificateBase64 = tseCertBase64 });
            var result = sut.Create(dailyClosingResponse, zNumber, closingMoment, null);

            result.Z_KASSE_ID.Should().Be(cashboxIdentification);
            result.Z_ERSTELLUNG.Should().Be(closingMoment);
            result.Z_NR.Should().Be(zNumber);
            result.TSE_ID.Should().Be(1);
            result.TSE_SERIAL.Should().Be("DF00AC9741FAA7CC03FA80E9A84A969741CADF958F1DBC43D6AFF12F9835FC86".ToLower());
            result.TSE_SIG_ALGO.Should().Be("ecdsa-plain-SHA256");
            result.TSE_ZEITFORMAT.Should().Be("utcTime");
            result.TSE_PD_ENCODING.Should().Be("UTF-8");
            result.TSE_PUBLIC_KEY.Should().Be("BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA==");
            result.TSE_ZERTIFIKAT_I.Should().Be(tseCertBase64.Substring(0, 1000));
            result.TSE_ZERTIFIKAT_II.Should().Be(tseCertBase64.Substring(1000));
            result.TSE_ZERTIFIKAT_III.Should().Be(string.Empty);
            result.TSE_ZERTIFIKAT_IV.Should().Be(string.Empty);
            result.TSE_ZERTIFIKAT_V.Should().Be(string.Empty);
        }

        private ReceiptResponse GetDailyClosingResponse() => new ReceiptResponse
        {
            ftSignatures = new[]
            {
                new SignaturItem
                {
                    ftSignatureType = (long) SignatureTypesDE.SignaturAlgorithm,
                    Data = "ecdsa-plain-SHA256"
                },
                new SignaturItem
                {
                    ftSignatureType = (long) SignatureTypesDE.LogTimeFormat,
                    Data = "utcTime"
                },
                new SignaturItem
                {
                    ftSignatureType = (long) SignatureTypesDE.PublicKey,
                    Data = "BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA=="
                }
            }
        };
    }
}
