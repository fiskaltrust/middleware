using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest;

/// <summary>
/// Unit tests for CustomRTServerSCU.ProcessReceiptAsync — covers only the cases
/// added as fixes for issues #581 and #584.
///
/// Strategy: a JSON state-cache file is written to a temp dir so that
/// ReloadCashUUID returns without making any HTTP calls, keeping the NoOp tests
/// fully offline. For Monthly/Yearly closing, a 100 ms timeout lets the HTTP
/// call to the unreachable fake server fail fast; PerformDailyCosingAsync has
/// its own try/catch that emits "rt-server-dailyclosing-error" — distinct from
/// the outer "rt-server-generic-error" — which proves routing reached the
/// correct handler.
/// </summary>
public class CustomRTServerSCUTests : IDisposable
{
    private static readonly Guid _scuId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _queueId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string _cashBoxId = "0001test001";
    private readonly string _tempDir;

    public CustomRTServerSCUTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Pre-populate the state cache so ReloadCashUUID skips the HTTP call.
        var cache = new Dictionary<Guid, QueueIdentification>
        {
            [_queueId] = new QueueIdentification
            {
                CashUuId = _cashBoxId,
                CashStatus = "1",
                LastZNumber = 5,
                LastDocNumber = 10,
                CurrentGrandTotal = 0,
                RTServerSerialNumber = "TESTSERIAL",
                LastSignature = "testsig",
                CashHmacKey = "dGVzdGtleQ==" // base64("testkey"); GenerateQRCodeData requires a valid base64 HMAC key

            }
        };
        File.WriteAllText(
            Path.Combine(_tempDir, $"{_scuId}_customrtserver_statecache.json"),
            JsonConvert.SerializeObject(cache)
        );
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private CustomRTServerSCU CreateSut(int httpTimeoutMs = 5000)
    {
        var config = new CustomRTServerConfiguration
        {
            ServerUrl = "http://127.0.0.1:1/",
            Password = "test",
            ServiceFolder = _tempDir,
            RTServerHttpTimeoutInMs = httpTimeoutMs,
            IgnoreRTServerErrors = false
        };
        var client = new CustomRTServerClient(config, NullLogger<CustomRTServerClient>.Instance);
        var queue = new CustomRTServerCommunicationQueue(_scuId, client, NullLogger<CustomRTServerCommunicationQueue>.Instance, config);
        return new CustomRTServerSCU(_scuId, NullLogger<CustomRTServerSCU>.Instance, config, client, queue);
    }

