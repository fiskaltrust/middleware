using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    /// <summary>
    /// The recovery baseline is the document counter as it stood before a receipt was sent. When a receipt
    /// ends in "unknown document state" the counter was never confirmed, so the cached baseline may already
    /// be behind the printer. Keeping it lets the NEXT receipt read "the counter advanced" and adopt a
    /// document that belongs to the previous one.
    /// </summary>
    public class StaleBaselineRecoveryTests
    {
        private static EpsonRTPrinterSCU CreateSut(IEpsonFpMateClient client) =>
            new(NullLogger<EpsonRTPrinterSCU>.Instance,
                new EpsonRTPrinterSCUConfiguration
                {
                    // What the baseline does across two receipts is under test, not how long the verdict loop
                    // is willing to wait. One retry also keeps RetryReceiptWithRecoveryAsync's 1s back-off
                    // out of the runtime.
                    RecoveryVerdictTimeoutMs = 200,
                    RecoveryVerdictPollIntervalMs = 10,
                    RecoveryStatusQueryTimeoutMs = 10,
                    MaxNetworkRetries = 1
                },
                client);

        private static Task<ProcessResponse> Sign(EpsonRTPrinterSCU sut, string reference) =>
            sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = PosReceipt(reference),
                ReceiptResponse = new ReceiptResponse()
            });

        [Fact]
        public async Task AfterUnknownDocumentState_TheNextReceiptDoesNotAdoptTheEarlierDocument()
        {
            // The printer is at Z 320 / document 2.
            var printer = new ScriptedPrinter { StatusQueriesBeforeFailure = 1 };
            var sut = CreateSut(printer);

            // Receipt A: the printer prints document 3, the answer is lost, and the follow-up status read
            // fails too — so the SCU cannot confirm what happened and reports "unknown document state".
            var a = await Sign(sut, "receipt-a");
            Assert.Equal(EpsonRTPrinterSCU.UnknownDocumentStateError, FailureData(a));

            // The link is back. Receipt B fails BEFORE the printer prints anything.
            printer.StatusQueriesBeforeFailure = int.MaxValue;
            printer.FirstPrintingSend = int.MaxValue;

            var b = await Sign(sut, "receipt-b");

            // B must not be handed document 3 — that document belongs to A and was never printed for B.
            Assert.Null(DocNumber(b));
            Assert.True(HasFailed(b), "receipt B was reported as fiscalized although it was never printed");
        }

        [Fact]
        public async Task UnknownDocumentState_DropsTheBaselineSoTheNextReceiptReReadsIt()
        {
            var printer = new ScriptedPrinter { StatusQueriesBeforeFailure = 1 };
            var sut = CreateSut(printer);

            await Sign(sut, "receipt-a");

            printer.StatusQueriesBeforeFailure = int.MaxValue;
            printer.ResetCounters();

            await Sign(sut, "receipt-b");

            // A retained baseline would let B go straight to the printer with A's counter.
            Assert.Equal(1, printer.StatusQueriesBeforeFirstSend);
        }

        [Fact]
        public async Task UnknownDocumentStateInsideTheRetryLoop_AlsoDropsTheBaseline()
        {
            // Baseline read succeeds, the recovery's first read succeeds, the read after the retry fails.
            var printer = new ScriptedPrinter { StatusQueriesBeforeFailure = 2, FirstPrintingSend = 2 };
            var sut = CreateSut(printer);

            var a = await Sign(sut, "receipt-a");
            Assert.Equal(EpsonRTPrinterSCU.UnknownDocumentStateError, FailureData(a));

            printer.StatusQueriesBeforeFailure = int.MaxValue;
            printer.ResetCounters();

            await Sign(sut, "receipt-b");

            Assert.Equal(1, printer.StatusQueriesBeforeFirstSend);
        }

        private static bool HasFailed(ProcessResponse response) =>
            ((ulong) response.ReceiptResponse.ftState & 0xEEEE_EEEE) == 0xEEEE_EEEE;

        private static string FailureData(ProcessResponse response) =>
            response.ReceiptResponse.ftSignatures?.FirstOrDefault(x => x.Caption == "FAILURE")?.Data;

        private static string DocNumber(ProcessResponse response) =>
            response.ReceiptResponse.ftSignatures?.FirstOrDefault(x => x.Caption == "<rt-doc-number>")?.Data;

        private static ReceiptRequest PosReceipt(string reference) => new()
        {
            ftCashBoxID = Guid.Empty.ToString(),
            cbTerminalID = "00010001",
            cbReceiptReference = reference,
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCase = 0x4954_2000_0000_0001,
            cbChargeItems = new ChargeItem[]
            {
                new()
                {
                    Position = 0,
                    Quantity = 1,
                    Description = "Arnica 50ml",
                    Amount = 78m,
                    VATRate = 22m,
                    ftChargeItemCase = 0x4954_2000_0000_0003
                }
            },
            cbPayItems = new PayItem[]
            {
                new()
                {
                    Position = 0,
                    Quantity = 1,
                    Description = "Cash",
                    Amount = 78m,
                    ftPayItemCase = 0x4954_2000_0000_0005
                }
            }
        };

        /// <summary>
        /// An RT printer whose answers can be scripted. A send always loses its answer — the document may or
        /// may not be on paper, which is exactly the state the recovery has to resolve.
        /// </summary>
        private sealed class ScriptedPrinter : IEpsonFpMateClient
        {
            private const string LastEmittedDocCommand = "1387";

            public long ZNumber { get; set; } = 320;
            public long DocNumber { get; set; } = 2;

            /// <summary>Status reads start failing once this many have been answered.</summary>
            public int StatusQueriesBeforeFailure { get; set; } = int.MaxValue;

            /// <summary>1-based index of the first send that actually puts a document on paper.</summary>
            public int FirstPrintingSend { get; set; } = 1;

            public int StatusQueries { get; private set; }
            public int Sends { get; private set; }

            /// <summary>
            /// Status reads answered before the first send. A receipt that re-reads its baseline shows 1
            /// here; one that reuses a cached baseline shows 0.
            /// </summary>
            public int StatusQueriesBeforeFirstSend { get; private set; }

            public void ResetCounters()
            {
                StatusQueries = 0;
                Sends = 0;
                StatusQueriesBeforeFirstSend = 0;
            }

            public Task<HttpResponseMessage> SendCommandAsync(string payload, TimeSpan? timeout = null)
            {
                if (payload.Contains(LastEmittedDocCommand))
                {
                    StatusQueries++;
                    if (StatusQueries > StatusQueriesBeforeFailure)
                    {
                        throw new HttpRequestException("the printer could not be reached");
                    }

                    return Task.FromResult(Soap(LastEmittedDocXml(ZNumber, DocNumber)));
                }

                if (Sends == 0)
                {
                    StatusQueriesBeforeFirstSend = StatusQueries;
                }

                Sends++;
                if (Sends >= FirstPrintingSend)
                {
                    DocNumber++;
                }

                throw new TaskCanceledException(
                    "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.");
            }

            private static HttpResponseMessage Soap(string xml) => new() { Content = new StringContent(xml) };

            /// <summary>
            /// Answer of the "last emitted document" DirectIO command (1387/01), in the fixed-width layout
            /// <see cref="LastEmittedDocStatus"/> parses.
            /// </summary>
            private static string LastEmittedDocXml(long zNumber, long docNumber)
            {
                var responseData = new StringBuilder()
                    .Append("01")                     // OP
                    .Append(7800.ToString("D9"))      // total doc amount, cents
                    .Append("000001407")              // total vat amount, cents
                    .Append("220826")                 // date DDMMYY
                    .Append("132600")                 // time HHMMSS
                    .Append(zNumber.ToString("D4"))
                    .Append(docNumber.ToString("D4"))
                    .Append("99IEB132091")            // printer serial number, 11 bytes
                    .Append('0')                      // lottery installed
                    .Append("        ")               // lottery code
                    .Append('1')                      // fiscal document
                    .ToString();

                return SoapSerializer.Serialize(new PrinterCommandResponse
                {
                    Success = true,
                    CommandResponse = new CommandResponse
                    {
                        PrinterStatus = "00000000",
                        ResponseData = responseData
                    }
                });
            }
        }
    }
}
