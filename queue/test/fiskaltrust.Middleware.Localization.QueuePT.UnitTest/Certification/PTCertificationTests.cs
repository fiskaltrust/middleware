using System.IO.Pipelines;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

[Collection("Sequential")]
[Trait("only", "local")]
public class PTCertificationTests
{
    private readonly Func<string, Task<string>> _signMethod;
    private readonly Func<string, Task<(ContentType contentType, PipeReader reader)>> _journalMethod;
    private readonly Guid _cashboxid;

    public PTCertificationTests()
    {
        (var bootstrapper, var cashBoxId) = Task.Run(() => InitializeQueueGRBootstrapperAsync()).Result;
        _signMethod = bootstrapper.RegisterForSign();
        _journalMethod = bootstrapper.RegisterForJournal();
        _cashboxid = cashBoxId;
    }

    public async Task<(QueuePTBootstrapper bootstrapper, Guid cashBoxId)> InitializeQueueGRBootstrapperAsync()
    {
        var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
        var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
        var configuration = await TestHelpers.GetConfigurationAsync(cashBoxId, accessToken);
        var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
        var ptSSCD = new InMemorySCU(new ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("..\\PrivateKey.pem"),

            SoftwareCertificateNumber = "9999"
        });
        var bootstrapper = new QueuePTBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>(), ptSSCD);
        return (bootstrapper, cashBoxId);
    }

    private async Task ExecuteMiddleware(ReceiptRequest receiptRequest, string basePath, [CallerMemberName] string caller = "")
    {
        using var scope = new AssertionScope();
        var (ticks, receiptResponse) = await ExecuteSign(receiptRequest);
        await TestHelpers.StoreDataAsync(Path.Combine(basePath, caller), caller, ticks, _journalMethod, receiptRequest, receiptResponse);
    }

    private async Task ExecuteMiddleware(ReceiptRequest receiptRequest, [CallerMemberName] string caller = "")
    {
        await ExecuteMiddleware(receiptRequest, "..\\Examples", caller);
    }

    private async Task<(long ticks, ReceiptResponse receiptResponse)> ExecuteSign(ReceiptRequest receiptRequest)
    {
        receiptRequest.ftCashBoxID = _cashboxid;
        var ticks = DateTime.UtcNow.Ticks;
        var exampleCashSalesResponse = await _signMethod(JsonSerializer.Serialize(receiptRequest));
        var receiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
        if ((receiptResponse.ftState & (State) 0xF) != State.Success)
        {
            var errors = "The receiptResponse.ftState is not Success";
            errors += Environment.NewLine;
            errors += string.Join(Environment.NewLine, receiptResponse.ftSignatures.Select(x => x.Data));
            throw new Exception(errors);
        }

        return (ticks, receiptResponse);
    }

    [Fact(Skip = "")]
    public async Task RunStartReceipt()
    {
        var result = await _signMethod(JsonSerializer.Serialize(new ReceiptRequest
        {
            ftCashBoxID = _cashboxid,
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [],
            cbPayItems = [],
            cbUser = 1,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0000).WithCase(ReceiptCase.InitialOperationReceipt0x4001),
        }));
    }

    [Fact(Skip = "")]
    public async Task RunJournalCall()
    {
        var xmlData = await _journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
        {
            ftJournalType = 0x5054_2000_0000_0001
        }));
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamplesAll()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        //var targetFolder = "/Users/stefan.kert/Desktop/Sources/PT_Certification";
        var targetFolder = "..\\20250729";

        var receiptRequest = PTCertificationExamples.Case_5_9();
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_9");

        receiptRequest = PTCertificationExamples.Case_5_1();
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_1");

        receiptRequest = PTCertificationExamples.Case_5_3();
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_3");

        receiptRequest = PTCertificationExamples.Case_5_4(receiptRequest.cbReceiptReference);
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_4");

        receiptRequest = PTCertificationExamples.Case_5_5(receiptRequest.cbReceiptReference);
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_5");

        receiptRequest = PTCertificationExamples.Case_5_6();
        var _56receipt = receiptRequest;
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_6");

        receiptRequest = PTCertificationExamples.Case_5_7();
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_7");

        receiptRequest = PTCertificationExamples.Case_5_10();
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_10");

        receiptRequest = PTCertificationExamples.Case_5_12();
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_12");

        receiptRequest = PTCertificationExamples.Case_5_13_2_Payment(_56receipt.cbReceiptReference);
        await ExecuteMiddleware(receiptRequest, targetFolder, caller: "Case_5_13");

        var xmlData = await _journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
        {
            ftJournalType = 0x5054_2000_0000_0001,
            From = timestamp
        }));
        //File.WriteAllText($"{targetFolder}\\SAFT_journal.xml", xmlData);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_1()
    {
        var receiptRequest = PTCertificationExamples.Case_5_1();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_2()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_3()
    {
        var receiptRequest = PTCertificationExamples.Case_5_3();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_4()
    {
        var receiptRequest = PTCertificationExamples.Case_5_3();
        var (ticks, receiptResponse) = await ExecuteSign(receiptRequest);
        var invoiceRequest = PTCertificationExamples.Case_5_4(receiptRequest.cbReceiptReference);
        await ExecuteMiddleware(invoiceRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_5()
    {
        var receiptRequest = PTCertificationExamples.Case_5_1();
        var (ticks, receiptResponse) = await ExecuteSign(receiptRequest);
        var refundRequest = PTCertificationExamples.Case_5_5(receiptRequest.cbReceiptReference);
        await ExecuteMiddleware(refundRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_6()
    {
        var receiptRequest = PTCertificationExamples.Case_5_6();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_7()
    {
        var receiptRequest = PTCertificationExamples.Case_5_7();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_8()
    {
        var receiptRequest = PTCertificationExamples.Case_5_8();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_9()
    {
        var receiptRequest = PTCertificationExamples.Case_5_9();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_10()
    {
        var receiptRequest = PTCertificationExamples.Case_5_10();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_11()
    {
        var receiptRequest = PTCertificationExamples.Case_5_11();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_12()
    {
        var receiptRequest = PTCertificationExamples.Case_5_12();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_13()
    {
        var receiptRequest = PTCertificationExamples.Case_5_3();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_13_1()
    {
        var receiptRequest = PTCertificationExamples.Case_5_13_1_Invoice();
        await ExecuteMiddleware(receiptRequest);
    }

    [Fact(Skip = "")]
    public async Task PTCertificationExamples_Case_5_13_2()
    {
        var receiptRequest = PTCertificationExamples.Case_5_6();
        var _56receipt = receiptRequest;
        await ExecuteMiddleware(receiptRequest, caller: "Case_5_6");
        receiptRequest = PTCertificationExamples.Case_5_13_2_Payment(_56receipt.cbReceiptReference);
        await ExecuteMiddleware(receiptRequest);
    }
}
