using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetPLSSCDTests
{
    private static readonly PosNetConfiguration s_configuration = new() { DeviceUrl = "tcp://localhost:6666" };

    private static PosNetPLSSCD CreateSut(FakePosNetTransport transport)
        => new(s_configuration, new PosNetClient(transport));

    private static ProcessRequest CreateSaleRequest(decimal amount = 9.99m, decimal payment = 9.99m) => new()
    {
        ReceiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
            cbChargeItems =
            [
                new ChargeItem { Description = "Candies", Amount = amount, Quantity = 1m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0011 },
            ],
            cbPayItems =
            [
                new PayItem { Description = "Cash", Amount = payment, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001 },
            ],
        },
        ReceiptResponse = new ReceiptResponse { ftCashBoxIdentification = "test", ftQueueID = Guid.NewGuid() },
    };

    [Fact]
    public async Task ProcessReceiptAsync_Sale_SendsTheFullCommandSequence_AndReadsTheFiscalNumber()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftReceiptIdentification.Should().EndWith("85");
        // The numer unikatowy is a legal element of the fiscal document and identifies the register.
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer unikatowy" && s.Data == "ZBF 2101002392");
        result.ReceiptResponse.ftCashBoxIdentification.Should().Be("ZBF 2101002392");
    }

    [Fact]
    public async Task ProcessReceiptAsync_NipReceipt_SendsTrnipsetInsideTheTransaction()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0020_0001;
        request.ReceiptRequest.cbCustomer = """{"CustomerVATId": "123-456-32-18"}""";

        await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("scomm", "trinit", "trnipset", "trline", "trpayment", "trend", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("trnipset")).Should().Contain("ni1234563218");
    }

    [Fact]
    public async Task ProcessReceiptAsync_NipReceiptWithoutCustomerVatId_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0020_0001;

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_FailedFiscalNumberReadback_DoesNotFailTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("scnt");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Numer dokumentu fiskalnego");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousCancelAfterDeviceError_PropagatesTheAmbiguity()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        transport.FailAmbiguously("prncancel");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        // Whether the cancel reached the device is unknown, so the receipt must surface the
        // ambiguous/unreachable state rather than the (already handled) device error.
        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        transport.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceErrorMidTransaction_CancelsAndSurfacesTheDeviceError()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(382);
        transport.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousOutcome_IsNeverRetriedAndSendsNothingFurther()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("trpayment");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        // The ambiguous command is sent exactly once and nothing (not even a cancel) follows —
        // a blind retry or cleanup could duplicate or destroy a successfully printed document.
        transport.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceUnreachable_PropagatesWithoutCancel()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("trinit");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
        transport.SentMnemonics.Should().Equal("scomm", "trinit");
    }

    [Fact]
    public async Task ProcessReceiptAsync_UnsettledPayments_FailValidationBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest(amount: 9.99m, payment: 5.00m));

        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_Invoice_IsRejectedWithoutDeviceInteraction()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_1002;

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ZeroReceipt_ReadsTheDeviceStatus()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2000;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("scomm");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DailyClosing_IsNotSupportedYet()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInfoAsync_MapsTheFiscalModeFromScomm()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var info = await sut.GetInfoAsync();

        var deviceInfo = PLDeviceInfo.FromPLSSCDInfo(info);
        deviceInfo.Should().NotBeNull();
        deviceInfo!.FiscalizationState.Should().Be(PLFiscalizationState.Fiscalized);
        deviceInfo.VatRateTable.Should().NotBeEmpty();
        deviceInfo.UniqueDeviceNumber.Should().Be("ZBF 2101002392");
    }

    /// <summary>
    /// A POSNET Online printer answers the status flags as T (tak) and N (nie) — the 1/0 form of
    /// the protocol description is accepted as well. Anything else must not be read as non-fiscal:
    /// a fiscalized register reported as NonFiscal can never activate a PL queue.
    /// </summary>
    [Theory]
    [InlineData("scomm\tfsT\ttzN\tts0\thrT\ttdN\t", PLFiscalizationState.Fiscalized)]
    [InlineData("scomm\tfsN\ttzN\tts0\thrT\ttdN\t", PLFiscalizationState.NonFiscal)]
    [InlineData("scomm\tfs1\ttz1\tts0\thr1\t", PLFiscalizationState.Fiscalized)]
    [InlineData("scomm\tfs0\ttz1\tts0\thr1\t", PLFiscalizationState.NonFiscal)]
    [InlineData("scomm\ttzN\tts0\thrT\t", PLFiscalizationState.Unknown)]
    [InlineData("scomm\tfsX\ttzN\tts0\thrT\t", PLFiscalizationState.Unknown)]
    public async Task GetInfoAsync_ReadsTheStatusFlagsOfARealPrinter(string status, PLFiscalizationState expected)
    {
        var transport = FakePosNetTransport.Confirming();
        transport.AnswerScommWith(status);
        var sut = CreateSut(transport);

        var info = await sut.GetInfoAsync();

        PLDeviceInfo.FromPLSSCDInfo(info)!.FiscalizationState.Should().Be(expected);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ConsecutiveSales_ReadTheRegisterIdentityOnlyOnce()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        await sut.ProcessReceiptAsync(CreateSaleRequest());
        await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal(
            "scomm", "trinit", "trline", "trpayment", "trend", "scnt",
            "trinit", "trline", "trpayment", "trend", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_QuantityWithAWholeGroszUnitPrice_SendsPriceQuantityAndValueConsistently()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(amount: 9.99m, payment: 9.99m);
        request.ReceiptRequest.cbChargeItems[0].Quantity = 3m;

        await sut.ProcessReceiptAsync(request);

        // 999 gr over 3 units: price × quantity has to equal the line value the register totalizes.
        transport.SentPayloads.Single(p => p.StartsWith("trline")).Should().Contain("pr333").And.Contain("il3.000").And.Contain("wa999");
    }

    [Fact]
    public async Task ProcessReceiptAsync_QuantityWithoutAWholeGroszUnitPrice_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(amount: 10.00m, payment: 10.00m);
        request.ReceiptRequest.cbChargeItems[0].Quantity = 3m;

        var act = () => sut.ProcessReceiptAsync(request);

        // 1000 gr over 3 units would print 3 × 333 gr = 999 gr next to a value of 1000 gr.
        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_DiscountPosition_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.cbChargeItems.Add(new ChargeItem
        {
            Description = "Rabat",
            Amount = -2m,
            Quantity = 1m,
            ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011,
        });

        var act = () => sut.ProcessReceiptAsync(request);

        // The queue passes discounts through (they do not make a document a return); the register
        // expresses them as rabat parameters, so until that is implemented they are rejected here.
        (await act.Should().ThrowAsync<PLValidationException>()).WithMessage("*discount*");
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_DescriptionWithATab_DoesNotInjectAProtocolField()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.cbChargeItems[0].Description = "Candies\tvt0\tpr1";

        await sut.ProcessReceiptAsync(request);

        // One vt and one pr, and the name travels as text: a TAB inside a value would otherwise
        // open further protocol fields and silently change the PTU slot and the price.
        var trline = transport.SentPayloads.Single(p => p.StartsWith("trline"));
        trline.Should().StartWith("trline\tnaCandies vt0 pr1\tvt1\tpr999\t#");
    }

    /// <summary>
    /// A scripted transport: confirms every command (scomm answers with a fiscal-mode status) and
    /// can be armed to fail a specific mnemonic in one of the three failure modes.
    /// </summary>
    private sealed class FakePosNetTransport : IPosNetTransport
    {
        private readonly Dictionary<string, Func<Exception>> _failures = [];

        public List<string> SentMnemonics { get; } = [];

        public List<string> SentPayloads { get; } = [];

        private string _scommResponse = "scomm\tfsT\ttzN\tts0\thrT\tnuZBF 2101002392\ttdN\t";

        public static FakePosNetTransport Confirming() => new();

        /// <summary>Answers the status read with a specific payload — device firmwares differ.</summary>
        public void AnswerScommWith(string payload) => _scommResponse = payload;

        public void FailWithDeviceError(string mnemonic, int errorCode)
            => _failures[mnemonic] = () => new ExpectedDeviceError(errorCode);

        public void FailAmbiguously(string mnemonic)
            => _failures[mnemonic] = () => new PosNetAmbiguousResponseException("no response within the receive timeout");

        public void FailUnreachable(string mnemonic)
            => _failures[mnemonic] = () => new PLDeviceUnreachableException("connection refused");

        public Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
        {
            var payload = Encoding.ASCII.GetString(frame, 1, frame.Length - 2);
            var mnemonic = payload.Split('\t')[0];
            SentMnemonics.Add(mnemonic);
            SentPayloads.Add(payload);

            if (_failures.TryGetValue(mnemonic, out var failure))
            {
                var exception = failure();
                if (exception is ExpectedDeviceError deviceError)
                {
                    return Task.FromResult(PosNetProtocolTests.EncodeResponse($"{mnemonic}\t?{deviceError.ErrorCode}\t"));
                }
                throw exception;
            }

            var responsePayload = mnemonic switch
            {
                // The T/N flags and the numer unikatowy in the shape a POSNET Online printer answers them.
                "scomm" => _scommResponse,
                "scnt" => "scnt\trd12\tbn85\tbt85\tfn3\t",
                _ => $"{mnemonic}\t",
            };
            return Task.FromResult(PosNetProtocolTests.EncodeResponse(responsePayload));
        }

        public void Dispose() { }

        private sealed class ExpectedDeviceError(int errorCode) : Exception
        {
            public int ErrorCode { get; } = errorCode;
        }
    }
}
