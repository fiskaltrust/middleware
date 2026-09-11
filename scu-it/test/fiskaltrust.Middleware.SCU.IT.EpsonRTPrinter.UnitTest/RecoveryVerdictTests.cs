using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    /// <summary>
    /// When a receipt command fails at transport level the printer may or may not have emitted the document,
    /// and the SCU has to find out which before it decides whether sending the receipt again is safe. These
    /// cover the three answers it can reach, and the rule that ties them together: a verdict is drawn from an
    /// answer, never from silence.
    /// </summary>
    public class RecoveryVerdictTests
    {
        private const string LastEmittedDocCommand = "1387";

        private static EpsonRTPrinterSCUConfiguration Configuration() => new()
        {
            // Production waits tens of seconds for the printer to speak again; the shape of the loop is what
            // is under test, not its patience.
            RecoveryVerdictTimeoutMs = 200,
            RecoveryVerdictPollIntervalMs = 10,
            RecoveryStatusQueryTimeoutMs = 10,
            MaxNetworkRetries = 1
        };

        [Fact]
        public async Task WhenTheCounterAdvanced_TheDocumentIsRecoveredWithoutReprinting()
        {
            var printer = Printer(
                status: n => n == 1 ? LastEmittedDoc(zNumber: 320, docNumber: 3) : LastEmittedDoc(zNumber: 320, docNumber: 4),
                receipt: _ => throw new TaskCanceledException());

            var response = await ProcessAsync(printer);

            response.HasFailed().Should().BeFalse();
            SignatureData(response, SignatureTypesIT.RTZNumber).Should().Be("0320");
            SignatureData(response, SignatureTypesIT.RTDocumentNumber).Should().Be("0004");
            printer.ReceiptCommands.Should().Be(1);
        }

        [Fact]
        public async Task WhenTheCounterDidNotMove_TheReceiptIsSentAgain()
        {
            // The printer answered, and a printer that answers is not in the middle of a document, so "still
            // at 3" is settled: nothing was emitted and the resend cannot duplicate anything.
            var printer = Printer(
                status: _ => LastEmittedDoc(zNumber: 320, docNumber: 3),
                receipt: n => n == 1 ? throw new TaskCanceledException() : PrintedReceipt(zNumber: 320, docNumber: 4));

            var response = await ProcessAsync(printer);

            response.HasFailed().Should().BeFalse();
            SignatureData(response, SignatureTypesIT.RTDocumentNumber).Should().Be("0004");
            printer.ReceiptCommands.Should().Be(2);
        }

        [Fact]
        public async Task WhenThePrinterNeverAnswers_TheStateIsUnknownAndNothingIsReprinted()
        {
            var printer = Printer(
                status: n => n == 1 ? LastEmittedDoc(zNumber: 320, docNumber: 3) : throw new TaskCanceledException(),
                receipt: _ => throw new TaskCanceledException());

            var response = await ProcessAsync(printer);

            response.HasFailed().Should().BeTrue();
            FailureData(response).Should().Be(EpsonRTPrinterSCU.UnknownDocumentStateError);
            printer.ReceiptCommands.Should().Be(1);
        }

        [Fact]
        public async Task WhenThePrinterIsSilentAtFirst_TheQuestionIsAskedAgain()
        {
            // A printer busy emitting the document cannot reply at all, so the first answers are silence.
            // Concluding from the first one would report an unknown state for a document that is on paper.
            var printer = Printer(
                status: n => n switch
                {
                    1 => LastEmittedDoc(zNumber: 320, docNumber: 3),
                    2 or 3 => throw new TaskCanceledException(),
                    _ => LastEmittedDoc(zNumber: 320, docNumber: 4)
                },
                receipt: _ => throw new TaskCanceledException());

            var response = await ProcessAsync(printer);

            response.HasFailed().Should().BeFalse();
            SignatureData(response, SignatureTypesIT.RTDocumentNumber).Should().Be("0004");
            printer.StatusCommands.Should().BeGreaterThan(3);
            printer.ReceiptCommands.Should().Be(1);
        }

        [Fact]
        public async Task WhenTheLastEmittedDocumentIsNotFiscal_TheStateIsUnknown()
        {
            // A non-fiscal document numbers on another counter, so it cannot be compared with the baseline.
            var printer = Printer(
                status: n => n == 1
                    ? LastEmittedDoc(zNumber: 320, docNumber: 3)
                    : LastEmittedDoc(zNumber: 320, docNumber: 1, fiscal: false),
                receipt: _ => throw new TaskCanceledException());

            var response = await ProcessAsync(printer);

            response.HasFailed().Should().BeTrue();
            FailureData(response).Should().Be(EpsonRTPrinterSCU.UnknownDocumentStateError);
            printer.ReceiptCommands.Should().Be(1);
        }

        [Fact]
        public async Task ARecoveredDocumentKeepsTheCustomerTaxId()
        {
            // The document on paper carries it, because it was in the command that printed it.
            var printer = Printer(
                status: n => n == 1 ? LastEmittedDoc(zNumber: 320, docNumber: 3) : LastEmittedDoc(zNumber: 320, docNumber: 4),
                receipt: _ => throw new TaskCanceledException());

            var response = await ProcessAsync(printer, ReceiptExamples.GetTakeAway_Delivery_Card_WithCustomerIva());

            SignatureData(response, SignatureTypesIT.RTCustomerID).Should().Be("01606720215");
        }

        [Fact]
        public async Task TheStatusQueryCarriesItsOwnShortDeadline()
        {
            // The loop can only ask several times if an unanswered question is cheap. Inheriting the client's
            // own wait would spend the whole window on the first attempt — the attempt most likely to find the
            // printer busy. A mock answers instantly, so only the deadline itself can show this.
            var configuration = Configuration();
            var printer = Printer(
                status: _ => LastEmittedDoc(zNumber: 320, docNumber: 3),
                receipt: n => n == 1 ? throw new TaskCanceledException() : PrintedReceipt(zNumber: 320, docNumber: 4));

            await ProcessAsync(printer, configuration: configuration);

            var expected = TimeSpan.FromMilliseconds(configuration.RecoveryStatusQueryTimeoutMs);
            printer.StatusTimeouts.Should().OnlyContain(t => t == expected);
            printer.ReceiptTimeouts.Should().OnlyContain(t => t == null);
        }

        private static async Task<ReceiptResponse> ProcessAsync(
            FakePrinter printer,
            ReceiptRequest request = null,
            EpsonRTPrinterSCUConfiguration configuration = null)
        {
            var sut = new EpsonRTPrinterSCU(NullLogger<EpsonRTPrinterSCU>.Instance, configuration ?? Configuration(), printer.Client);
            var response = await sut.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request ?? ReceiptExamples.GetTakeAway_Delivery_Cash(),
                ReceiptResponse = new ReceiptResponse { ftSignatures = Array.Empty<SignaturItem>() }
            });
            return response.ReceiptResponse;
        }

        private static string SignatureData(ReceiptResponse response, SignatureTypesIT type) =>
            response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (ITConstants.BASE_STATE | (long) type))?.Data;

        private static string FailureData(ReceiptResponse response) => response.ftSignatures.Single().Data;

        /// <summary>
        /// Answers by the kind of command it is asked rather than by call order: the recovery asks for the
        /// status as many times as it needs to, so a script keyed on a global index would break whenever the
        /// loop changes its mind. Each callback receives its own 1-based attempt number.
        /// </summary>
        private static FakePrinter Printer(
            Func<int, Task<HttpResponseMessage>> status,
            Func<int, Task<HttpResponseMessage>> receipt) => new(status, receipt);

        private sealed class FakePrinter
        {
            private readonly List<TimeSpan?> _statusTimeouts = new();
            private readonly List<TimeSpan?> _receiptTimeouts = new();

            public FakePrinter(Func<int, Task<HttpResponseMessage>> status, Func<int, Task<HttpResponseMessage>> receipt)
            {
                var mock = new Mock<IEpsonFpMateClient>();
                mock.Setup(c => c.SendCommandAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                    .Returns((string payload, TimeSpan? timeout) =>
                    {
                        if (payload.Contains(LastEmittedDocCommand))
                        {
                            _statusTimeouts.Add(timeout);
                            return status(_statusTimeouts.Count);
                        }

                        _receiptTimeouts.Add(timeout);
                        return receipt(_receiptTimeouts.Count);
                    });
                Client = mock.Object;
            }

            public IEpsonFpMateClient Client { get; }

            public int StatusCommands => _statusTimeouts.Count;

            public int ReceiptCommands => _receiptTimeouts.Count;

            public IEnumerable<TimeSpan?> StatusTimeouts => _statusTimeouts;

            public IEnumerable<TimeSpan?> ReceiptTimeouts => _receiptTimeouts;
        }

        /// <summary>
        /// Answer of the "last emitted document" DirectIO command, laid out as the device returns it: fixed
        /// width fields, see <see cref="LastEmittedDocStatus"/>.
        /// </summary>
        private static Task<HttpResponseMessage> LastEmittedDoc(long zNumber, long docNumber, bool fiscal = true)
        {
            var responseData = new StringBuilder()
                .Append("01")                                            // OP
                .Append(7800.ToString("D9", CultureInfo.InvariantCulture)) // total doc amount
                .Append("000000000")                                     // total vat amount
                .Append("220826")                                        // date DDMMYY
                .Append("132600")                                        // time HHMMSS
                .Append(zNumber.ToString("D4", CultureInfo.InvariantCulture))
                .Append(docNumber.ToString("D4", CultureInfo.InvariantCulture))
                .Append("99MEY123456")                                   // printer serial number
                .Append("0")                                             // lottery installed
                .Append("        ")                                      // lottery code
                .Append(fiscal ? "1" : "0")
                .ToString();

            return Soap(new PrinterCommandResponse
            {
                Success = true,
                CommandResponse = new CommandResponse { PrinterStatus = "00000000", ResponseData = responseData }
            });
        }

        private static Task<HttpResponseMessage> PrintedReceipt(long zNumber, long docNumber) =>
            Soap(new PrinterReceiptResponse
            {
                Success = true,
                Receipt = new PrinterReceiptReceiptInfo
                {
                    PrinterStatus = "00000000",
                    FiscalReceiptNumber = docNumber.ToString(CultureInfo.InvariantCulture),
                    ZRepNumber = zNumber.ToString(CultureInfo.InvariantCulture),
                    FiscalReceiptDate = "22/8/2026",
                    FiscalReceiptTime = "13:26:00",
                    SerialNumber = "99MEY123456"
                }
            });

        private static Task<HttpResponseMessage> Soap<T>(T body) where T : class =>
            Task.FromResult(new HttpResponseMessage
            {
                Content = new StringContent(SoapSerializer.Serialize(body), Encoding.UTF8, "application/xml")
            });
    }
}
