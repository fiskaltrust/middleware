using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class ITSSCDTests
    {
        private static readonly Guid _queueId = Guid.NewGuid();
        private static readonly Uri _serverUri = new Uri("http://192.168.0.34/");
        private readonly EpsonRTPrinterSCUConfiguration _config = new EpsonRTPrinterSCUConfiguration
        {
            DeviceUrl = _serverUri.ToString()
        };
        private static readonly ReceiptResponse _receiptResponse = new ReceiptResponse
        {
            ftCashBoxIdentification = "00010001",
            ftQueueID = _queueId.ToString()
        };

        public static IEnumerable<object[]> rtNoHandleReceipts()
        {
            yield return new object[] { ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003 };
            yield return new object[] { ITReceiptCases.ECommerce0x0004 };
            yield return new object[] { ITReceiptCases.InvoiceUnknown0x1000 };
            yield return new object[] { ITReceiptCases.InvoiceB2C0x1001 };
            yield return new object[] { ITReceiptCases.InvoiceB2B0x1002 };
            yield return new object[] { ITReceiptCases.InvoiceB2G0x1003 };
            yield return new object[] { ITReceiptCases.OneReceipt0x2001 };
            yield return new object[] { ITReceiptCases.ShiftClosing0x2010 };
            yield return new object[] { ITReceiptCases.MonthlyClosing0x2012 };
            yield return new object[] { ITReceiptCases.YearlyClosing0x2013 };
            yield return new object[] { ITReceiptCases.ProtocolUnspecified0x3000 };
            yield return new object[] { ITReceiptCases.ProtocolTechnicalEvent0x3001 };
            yield return new object[] { ITReceiptCases.ProtocolAccountingEvent0x3002 };
            yield return new object[] { ITReceiptCases.InternalUsageMaterialConsumption0x3003 };
            yield return new object[] { ITReceiptCases.InitSCUSwitch0x4011 };
            yield return new object[] { ITReceiptCases.FinishSCUSwitch0x4012 };
        }

        public static IEnumerable<object[]> rtHandledReceipts()
        {
            yield return new object[] { ITReceiptCases.UnknownReceipt0x0000 };
            yield return new object[] { ITReceiptCases.PointOfSaleReceipt0x0001 };
            yield return new object[] { ITReceiptCases.PaymentTransfer0x0002 };
            yield return new object[] { ITReceiptCases.Protocol0x0005 };
        }

        private IITSSCD GetSUT()
        {


            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
            };
            sut.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }

        [Fact]
        public async Task GetRTInfoAsync_ShouldReturn_Serialnumber()
        {
            var itsscd = GetSUT();

            var result = await itsscd.GetRTInfoAsync();
            result.SerialNumber.Should().Be("96SRT001239");
        }

        [Theory]
        [MemberData(nameof(rtNoHandleReceipts))]
        public async Task ProcessAsync_Should_Do_Nothing(ITReceiptCases receiptCase)
        {
            var initOperationReceipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "{{Guid.NewGuid()}}",
    "cbReceiptMoment": "{{DateTime.UtcNow.ToString("o")}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000000000 | (long) receiptCase}},
    "ftReceiptCaseData": "",
    "cbUser": "Admin"
}
""";
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(initOperationReceipt);

            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = new ReceiptResponse
                {
                    ftQueueID = Guid.NewGuid().ToString()
                }
            });
            result.ReceiptResponse.ftSignatures.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessPosReceipt_InitialOperation_0x4954_2000_0000_4001()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetInitialOperation(),
                ReceiptResponse = _receiptResponse
            });
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<customrtserver-cashuuid>");
        }

        [Fact]
        public async Task ProcessPosReceipt_OutOfOperation_0x4954_2000_0000_4002()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetOutOOperation(),
                ReceiptResponse = _receiptResponse
            });
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "<customrtserver-cashuuid>");
        }

        [Fact]
        public async Task ProcessPosReceipt_Daily_Closing0x4954_2000_0000_2011()
        {

            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetDailyClosing(),
                ReceiptResponse = _receiptResponse
            });
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == 0x4954000000000011);
        }

        [Fact]
        public async Task ProcessPosReceipt_ZeroReceipt0x4954_2000_0000_2000()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetZeroReceipt(),
                ReceiptResponse = _receiptResponse
            });
            var dictioanry = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.ReceiptResponse.ftStateData);
            dictioanry.Should().ContainKey("DeviceMemStatus");
            dictioanry.Should().ContainKey("DeviceDailyStatus");
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Cash()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Cash(),
                ReceiptResponse = _receiptResponse
            });

            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.CustomRTServerShaMetadata));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Cash_Refund()
        {
            var response = _receiptResponse;
            var itsscd = GetSUT();
            var request = ReceiptExamples.GetTakeAway_Delivery_Cash();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = _receiptResponse
            });


            var zNumber = result.ReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)).Data;
            var rtdocNumber = result.ReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentNumber)).Data;
            var signatures = new List<SignaturItem>();
            signatures.AddRange(response.ftSignatures);
            signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber,
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = rtdocNumber,
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = request.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTDocumentMoment
                        },
                    });
            response.ftSignatures = signatures.ToArray();

            var refundResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Refund(),
                ReceiptResponse = response
            });

        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Card()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Card(),
                ReceiptResponse = _receiptResponse
            });

            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.CustomRTServerShaMetadata));
        }
    }
}