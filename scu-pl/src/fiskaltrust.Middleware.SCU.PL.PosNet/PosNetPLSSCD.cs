using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

namespace fiskaltrust.Middleware.SCU.PL.PosNet;

/// <summary>
/// IPLSSCD implementation driving a POSNET Online fiscal printer over TCP — the certified
/// register owns numbering, the PTU table, reports and the CRK transmission; this SCU translates
/// receipt cases into the trinit → trline → trpayment → trend flow. First milestone
/// (middleware#751): fiscal sale receipts and the status read behind the zero receipt. Reports,
/// returns, non-fiscal printouts and device setup are follow-ups.
/// </summary>
public class PosNetPLSSCD : IPLSSCD, IDisposable
{
    private readonly PosNetClient _client;
    private readonly PtuSlotResolver _ptuSlotResolver;
    private readonly PosNetConfiguration _configuration;

    public PosNetPLSSCD(PosNetConfiguration configuration)
        : this(configuration, new PosNetClient(new TcpPosNetTransport(configuration))) { }

    public PosNetPLSSCD(PosNetConfiguration configuration, PosNetClient client)
    {
        _configuration = configuration;
        _client = client;
        _ptuSlotResolver = new PtuSlotResolver(configuration.VatRateTable);
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public async Task<PLSSCDInfo> GetInfoAsync()
    {
        var status = await _client.ExecuteAsync(PosNetCommands.Scomm());
        return ToDeviceInfo(status).ToPLSSCDInfo();
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase;
        var response = request.ReceiptResponse;

        if (receiptCase.IsType(ReceiptCaseType.Invoice))
        {
            throw new PLValidationException("Invoice cases (0x1xxx) must not reach a Polish SCU — QueuePL persists them without fiscalization.");
        }

        if (IsFiscalReceiptCase(receiptCase))
        {
            await ExecuteSaleAsync(request.ReceiptRequest);
        }
        else if (receiptCase.IsCase(ReceiptCase.ZeroReceipt0x2000))
        {
            // The zero receipt is the operator's connectivity/state probe: one status read must
            // succeed. The printer state itself is returned via GetInfoAsync.
            await _client.ExecuteAsync(PosNetCommands.Scomm());
        }
        else if (receiptCase.IsCase(ReceiptCase.DailyClosing0x2011)
            || receiptCase.IsCase(ReceiptCase.MonthlyClosing0x2012)
            || receiptCase.IsCase(ReceiptCase.YearlyClosing0x2013))
        {
            throw new PLValidationException("Daily and periodic reports are not supported by the PosNet SCU yet (follow-up to middleware#751).");
        }

        // Non-fiscal receipt cases pass through without device interaction — like the InMemory
        // SCU, only fiscal documents talk to the register.
        return new ProcessResponse { ReceiptResponse = response };
    }

    private async Task ExecuteSaleAsync(ReceiptRequest request)
    {
        var commands = PosNetReceiptMapper.MapSale(request, _ptuSlotResolver);
        var executed = 0;
        try
        {
            foreach (var command in commands)
            {
                await _client.ExecuteAsync(command);
                executed++;
            }
        }
        catch (PLDeviceErrorException)
        {
            // The device rejected a command mid-transaction with a definite answer, so the
            // transaction is safely cancellable. After an ambiguous or unreachable outcome
            // nothing more is sent — the device state must be verified by the operator first.
            if (executed > 0)
            {
                await TryCancelAsync();
            }
            throw;
        }
    }

    private async Task TryCancelAsync()
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.Prncancel());
        }
        catch (PLSSCDException)
        {
            // Best effort: cancelling an already-closed transaction is rejected by the device;
            // the original error stays the reported failure.
        }
    }

    private PLDeviceInfo ToDeviceInfo(PosNetResponse status) => new()
    {
        FiscalizationState = status.Parameters.TryGetValue("fs", out var fiscalMode) && fiscalMode == "1"
            ? PLFiscalizationState.Fiscalized
            : PLFiscalizationState.NonFiscal,
        VatRateTable = _configuration.VatRateTable,
        // The scomm status does not carry the register identity; reading the numer unikatowy
        // (getrealid) is a follow-up to middleware#751.
        DeviceSerialNumber = null,
        UniqueDeviceNumber = null,
    };

    private static bool IsFiscalReceiptCase(ReceiptCase receiptCase)
        => receiptCase.IsType(ReceiptCaseType.Receipt)
            && (receiptCase.IsCase(ReceiptCase.UnknownReceipt0x0000)
                || receiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001)
                || receiptCase.IsCase(ReceiptCase.ECommerce0x0004));

    public void Dispose() => _client.Dispose();
}