    private ProcessRequest CreateRequest(long ftReceiptCase) => new ProcessRequest
    {
        ReceiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ftReceiptCase,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        },
        ReceiptResponse = new ReceiptResponse
        {
            ftQueueID = _queueId.ToString(),
            ftCashBoxIdentification = _cashBoxId,
            ftState = 0x4954_2000_0000_0000,
            ftSignatures = Array.Empty<SignaturItem>()
        }
    };

    [Fact]
    public async Task ProcessReceiptAsync_ProtocolUnspecified0x3000_ReturnsNoOp()
    {
        var sut = CreateSut();
        var result = await sut.ProcessReceiptAsync(CreateRequest(0x4954_2000_0000_3000));

        result.ReceiptResponse.ftSignatures.Should().BeEmpty();
        result.ReceiptResponse.ftState.Should().Be(0x4954_2000_0000_0000);
    }

    // Fix #581b
    [Fact]
    public async Task ProcessReceiptAsync_UnknownReceiptCase_ReturnsNoOp()
    {
        var sut = CreateSut();
        var result = await sut.ProcessReceiptAsync(CreateRequest(0x4954_2000_0000_9999));

        result.ReceiptResponse.ftSignatures.Should().BeEmpty();
        result.ReceiptResponse.ftState.Should().Be(0x4954_2000_0000_0000);
    }

    [Fact]
    public async Task ProcessReceiptAsync_PointOfSaleReceiptWithLotteryCode_EmitsLotterySignature()
    {
        var sut = CreateSut();
        var request = CreateRequest((long) ITReceiptCases.PointOfSaleReceipt0x0001);
        request.ReceiptRequest.ftReceiptCaseData = JsonConvert.SerializeObject(new ReceiptCaseLotteryData
        {
            servizi_lotteriadegliscontrini_gov_it = new servizi_lotteriadegliscontrini_gov_it { codicelotteria = "ABCD1234" }
        });

        var result = await sut.ProcessReceiptAsync(request);

        result.ReceiptResponse.ftSignatures.Should().Contain(s => s.Caption == "<rt-lottery-id>" && s.Data == "ABCD1234");
    }

    [Fact]
    public async Task ProcessReceiptAsync_PointOfSaleReceiptWithoutLotteryCode_OmitsLotterySignature()
    {
        var sut = CreateSut();
        var result = await sut.ProcessReceiptAsync(CreateRequest((long) ITReceiptCases.PointOfSaleReceipt0x0001));

        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "<rt-lottery-id>");
    }

    // Fix #581a
    [Theory]
    [InlineData(0x4954_2000_0000_2012L)]   // MonthlyClosing0x2012
    [InlineData(0x4954_2000_0000_2013L)]   // YearlyClosing0x2013
    public async Task ProcessReceiptAsync_MonthlyAndYearlyClosing_RoutesToDailyClosingHandler(long ftReceiptCase)
    {
        var sut = CreateSut(httpTimeoutMs: 100);
        var result = await sut.ProcessReceiptAsync(CreateRequest(ftReceiptCase));

        result.ReceiptResponse.ftSignatures.Should().Contain(s => s.Caption == "rt-server-dailyclosing-error");
        result.ReceiptResponse.ftState.Should().Be(0x4954_2001_EEEE_EEEE);
    }

    [Fact]
    public void GenerateFiscalDocument_WithLotteryCode_EmbedsLotteryClientCodeInFiscalData()
    {
        var queueIdentification = new QueueIdentification { CashUuId = _cashBoxId, CashHmacKey = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }) };
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (long) ITReceiptCases.PointOfSaleReceipt0x0001,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>(),
            ftReceiptCaseData = JsonConvert.SerializeObject(new ReceiptCaseLotteryData
            {
                servizi_lotteriadegliscontrini_gov_it = new servizi_lotteriadegliscontrini_gov_it { codicelotteria = "ABCD1234" }
            })
        };

        (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.GenerateFiscalDocument(receiptRequest, queueIdentification);

        fiscalDocument.document.Should().BeOfType<DocumentDataLottery>();
        ((DocumentDataLottery) fiscalDocument.document).lottery_client_code.Should().Be("ABCD1234");
        commercialDocument.fiscalData.Should().Contain("\"lottery_client_code\":\"ABCD1234\"");
    }

    [Fact]
    public void EnqueueDocument_RoutesLotteryAndNormalDocumentsToDistinctCacheFiles()
    {
        var cacheDir = Path.Combine(_tempDir, "queuecache");
        var config = new CustomRTServerConfiguration
        {
            ServerUrl = "http://127.0.0.1:1/",
            Password = "test",
            SendReceiptsSync = false, // cache to disk; unreachable server means files are never deleted
            CacheDirectory = cacheDir,
            RTServerHttpTimeoutInMs = 100
        };
        var client = new CustomRTServerClient(config, NullLogger<CustomRTServerClient>.Instance);
        using var queue = new CustomRTServerCommunicationQueue(_scuId, client, NullLogger<CustomRTServerCommunicationQueue>.Instance, config);

        var doc = new CommercialDocument { fiscalData = "{}", qrData = new QrCodeData() };
        queue.EnqueueDocument(_cashBoxId, doc, 3, 541, isLottery: true).GetAwaiter().GetResult();
        queue.EnqueueDocument(_cashBoxId, doc, 3, 542, isLottery: false).GetAwaiter().GetResult();

        var files = Directory.GetFiles(Path.Combine(cacheDir, _cashBoxId));
        files.Should().Contain(f => f.EndsWith("_commercialdocumentlottery.json"));
        files.Should().Contain(f => f.EndsWith("_commercialdocument.json") && !f.EndsWith("_commercialdocumentlottery.json"));
    }

    [Fact]
    public void GenerateFiscalDocument_WithoutLotteryCode_DoesNotEmbedLotteryClientCode()
    {
        var queueIdentification = new QueueIdentification { CashUuId = _cashBoxId, CashHmacKey = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }) };
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (long) ITReceiptCases.PointOfSaleReceipt0x0001,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = Array.Empty<ChargeItem>(),
            cbPayItems = Array.Empty<PayItem>()
        };

        (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.GenerateFiscalDocument(receiptRequest, queueIdentification);

        fiscalDocument.document.Should().NotBeOfType<DocumentDataLottery>();
        commercialDocument.fiscalData.Should().NotContain("lottery_client_code");
    }
}
