using System.IO.Pipelines;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Azure.Core;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;
using fiskaltrust.storage.V0;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;

var accountId = Guid.Parse("");
var accessToken = "";
var testRunner = await TestRunner.InitializeDryTestRun(accountId, accessToken);
await PTCertificationExamplesAll(testRunner);
Console.WriteLine("Done");
Console.ReadLine();


async Task SubmissionRound2(TestRunner runner)
{
    //var timestamp = DateTime.UtcNow.Ticks;
    //var targetFolder = "..\\20250729";
    //if (Directory.Exists(targetFolder))
    //    Directory.Delete(targetFolder, true);
    //Directory.CreateDirectory(targetFolder);

    //// T1 => Text
    //// T2 => Text
    //// T3 => Text
    //// T4 => Text
    //// T5 => Text

    //var receiptRequest = PT_Phase2_CertificationExamples.Case_T6();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T6");
    //// T7 void is not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T7();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T7");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T8();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T8");
    //// T9 transport documents are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T9();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T9");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T10(receiptRequest.cbReceiptReference);
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T10");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T11(receiptRequest);
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T11");
    //// T12 invoice receipts are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T12();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T12");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T13();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T13");
    //// T14 Invoice receipts are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T14();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T14");
    //// T15 partial refunds are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T15();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T15");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T16();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T16");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T17();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T17");
    //// T18 Foreign currencies are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T18();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T18");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T19();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T19");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T20();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T20");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T21();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T21");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T22();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T22");
    //// T23 The application does not support mulitple pages invoice. The height of the invoice changes with the content.
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T23();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T23");
    //// T24 Credit memos are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T24();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T24");
    //// T25 Serial numbers are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T25();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T25");
    //// T26 toabcco / petrocl are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T26();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T26");
    //// T27 IEC is not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T27();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T27");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T28();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T28");
    //// T29 Not needed because our infra takes care
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T29();    
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T29");
    //// T30 Other systems are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T30();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T30");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T31();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T31");
    //// T32 Withholding taxes are not supported
    //// receiptRequest = PT_Phase2_CertificationExamples.Case_T32();
    //// await ExecuteMiddleware(receiptRequest, targetFolder, caller: "T32");
    //receiptRequest = PT_Phase2_CertificationExamples.Case_T33();
    //await runner.ExecuteMiddleware(receiptRequest, targetFolder, caller: "T33");

    //var xmlData = await runner.ExecuteJournal(new JournalRequest
    //{
    //    ftJournalType = (JournalType) 0x5054_2000_0000_0001,
    //    From = timestamp
    //});
    //File.WriteAllText($"{targetFolder}\\SAFT_journal.xml", Encoding.UTF8.GetString(xmlData));
}

async Task PTCertificationExamplesAll(TestRunner runner)
{
    var timestamp = DateTime.UtcNow.Ticks;
    var cases = new Dictionary<string, (ReceiptRequest request, ReceiptResponse response, long ticks, byte[] journal)>();
    foreach (var businessCase in PTCertificationTestCasesPhase1.Cases)
    {
        try
        {
            if (!businessCase.supported)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Skipping case {businessCase.title} as it is not supported.");
                Console.ResetColor();
                continue;
            }

            var request = businessCase.receiptRequest;
            if (businessCase.referencedCase is not null)
            {
                var receiptResponse = cases[businessCase.referencedCase].Item2;
                request.cbPreviousReceiptReference = receiptResponse.cbReceiptReference;
            }
            var result = await runner.ExecuteSign(request);
            var journal = testRunner.ExecuteJournal(new JournalRequest
            {
                ftJournalType = (JournalType) 0x5054_2000_0000_0001,
                From = result.ticks
            }).Result;
            cases[businessCase.title] = (request, result.receiptResponse, result.ticks, journal);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Completed case {businessCase.title}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error in case {businessCase.title}: {ex.Message}");
            Console.ResetColor();
        }
    }

    var basePath = "C:\\GitHub\\market-pt\\dddd";
    basePath = Path.Combine(basePath, DateTime.UtcNow.Ticks.ToString());
    Directory.CreateDirectory(basePath);
    foreach (var cased in cases)
    {
       
        await TestHelpers.StoreDataAsync(runner._cashboxid, runner._accessToken, Path.Combine(basePath, cased.Key), cased.Key, cased.Value.ticks, cased.Value.journal, cased.Value.request, cased.Value.response);
    }
    var xmlData = await runner.ExecuteJournal(new JournalRequest
    {
        ftJournalType = (JournalType) 0x5054_2000_0000_0001,
        From = timestamp
    });
    File.WriteAllText(Path.Combine(basePath, "SAFT_journal.xml"), Encoding.UTF8.GetString(xmlData));
}
