using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public class CustomRTServerTests : ITSSCDTests
    {
        private static readonly Guid _accountid = Guid.Parse("4b95ea47-dbf7-4ba6-bcab-ae46030bc0e9");

        private static readonly Uri _serverUri = new Uri("https://f51f-88-116-45-202.ngrok-free.app/");
        private readonly CustomRTServerConfiguration _config = new CustomRTServerConfiguration
        {
            ServerUrl = _serverUri.ToString(),
            Username = "0001ab05",
            Password = "admin",
            AccountMasterData = JsonConvert.SerializeObject(new AccountMasterData
            {
                AccountId = _accountid,
                VatId = "MTLFNC75A16E783N"
            }),
            SendReceiptsSync = false
        };

        protected override string SerialNumber => "96SRT001239";

        protected override IMiddlewareBootstrapper GetMiddlewareBootstrapper(Guid queueId) => new ScuBootstrapper
        {
            Id = queueId,
            Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
        };


        [Fact]
        public async Task ProcessPosReceipt_0x4954_2000_0000_0001_TakeAway_Delivery_Cash_MultipleResults()
        {
            var itsscd = GetSUT();
            using var scope = new AssertionScope();

            var lastZNumber = 0L;
            var lastReceiptNumber = 0L;

            var dailyClosingResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetDailyClosing(),
                ReceiptResponse = _receiptResponse
            });
            lastZNumber = long.Parse(dailyClosingResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber)).Subject.Data);

            for (var i = 0; i < 100; i++)
            {
                var request = ReceiptExamples.GetTakeAway_Delivery_Cash();
                var result = await itsscd.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = _receiptResponse
                });
                result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTSerialNumber)).Subject.Data.Should().Be(SerialNumber);

                var zNumber= long.Parse(result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber)).Subject.Data);
                zNumber.Should().Be(lastZNumber + 1);
                var docNumber = long.Parse(result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber)).Subject.Data);
                docNumber.Should().Be(lastReceiptNumber + 1);
                lastReceiptNumber = docNumber;
                DateTime.Parse(result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment)).Subject.Data).Should().BeCloseTo(request.cbReceiptMoment, TimeSpan.FromSeconds(1));
                result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTDocumentType)).Subject.Data.Should().Be("POSRECEIPT");
            }
            await Task.Delay(TimeSpan.FromSeconds(30));
            dailyClosingResult = await itsscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = ReceiptExamples.GetDailyClosing(),
                ReceiptResponse = _receiptResponse
            });
            var nextZNumber = long.Parse(dailyClosingResult.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) SignatureTypesIT.RTZNumber)).Subject.Data);
            nextZNumber.Should().Be(lastZNumber + 1);
        }
    }
}