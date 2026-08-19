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

        transport.SentMnemonics.Should().Equal("trinit", "trline", "trpayment", "trend", "scnt");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftReceiptIdentification.Should().EndWith("85");
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

        transport.SentMnemonics.Should().Equal("trinit", "trnipset", "trline", "trpayment", "trend", "scnt");
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

        transport.SentMnemonics.Should().Equal("trinit", "trline", "trpayment", "trend", "scnt");
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
        transport.SentMnemonics.Should().Equal("trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceErrorMidTransaction_CancelsAndSurfacesTheDeviceError()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(382);
        transport.SentMnemonics.Should().Equal("trinit", "trline", "prncancel");
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
        transport.SentMnemonics.Should().Equal("trinit", "trline", "trpayment");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceUnreachable_PropagatesWithoutCancel()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("trinit");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
        transport.SentMnemonics.Should().Equal("trinit");
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

        public static FakePosNetTransport Confirming() => new();

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
                "scomm" => "scomm\tfs1\ttz1\tts0\thr1\t",
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
