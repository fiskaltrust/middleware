using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class SignatureFactoryDETests

    {
        [Fact]
        public void CreateSignaturesForStartTransaction_ShouldReturn_SignatureItem_WithType_StartTransaction_And_SignatureBase64_InData()
        {
            var startTransactionResponse = new StartTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "ddffc471-b101-4b89-8761-dd3c7f779f7c",
                TimeStamp = new DateTime(2020, 1, 24, 6, 7, 10),
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgsfhKUBbX8WubnpjmQnNW4dE+QsxG3RJxAisp5oxXPb4n6e/+NHf5OQKaeGrQH8+EOG0MDpzhE5oWaFxi2ECKA==")),
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "c+3+k0v3bycwayxb8oyB01WgMNVmZEPMH9ink2XDY3z57g/IfX2kfH9xxDYcGbc2wEF9UbCSG1DLjtJAlpENvQ==",
                    SignatureCounter = 4213
                },
                TransactionNumber = 2422
            };


            var sut = new SignatureFactoryDE(new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() });

            var signature = sut.GetSignaturForStartTransaction(startTransactionResponse);

            signature.ftSignatureType.Should().Be(0x4445_0000_0000_0010);
            signature.ftSignatureFormat.Should().Be(0x0D | 0x10000);
            signature.Caption.Should().Be("start-transaction-signature");
            signature.Data.Should().Be(startTransactionResponse.SignatureData.SignatureBase64);
        }

        [Fact]
        public void CreateSignaturesForFinishTransaction_ShouldReturn_Two_SignatureItems_ForFinishTransactionResult_AndFinishTransactionPayload()
        {
            var finishResultResponse = new FinishTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "ddffc471-b101-4b89-8761-dd3c7f779f7c",
                TimeStamp = new DateTime(2020, 1, 24, 6, 22, 5),
                TseTimeStampFormat = "utcTime",
                StartTransactionTimeStamp = new DateTime(2020, 1, 24, 6, 22, 4),
                ProcessDataBase64 = "QmVsZWdeNzUuMzNfNy45OV8wLjAwXzAuMDBfMC4wMF4xMC4wMDpCYXJfNS4wMDpCYXI6Q0hGXzUuMDA6QmFyOlVTRF82NC4zMDpVbmJhcg==",
                ProcessType = "Kassenbeleg-V1",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgsfhKUBbX8WubnpjmQnNW4dE+QsxG3RJxAisp5oxXPb4n6e/+NHf5OQKaeGrQH8+EOG0MDpzhE5oWaFxi2ECKA==")),
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "778HA4jJfYVpXFtOI8GxOI7PlZytnV8fQ8v36/il7oJgui2J3Zmhr42n5PJEA4doEOi7L1IkuDKDNqwaSPI4Vg==",
                    SignatureCounter = 4218
                },
                TransactionNumber = 2425
            };

            var sut = new SignatureFactoryDE(new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() });

            var signatures = sut.GetSignaturesForFinishTransaction(finishResultResponse);
            signatures.Should().HaveCount(3);

            signatures[0].ftSignatureType.Should().Be(0x4445_0000_0000_0011);
            signatures[0].ftSignatureFormat.Should().Be(0x0D | 0x10000);
            signatures[0].Caption.Should().Be("finish-transaction-payload");
            signatures[0].Data.Should().Be(finishResultResponse.ProcessDataBase64);

            signatures[1].ftSignatureType.Should().Be(0x4445_0000_0000_0012);
            signatures[1].ftSignatureFormat.Should().Be(0x0D | 0x10000);
            signatures[1].Caption.Should().Be("finish-transaction-signature");
            signatures[1].Data.Should().Be(finishResultResponse.SignatureData.SignatureBase64);

            signatures[2].Caption.Should().Be("<processType>");
            signatures[2].Data.Should().Be("Kassenbeleg-V1");
        }

        [Fact]
        public void CreateSignaturesFor_Finish_Pos_Transaction_ShouldReturnFullBlownSignaturesWithContentForReceipt()
        {
            var certificationIdentification = "BSI-TK-0000-0000";
            var startTransactionResponse = new StartTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "c+3+k0v3bycwayxb8oyB01WgMNVmZEPMH9ink2XDY3z57g/IfX2kfH9xxDYcGbc2wEF9UbCSG1DLjtJAlpENvQ==",
                    SignatureCounter = 111
                },
                TransactionNumber = 18
            };

            var finishResultResponse = new FinishTransactionResponse
            {
                TseSerialNumberOctet = "dc19faf6e7ab21690772be6f0ffc586eccdfeb299c17985c06b59029409a7613",
                ClientId = "955002-00",
                TimeStamp = new DateTime(2019, 7, 10, 18, 41, 4),
                TseTimeStampFormat = "unixTime",
                StartTransactionTimeStamp = new DateTime(2019, 7, 10, 18, 41, 2),
                ProcessDataBase64 = "QmVsZWdeMC4wMF8yLjU1XzAuMDBfMC4wMF8wLjAwXjIuNTU6QmFy",
                ProcessType = "Kassenbeleg-V1",
                SignatureData = new TseSignatureData
                {
                    PublicKeyBase64 = "BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=",
                    SignatureAlgorithm = "ecdsa-plain-SHA256",
                    SignatureBase64 = "MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==",
                    SignatureCounter = 112
                },
                TransactionNumber = 18
            };

            var sut = new SignatureFactoryDE(new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() });

            var signatures = sut.GetSignaturesForPosReceiptTransaction(startTransactionResponse.SignatureData.SignatureBase64, finishResultResponse, certificationIdentification);
            signatures.Should().HaveCount(17);

            signatures[0].ftSignatureType.Should().Be(0x4445_0000_0000_0001);
            signatures[0].ftSignatureFormat.Should().Be(0x03);
            signatures[0].Caption.Should().Be("www.fiskaltrust.de");
            signatures[0].Data.Should().Be("V0;955002-00;Kassenbeleg-V1;Beleg^0.00_2.55_0.00_0.00_0.00^2.55:Bar;18;112;2019-07-10T18:41:02.000Z;2019-07-10T18:41:04.000Z;ecdsa-plain-SHA256;unixTime;MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==;BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=");

            signatures[1].ftSignatureType.Should().Be(0x4445_0000_0000_0010);
            signatures[1].ftSignatureFormat.Should().Be(0x0D | 0x10000);
            signatures[1].Caption.Should().Be("start-transaction-signature");
            signatures[1].Data.Should().Be(startTransactionResponse.SignatureData.SignatureBase64);

            signatures[2].ftSignatureType.Should().Be(0x4445_0000_0000_0011);
            signatures[2].ftSignatureFormat.Should().Be(0x0D | 0x10000);
            signatures[2].Caption.Should().Be("finish-transaction-payload");
            signatures[2].Data.Should().Be(finishResultResponse.ProcessDataBase64);

            signatures[3].ftSignatureType.Should().Be(0x4445_0000_0000_0012);
            signatures[3].ftSignatureFormat.Should().Be(0x0D | 0x10000);
            signatures[3].Caption.Should().Be("finish-transaction-signature");
            signatures[3].Data.Should().Be(finishResultResponse.SignatureData.SignatureBase64);

            signatures[4].ftSignatureType.Should().Be(0x4445_0000_0000_0022);
            signatures[4].ftSignatureFormat.Should().Be(0x01);
            signatures[4].Caption.Should().Be("<certification-id>");
            signatures[4].Data.Should().Be(certificationIdentification);

            signatures[5].ftSignatureType.Should().Be(0x4445_0000_0000_0013);
            signatures[5].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[5].Caption.Should().Be("<qr-code-version>");
            signatures[5].Data.Should().Be("V0");

            signatures[6].ftSignatureType.Should().Be(0x4445_0000_0000_0014);
            signatures[6].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[6].Caption.Should().Be("<kassen-seriennummer>");
            signatures[6].Data.Should().Be("955002-00");

            signatures[7].ftSignatureType.Should().Be(0x4445_0000_0000_0015);
            signatures[7].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[7].Caption.Should().Be("<processType>");
            signatures[7].Data.Should().Be("Kassenbeleg-V1");

            signatures[8].ftSignatureType.Should().Be(0x4445_0000_0000_0016);
            signatures[8].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[8].Caption.Should().Be("<processData>");
            signatures[8].Data.Should().Be("Beleg^0.00_2.55_0.00_0.00_0.00^2.55:Bar");

            signatures[9].ftSignatureType.Should().Be(0x4445_0000_0000_0017);
            signatures[9].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[9].Caption.Should().Be("<transaktions-nummer>");
            signatures[9].Data.Should().Be("18");

            signatures[10].ftSignatureType.Should().Be(0x4445_0000_0000_0018);
            signatures[10].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[10].Caption.Should().Be("<signatur-zaehler>");
            signatures[10].Data.Should().Be("112");

            signatures[11].ftSignatureType.Should().Be(0x4445_0000_0000_0019);
            signatures[11].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[11].Caption.Should().Be("<start-zeit>");
            signatures[11].Data.Should().Be("2019-07-10T18:41:02.000Z");

            signatures[12].ftSignatureType.Should().Be(0x4445_0000_0000_001A);
            signatures[12].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[12].Caption.Should().Be("<log-time>");
            signatures[12].Data.Should().Be("2019-07-10T18:41:04.000Z");

            signatures[13].ftSignatureType.Should().Be(0x4445_0000_0000_001B);
            signatures[13].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[13].Caption.Should().Be("<sig-alg>");
            signatures[13].Data.Should().Be("ecdsa-plain-SHA256");

            signatures[14].ftSignatureType.Should().Be(0x4445_0000_0000_001C);
            signatures[14].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[14].Caption.Should().Be("<log-time-format>");
            signatures[14].Data.Should().Be("unixTime");

            signatures[15].ftSignatureType.Should().Be(0x4445_0000_0000_001D);
            signatures[15].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[15].Caption.Should().Be("<signatur>");
            signatures[15].Data.Should().Be("MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==");

            signatures[16].ftSignatureType.Should().Be(0x4445_0000_0000_001E);
            signatures[16].ftSignatureFormat.Should().Be(0x01 | 0x10000);
            signatures[16].Caption.Should().Be("<public-key>");
            signatures[16].Data.Should().Be("BHhWOeisRpPBTGQ1W4VUH95TXx2GARf8e2NYZXJoInjtGqnxJ8sZ3CQpYgjI+LYEmW5A37sLWHsyU7nSJUBemyU=");
        }

        [Fact]
        public void GetSignaturesForTransaction_Should_FlagReceiptWhenNoConfigurationIsSet()
        {
            var mandatoryCaptions = new[] { "<certification-id>", "www.fiskaltrust.de", "<vorgangsbeginn>" };

            var sut = new SignatureFactoryDE(new MiddlewareConfiguration { Configuration = new Dictionary<string, object>() });
            var result = sut.GetSignaturesForTransaction("DoesntMatter", new FinishTransactionResponse { SignatureData = new TseSignatureData(), ProcessDataBase64 = "QQ==" }, "DoesntMatter");

            var mandatoryItems = result.Where(x => mandatoryCaptions.Contains(x.Caption));
            var optionalItems = result.Where(x => !mandatoryCaptions.Contains(x.Caption));

            mandatoryItems.Should().OnlyContain(x => ((long) x.ftSignatureFormat & 0x10000) == 0x00000);
            optionalItems.Should().OnlyContain(x => ((long) x.ftSignatureFormat & 0x10000) == 0x10000);
        }

        [Fact]
        public void GetSignaturesForTransaction_ShouldNot_FlagReceiptWhenConfigurationIsSet()
        {
            var mandatoryCaptions = new[] { "<certification-id>", "www.fiskaltrust.de", "<vorgangsbeginn>" };

            var sut = new SignatureFactoryDE(new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>
                {
                    { "FlagOptionalSignatures", false }
                }
            });
            var result = sut.GetSignaturesForTransaction("DoesntMatter", new FinishTransactionResponse { SignatureData = new TseSignatureData(), ProcessDataBase64 = "QQ==" }, "DoesntMatter");

            var mandatoryItems = result.Where(x => mandatoryCaptions.Contains(x.Caption));
            var optionalItems = result.Where(x => !mandatoryCaptions.Contains(x.Caption));

            mandatoryItems.Should().OnlyContain(x => ((long) x.ftSignatureFormat & 0x10000) == 0x00000);
            optionalItems.Should().OnlyContain(x => ((long) x.ftSignatureFormat & 0x10000) == 0x00000);
        }
    }
}
