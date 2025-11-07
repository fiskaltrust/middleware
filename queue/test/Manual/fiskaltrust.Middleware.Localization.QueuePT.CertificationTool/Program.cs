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
using fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;
using fiskaltrust.storage.V0;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;

var accountId = Guid.Parse("");
var accessToken = "";
var baseFolder = "C:\\GitHub\\market-pt\\doc\\certification\\Submissions\\2025-09-20";
var testRunner = await TestRunner.InitializeDryTestRun(accountId, accessToken);
Console.WriteLine("Starting Phase 1 Certification Tests...");
await PTCertificationExamplesPhase1(testRunner, false);

// Run Phase 1 certification tests
var testRunner2 = await TestRunner.InitializeDryTestRun(accountId, accessToken);
Console.WriteLine("Starting Phase 2 Certification Tests...");
await PTCertificationExamplesPhase2(testRunner2, false);

// Run Phase 2 certification tests
//Console.WriteLine("Starting Phase 2 Certification Tests...");
//await PTCertificationExamplesPhase2(testRunner);

Console.WriteLine("Done");
Console.ReadLine();



async Task PTCertificationExamplesPhase1(TestRunner runner, bool addTicks = false)
{
    var timestamp = DateTime.UtcNow.Ticks;
    var cases = new Dictionary<string, (ReceiptRequest request, ReceiptResponse response, long ticks, byte[] journal)>();
    foreach (var businessCase in PTCertificationTestCasesPhase1.Cases)
    {
        try
        {
            if (!businessCase.supported || businessCase.receiptRequest == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Skipping case {businessCase.title} as it is not supported or has no receipt request.");
                Console.ResetColor();
                continue;
            }

            var request = businessCase.receiptRequest;
            if (businessCase.referencedCase is not null && cases.ContainsKey(businessCase.referencedCase))
            {
                var receiptResponse = cases[businessCase.referencedCase].Item2;
                request.cbPreviousReceiptReference = receiptResponse.cbReceiptReference ?? "";
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

    var basePath = Path.Combine(baseFolder, "phase1");
    if (addTicks)
    {
        basePath = Path.Combine(basePath, DateTime.UtcNow.Ticks.ToString());
    }
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
    File.WriteAllText(Path.Combine(basePath, "SAFT_journal.xml"), Encoding.GetEncoding("windows-1252").GetString(xmlData), Encoding.GetEncoding("windows-1252"));
}

async Task PTCertificationExamplesPhase2(TestRunner runner, bool addTicks = false)
{
    var timestamp = DateTime.UtcNow.Ticks;
    var cases = new Dictionary<string, (ReceiptRequest request, ReceiptResponse response, long ticks, byte[] journal)>();
    foreach (var businessCase in PTCertificationTestCasesPhase2.Cases)
    {
        try
        {
            if (!businessCase.supported || businessCase.receiptRequest == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Skipping case {businessCase.title} as it is not supported or has no receipt request.");
                Console.ResetColor();
                continue;
            }

            var request = businessCase.receiptRequest;
            if (businessCase.referencedCase is not null && cases.ContainsKey(businessCase.referencedCase))
            {
                var receiptResponse = cases[businessCase.referencedCase].Item2;
                request.cbPreviousReceiptReference = receiptResponse.cbReceiptReference ?? "";
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

    var basePath = Path.Combine(baseFolder, "phase2");
    if (addTicks)
    {
        basePath = Path.Combine(basePath, DateTime.UtcNow.Ticks.ToString());
    }
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
    File.WriteAllText(Path.Combine(basePath, "SAFT_journal.xml"), Encoding.GetEncoding("windows-1252").GetString(xmlData), Encoding.GetEncoding("windows-1252"));
}

