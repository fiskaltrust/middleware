using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public abstract class ITSSCDTests
    {
        private static readonly Guid _queueId = Guid.Parse("da8147d6-a0c2-42c6-8091-4ea7adbe4afc");

        protected static ReceiptResponse _receiptResponse => new ReceiptResponse
        {
            ftCashBoxIdentification = "ACPT0001",
            ftQueueID = _queueId.ToString()
        };

        protected abstract IMiddlewareBootstrapper GetMiddlewareBootstrapper(Guid queueId);

        protected abstract string SerialNumber { get; }

        protected IITSSCD GetSUT()
        {
            // Disable SSL checks for the test server
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate
            { return true; };
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = GetMiddlewareBootstrapper(_queueId);
            sut.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }

        protected IITSSCD GetSUT(IMiddlewareBootstrapper sut)
        {
            // Disable SSL checks for the test server
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate
            { return true; };
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            sut.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }

        [Fact]
        public async Task GetRTInfoAsync_ShouldReturn_Serialnumber()
        {
            var itsscd = GetSUT();

            var result = await itsscd.GetRTInfoAsync();
            result.SerialNumber.Should().Be(SerialNumber);
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
            result.ReceiptResponse.ftSignatures.Should().BeEmpty();
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
            result.ReceiptResponse.ftSignatures.Should().BeEmpty();
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
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
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


            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
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


            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)?.Data;
            var rtdocNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)?.Data;
            var rtDocumentMoment = DateTime.Parse(result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data ?? DateTime.MinValue.ToString());

            var signatures = new List<SignaturItem>();
            signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber?.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = rtdocNumber?.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = rtDocumentMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                        },
                    });
            response.ftSignatures = signatures.ToArray();

            var refundResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Refund(),
                ReceiptResponse = response
            });
            using var scope = new AssertionScope();
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Cash_Refund_WithoutReferencess()
        {
            var response = _receiptResponse;
            var itsscd = GetSUT();

            var refundResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Refund(),
                ReceiptResponse = response
            });
            using var scope = new AssertionScope();
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
            refundResult.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment));
        }


        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Cash_Void()
        {
            var response = _receiptResponse;
            var itsscd = GetSUT();
            var request = ReceiptExamples.GetTakeAway_Delivery_Cash();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = _receiptResponse
            });


            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)?.Data;
            var rtdocNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)?.Data;
            var rtDocumentMoment = DateTime.Parse(result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data ?? DateTime.MinValue.ToString());
            var signatures = new List<SignaturItem>();
            signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber?.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = rtdocNumber?.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = rtDocumentMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                        },
                    });
            response.ftSignatures = signatures.ToArray();

            var refundResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Void(),
                ReceiptResponse = response
            });
            using var scope = new AssertionScope();
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Cash_Void_WithoutReferences()
        {
            var response = _receiptResponse;
            var itsscd = GetSUT();
            var refundResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Void(),
                ReceiptResponse = response
            });
            using var scope = new AssertionScope();
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
            refundResult.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
            refundResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment));
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
            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Card_WithCustomerIVA()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Card_WithCustomerIva(),
                ReceiptResponse = _receiptResponse
            });

            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTCustomerID));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Card_WithInvalidCustomerIVA()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Card_WithInvalidCustomerIva(),
                ReceiptResponse = _receiptResponse
            });

            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Card_WithInvalidCustomerIVA_ShouldReturnError()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetTakeAway_Delivery_Card_WithInvalidCustomerIva(),
                ReceiptResponse = _receiptResponse
            });
            result.ReceiptResponse.HasFailed().Should().BeTrue();
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_CashAndVoucher()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.FoodBeverage_CashAndVoucher(),
                ReceiptResponse = _receiptResponse
            });

            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
        }

        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_CashReceiptWithTip()
        {
            var itsscd = GetSUT();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetCashReceiptWithTip(),
                ReceiptResponse = _receiptResponse
            });

            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
        }

        [Fact]
        public async Task ReprintReceipt()
        {
            var response = _receiptResponse;
            var itsscd = GetSUT();

            var request = ReceiptExamples.GetTakeAway_Delivery_Cash();
            var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = response
            });


            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)?.Data;
            var rtdocNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)?.Data;
            var rtDocumentMoment = DateTime.Parse(result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data ?? DateTime.MinValue.ToString());
            var signatures = new List<SignaturItem>();
            signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber?.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = rtdocNumber?.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = rtDocumentMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                        },
                    });
            response.ftSignatures = signatures.ToArray();
            result = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetReprintReceipt(),
                ReceiptResponse = response
            });

            using var scope = new AssertionScope();
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber));
            result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment));
        }
    }
}

// TODO TEsts
// - Add test that sends wrong CUstomer IVA
// - Add test that sends customer data without iva