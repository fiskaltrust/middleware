using System;
using System.Globalization;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
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

    /// <summary>
    /// Serializes complete device sequences. The client's own lock only makes single commands
    /// atomic — a sale is bind → trinit … trend (+ readbacks), and the SCU is a singleton, so
    /// without this lock a concurrent plain sale could slip its trinit between another sale's
    /// eparagonidznext and trinit and consume that customer's e-receipt binding.
    /// </summary>
    private readonly System.Threading.SemaphoreSlim _deviceLock = new(1, 1);

    /// <summary>
    /// The register identity, read once per SCU instance. The numer unikatowy is assigned to a
    /// device for its lifetime, so re-reading it before every receipt would only add a round trip.
    /// </summary>
    private PLDeviceInfo? _identity;

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
            await _deviceLock.WaitAsync();
            try
            {
                await ExecuteSaleAsync(request.ReceiptRequest, response);
            }
            finally
            {
                _deviceLock.Release();
            }
        }
        else if (receiptCase.IsCase(ReceiptCase.ZeroReceipt0x2000))
        {
            // The zero receipt is the operator's connectivity/state probe: one status read must
            // succeed. The printer state itself is returned via GetInfoAsync.
            await _deviceLock.WaitAsync();
            try
            {
                await _client.ExecuteAsync(PosNetCommands.Scomm());
            }
            finally
            {
                _deviceLock.Release();
            }
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

    private async Task ExecuteSaleAsync(ReceiptRequest request, ReceiptResponse response)
    {
        // Both validations run before any frame is sent: a rejected IDZ or an unmappable sale must
        // not leave anything on the device.
        var eReceiptCustomerId = PosNetReceiptMapper.GetEReceiptCustomerId(request);
        var commands = PosNetReceiptMapper.MapSale(request, _ptuSlotResolver);

        // The numer unikatowy is a legal element of the fiscal document, so the response carries
        // it like the InMemory SCU does. Reading it before trinit keeps the order safe: a register
        // that cannot answer its status has not been asked to open a transaction either.
        await EnrichWithDeviceIdentityAsync(response);

        // The e-paragon binding goes out strictly before trinit: eparagonidznext binds the *next*
        // document, and a failed binding fails the sale while nothing has been printed yet. A
        // confirmed error needs no cleanup (no transaction is open); an ambiguous outcome
        // propagates without retry like every other command.
        var eDocumentId = eReceiptCustomerId is null ? (uint?)null : await BindEReceiptAsync(eReceiptCustomerId);

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
                await CancelAsync();
            }
            throw;
        }

        await TryReadFiscalDocumentNumberAsync(response);

        if (eDocumentId is { } documentId)
        {
            response.EnrichWithEDocumentId(documentId);
            await TryReadEDocumentDeliveryStateAsync(response, documentId);
        }
    }

    /// <summary>
    /// Binds the next document to the e-receipt customer identifier (IDZ) and returns the unique
    /// eDokument id (<c>ha</c>) the register assigned. Runs before trinit, so any failure here
    /// fails the sale with certainty that nothing was printed.
    /// </summary>
    private async Task<uint> BindEReceiptAsync(string eReceiptCustomerId)
    {
        PosNetResponse binding;
        try
        {
            binding = await _client.ExecuteAsync(PosNetCommands.EparagonIdzNext(eReceiptCustomerId));
        }
        catch (PLDeviceErrorException exception) when (exception.ErrorCode == 2034)
        {
            // ERR_NO_FISC_MODE: eDokument commands only work on a fiscalized register (training
            // mode does not unlock them). The sale fails here, before anything is printed.
            throw new PLDeviceErrorException(exception.ErrorCode,
                "The POSNET printer rejected the e-receipt binding (eparagonidznext) with error 2034 (ERR_NO_FISC_MODE): the device is not fiscalized, so eDokument emission is unavailable. Nothing was printed.");
        }

        if (!binding.Parameters.TryGetValue("ha", out var handle) || !uint.TryParse(handle, NumberStyles.None, CultureInfo.InvariantCulture, out var eDocumentId))
        {
            // The register confirmed the binding, so it is armed for the *next* document even
            // though the promised ha is missing — without cleanup, a later plain sale would
            // inherit this customer's IDZ and deliver their e-receipt to the wrong recipient.
            // Clear the binding first, then fail the sale (still nothing printed).
            await CancelEReceiptBindingAsync();
            throw new PLSSCDException("The POSNET printer confirmed the e-receipt binding (eparagonidznext) but did not return the eDokument id (ha). The binding was cancelled and nothing was printed.");
        }
        return eDocumentId;
    }

    private async Task CancelEReceiptBindingAsync()
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.EparagonIdzCancel());
        }
        catch (PLDeviceErrorException)
        {
            // A confirmed rejection (e.g. nothing pending) leaves the device in a known state —
            // the missing-ha error stays the reported failure. Ambiguous or unreachable outcomes
            // propagate instead: whether a binding is still armed is then unknown and the
            // operator has to verify before the next sale.
        }
    }

    /// <summary>
    /// Best-effort readback of the eDokument buffer record, mirroring the scnt pattern: the
    /// document is already closed on the register, so a failing readback must not fail the
    /// receipt. The record says whether the document went electronic (pr = N, no paper) and how
    /// far the delivery to the hub has come (st).
    /// </summary>
    private async Task TryReadEDocumentDeliveryStateAsync(ReceiptResponse response, uint eDocumentId)
    {
        try
        {
            var record = await _client.ExecuteAsync(PosNetCommands.EparagonBufferGet(eDocumentId));
            var form = record.Parameters.TryGetValue("pr", out var printed) ? printed.Trim().ToUpperInvariant() switch
            {
                "N" or "0" => "electronic",
                "T" or "1" => "printed",
                _ => "unknown",
            } : "unknown";
            var deliveryState = record.Parameters.TryGetValue("st", out var status) && status.Trim().Length > 0
                ? $"{form} (st{status.Trim()})"
                : form;
            response.AddSignatureItem(SignatureTypePL.EDocumentDeliveryState, "Status eDokumentu", deliveryState);
        }
        catch (PLSSCDException)
        {
            // eparagonbufferget is read-only — swallowing an ambiguous or failed readback is safe.
        }
    }

    /// <summary>
    /// Adds the register identity to the response, reading the status once and reusing it. An
    /// unreachable or silent register propagates: no sale may be recorded without a register.
    /// </summary>
    private async Task EnrichWithDeviceIdentityAsync(ReceiptResponse response)
    {
        _identity ??= ToDeviceInfo(await _client.ExecuteAsync(PosNetCommands.Scomm()));
        response.EnrichWithDeviceIdentification(_identity);
    }

    private async Task CancelAsync()
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.Prncancel());
        }
        catch (PLDeviceErrorException)
        {
            // A confirmed rejection of the cancel (e.g. no open transaction) leaves the device
            // in a known state — the original error stays the reported failure. Ambiguous or
            // unreachable outcomes of the cancel itself must propagate instead: whether the
            // transaction is still open is then unknown and the operator has to verify.
        }
    }

    /// <summary>
    /// The fiscal document number is not part of the trend confirmation — it is read back from
    /// the counter status (scnt, bt = last receipt number). The document is already printed at
    /// this point, so a failing readback must not fail the receipt; the number is then simply
    /// absent from the response.
    /// </summary>
    private async Task TryReadFiscalDocumentNumberAsync(ReceiptResponse response)
    {
        try
        {
            var counters = await _client.ExecuteAsync(PosNetCommands.Scnt());
            if (counters.Parameters.TryGetValue("bt", out var lastReceiptNumber)
                && long.TryParse(lastReceiptNumber, out var fiscalDocumentNumber))
            {
                response.EnrichWithFiscalDocumentNumber(fiscalDocumentNumber);
            }
        }
        catch (PLSSCDException)
        {
            // scnt is read-only — swallowing an ambiguous or failed readback is safe.
        }
    }

    private PLDeviceInfo ToDeviceInfo(PosNetResponse status) => new()
    {
        FiscalizationState = ToFiscalizationState(status),
        VatRateTable = _configuration.VatRateTable,
        // scomm reports the numer unikatowy (nu), the number printed on every fiscal document. The
        // numer fabryczny is not part of this status — reading it is a follow-up to middleware#751.
        DeviceSerialNumber = null,
        UniqueDeviceNumber = status.Parameters.TryGetValue("nu", out var uniqueNumber) && uniqueNumber.Trim().Length > 0
            ? uniqueNumber.Trim()
            : null,
    };

    /// <summary>
    /// The scomm status flags are the letters T (tak) and N (nie) — a POSNET Online printer answers
    /// e.g. <c>fsN tzN ts0 hrT nuZBF 2101002392 tdN</c>. 1/0 is accepted as well, and an
    /// unrecognized or missing flag leaves the state <see cref="PLFiscalizationState.Unknown"/>:
    /// reporting a fiscalized register as non-fiscal would keep a PL queue from ever activating.
    /// </summary>
    private static PLFiscalizationState ToFiscalizationState(PosNetResponse status)
    {
        if (!status.Parameters.TryGetValue("fs", out var fiscalMode))
        {
            return PLFiscalizationState.Unknown;
        }
        return fiscalMode.Trim().ToUpperInvariant() switch
        {
            "T" or "1" => PLFiscalizationState.Fiscalized,
            "N" or "0" => PLFiscalizationState.NonFiscal,
            _ => PLFiscalizationState.Unknown,
        };
    }

    private static bool IsFiscalReceiptCase(ReceiptCase receiptCase)
        => receiptCase.IsType(ReceiptCaseType.Receipt)
            && (receiptCase.IsCase(ReceiptCase.UnknownReceipt0x0000)
                || receiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001)
                || receiptCase.IsCase(ReceiptCase.ECommerce0x0004));

    public void Dispose()
    {
        _client.Dispose();
        _deviceLock.Dispose();
    }
}
