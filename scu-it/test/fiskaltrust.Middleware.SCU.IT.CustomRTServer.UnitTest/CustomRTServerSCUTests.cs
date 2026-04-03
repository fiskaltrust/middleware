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
                CashHmacKey = "testkey"
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
}
