using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    /// <summary>
    /// Acceptance tests against a physical Epson RT Server (fiskaltrust test device, till FISK0001).
    ///
    /// This class intentionally does NOT inherit ITSSCDTests: the base class uses the fixed till id
    /// "ACPT0001", which is not part of this shared device's till map, and running the inherited
    /// initial-operation tests would reprogram the map of a device shared with other teams.
    ///
    /// NOTE: these tests emit real fiscal documents on the test device, and the daily-closing test performs a
    /// till closure plus a server-level Z report (which transmits to the tax authority sandbox). Run manually.
    /// </summary>
    public class EpsonRTServerTests
    {
        private const string TillId = "FISK0001";
        private const string DeviceSerialNumber = "99SEA004010";
        private static readonly Guid _queueId = Guid.Parse("f81b4697-3060-4dd8-ab26-e30b0bbf1a23");

        private readonly EpsonRTServerConfiguration _config = new()
        {
            ServerUrl = "https://2.239.218.86:50191",
            Username = "epson",
            Password = "epson",
            DisableSSLValidation = true,
            SendReceiptsSync = true,
            IgnoreRTServerErrors = false
        };

        private static ReceiptResponse NewReceiptResponse => new()
        {
            ftCashBoxIdentification = TillId,
            ftQueueID = _queueId.ToString(),
            ftSignatures = Array.Empty<SignaturItem>()
        };

        private IITSSCD GetSUT()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var bootstrapper = new ScuBootstrapper
            {
                Id = _queueId,
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))!
            };
            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }

        private static ReceiptRequest Sale(decimal amount, string description = "ACCEPTANCE TEST") => new()
        {
            ftReceiptCase = 0x4954_2000_0000_0001,
            cbReceiptMoment = DateTime.Now,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = new[] { new ChargeItem { Amount = amount, Quantity = 1, Description = description, VATRate = 22m, ftChargeItemCase = 0x4954_2000_0000_0003 } },
            cbPayItems = new[] { new PayItem { Amount = amount, Quantity = 1, Description = "CONTANTE", ftPayItemCase = 0x4954_2000_0000_0000 } }
        };

        private static void AssertDocumentSignatures(ProcessResponse result)
        {
            result.ReceiptResponse.HasFailed().Should().BeFalse(result.ReceiptResponse.ftSignatures.FirstOrDefault()?.Data);
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber)).Subject.Data.Should().Be(DeviceSerialNumber);
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTServerShaMetadata));
        }

        private static SignaturItem[] BuildReferenceSignatures(ProcessResponse saleResult)
        {
            var zNumber = saleResult.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data;
            var docNumber = saleResult.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)!.Data;
            var docMoment = DateTime.Parse(saleResult.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)!.Data);
            return new[]
            {
                new SignaturItem { Caption = "<reference-z-number>", Data = zNumber, ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber },
                new SignaturItem { Caption = "<reference-doc-number>", Data = docNumber, ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber },
                new SignaturItem { Caption = "<reference-timestamp>", Data = docMoment.ToString("yyyy-MM-dd HH:mm:ss"), ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment }
            };
        }

        [Fact]
        public async Task GetRTInfoAsync_ShouldReturn_Serialnumber()
        {
            var result = await GetSUT().GetRTInfoAsync();
            result.SerialNumber.Should().Be(DeviceSerialNumber);
        }

        [Fact]
        public async Task ProcessPosReceipt_Sale_Cash()
        {
            var result = await GetSUT().ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = Sale(1.00m),
                ReceiptResponse = NewReceiptResponse
            });
            using var scope = new AssertionScope();
            AssertDocumentSignatures(result);
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType)).Subject.Data.Should().Be("POSRECEIPT");
        }

        [Fact]
        public async Task ProcessPosReceipt_Sale_Cash_WithDiscountAndChange()
        {
            var request = Sale(10.00m, "PRODOTTO");
            request.cbChargeItems = request.cbChargeItems.Concat(new[]
            {
                new ChargeItem { Amount = -2.00m, Quantity = 1, Description = "SCONTO", VATRate = 22m, ftChargeItemCase = 0x4954_2000_0000_0003 }
            }).ToArray();
            // Cash 10.00 for an 8.00 net total: 2.00 change (device-validated paidAmount semantics).
            var result = await GetSUT().ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = NewReceiptResponse
            });
            using var scope = new AssertionScope();
            AssertDocumentSignatures(result);
        }

        [Fact]
        public async Task ProcessPosReceipt_Refund_WithReferences()
        {
            var itsscd = GetSUT();
            var saleResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = Sale(2.00m, "DA RENDERE"),
                ReceiptResponse = NewReceiptResponse
            });
            AssertDocumentSignatures(saleResult);

            var refundRequest = Sale(-2.00m, "RESO");
            refundRequest.ftReceiptCase = 0x4954_2000_0100_0001; // refund flag
            refundRequest.cbPreviousReceiptReference = saleResult.ReceiptResponse.ftQueueID;
            var refundResponse = NewReceiptResponse;
            refundResponse.ftSignatures = BuildReferenceSignatures(saleResult);

            var refundResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = refundRequest,
                ReceiptResponse = refundResponse
            });

            using var scope = new AssertionScope();
            AssertDocumentSignatures(refundResult);
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType)).Subject.Data.Should().Be("REFUND");
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment));
        }

        [Fact]
        public async Task ProcessPosReceipt_Void_WithReferences()
        {
            var itsscd = GetSUT();
            var saleResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = Sale(3.00m, "DA ANNULLARE"),
                ReceiptResponse = NewReceiptResponse
            });
            AssertDocumentSignatures(saleResult);

            var voidRequest = Sale(-3.00m, "ANNULLO");
            voidRequest.ftReceiptCase = 0x4954_2000_0004_0001; // void flag
            voidRequest.cbPreviousReceiptReference = saleResult.ReceiptResponse.ftQueueID;
            var voidResponse = NewReceiptResponse;
            voidResponse.ftSignatures = BuildReferenceSignatures(saleResult);

            var voidResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = voidRequest,
                ReceiptResponse = voidResponse
            });

            using var scope = new AssertionScope();
            AssertDocumentSignatures(voidResult);
            voidResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType)).Subject.Data.Should().Be("VOID");
            voidResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            voidResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
        }

        [Fact]
        public async Task ProcessPosReceipt_Lottery_CardOverpayment_ShouldBeAcceptedCleanly()
        {
            var request = new ReceiptRequest
            {
                ftReceiptCase = 0x4954_2000_0000_0001,
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = new[] { new ChargeItem { Amount = 3.30m, Quantity = 1, Description = "COFFEE", VATRate = 22m, ftChargeItemCase = 0x4954_2000_0000_0003 } },
                cbPayItems = new[] { new PayItem { Amount = 3.50m, Quantity = 1, Description = "CARTA", ftPayItemCase = 0x4954_2000_0000_0004 } },
                ftReceiptCaseData = "{\"servizi_lotteriadegliscontrini_gov_it\":{\"codicelotteria\":\"ABCD1234\"}}"
            };
            var result = await GetSUT().ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = request, ReceiptResponse = NewReceiptResponse });
            using var scope = new AssertionScope();
            AssertDocumentSignatures(result);
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTLotteryID)).Subject.Data.Should().Be("ABCD1234");
            // #636: card tender clamped to the total -> device accepts with no payment warning (-39).
            result.ReceiptResponse.ftSignatures.Should().NotContain(x => x.Caption == "rt-server-receipt-warning");
        }

        [Fact]
        public async Task ProcessZeroReceipt_ShouldReport_DeviceState()
        {
            var request = new ReceiptRequest
            {
                ftReceiptCase = 0x4954_2000_0000_2000,
                cbReceiptMoment = DateTime.Now,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            var result = await GetSUT().ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = NewReceiptResponse
            });

            using var scope = new AssertionScope();
            result.ReceiptResponse.HasFailed().Should().BeFalse();
            var stateData = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.ReceiptResponse.ftStateData);
            stateData.Should().ContainKey("ServerInfo");
            stateData.Should().ContainKey("DocumentsInCache");
            stateData.Should().ContainKey("SigningDeviceAvailable");
        }

        [Fact]
        public async Task ProcessPosReceipt_DailyClosing()
        {
            // NOTE: closes the till session and requests a server-level Z report (transmits to the tax
            // authority sandbox on the test device).
            var itsscd = GetSUT();
            var saleResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = Sale(1.00m, "PRE-CLOSING"),
                ReceiptResponse = NewReceiptResponse
            });
            AssertDocumentSignatures(saleResult);
            var zNumberBefore = long.Parse(saleResult.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data);

            var closingRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x4954_2000_0000_2011,
                cbReceiptMoment = DateTime.Now,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            var closingResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = closingRequest,
                ReceiptResponse = NewReceiptResponse
            });

            using var scope = new AssertionScope();
            closingResult.ReceiptResponse.HasFailed().Should().BeFalse(closingResult.ReceiptResponse.ftSignatures.FirstOrDefault()?.Data);
            var closedZ = long.Parse(closingResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber)).Subject.Data);
            closedZ.Should().Be(zNumberBefore);

            // The next sale must run in the NEW session (Z + 1) via the fresh token.
            var nextSale = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = Sale(1.00m, "POST-CLOSING"),
                ReceiptResponse = NewReceiptResponse
            });
            AssertDocumentSignatures(nextSale);
            long.Parse(nextSale.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data).Should().Be(zNumberBefore + 1);
        }
    }
}
