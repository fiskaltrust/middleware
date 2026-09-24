using System;
using System.Collections.Generic;
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
/// IPLSSCD implementation driving a POSNET Online fiscal printer — the certified register owns
/// numbering, the PTU table, reports and the CRK transmission; this SCU translates receipt cases
/// into the register's commands: the trinit → trline → trpayment → trend flow for a sale, the goods
/// return printout for a return, the daily report for a daily closing, and the status read behind
/// the zero receipt. Periodic reports, non-fiscal forms and device setup are follow-ups.
/// </summary>
public class PosNetPLSSCD : IPLSSCD, IDisposable
{
    /// <summary>
    /// The zone a Polish fiscal register keeps its clock and its fiscal day in. A register is
    /// installed in Poland by law, so this is a property of the market, not of the deployment.
    /// </summary>
    private const string RegisterTimeZoneId = "Europe/Warsaw";

    private readonly PosNetClient _client;
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

    /// <summary>
    /// The PTU table, read once per SCU instance as well: it changes only through a service act
    /// that goes into fiscal memory, never between two receipts of a running SCU.
    /// </summary>
    private List<PLVatRateTableEntry>? _rateTable;
    private PtuSlotResolver? _ptuSlotResolver;

    public PosNetPLSSCD(PosNetConfiguration configuration)
        : this(configuration, new PosNetClient(PosNetTransportFactory.Create(configuration))) { }

    public PosNetPLSSCD(PosNetConfiguration configuration, PosNetClient client)
    {
        _configuration = configuration;
        _client = client;
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public async Task<PLSSCDInfo> GetInfoAsync()
    {
        var status = await _client.ExecuteAsync(PosNetCommands.Scomm());
        var rateTable = await GetRateTableAsync();
        return ToDeviceInfo(status, rateTable).ToPLSSCDInfo();
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase;
        var response = request.ReceiptResponse;

        if (receiptCase.IsType(ReceiptCaseType.Invoice))
        {
            throw new PLValidationException(PLReceiptCases.InvoiceCaseRefusal);
        }

        if (receiptCase.IsFiscalReceipt() && receiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            await ExecuteReturnAsync(request.ReceiptRequest, response);
        }
        else if (receiptCase.IsFiscalReceipt())
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
        else if (receiptCase.IsCase(ReceiptCase.DailyClosing0x2011))
        {
            await ExecuteDailyClosingAsync(request.ReceiptRequest, response);
        }
        else if (receiptCase.IsCase(ReceiptCase.MonthlyClosing0x2012) || receiptCase.IsCase(ReceiptCase.YearlyClosing0x2013))
        {
            throw new PLValidationException("Periodic reports (monthly/yearly closing) are not supported by the PosNet SCU yet — the daily report is (0x2011).");
        }

        // Non-fiscal receipt cases pass through without device interaction — like the InMemory
        // SCU, only fiscal documents talk to the register.
        return new ProcessResponse { ReceiptResponse = response };
    }

    private async Task ExecuteSaleAsync(ReceiptRequest request, ReceiptResponse response)
    {
        // Both validations run before any frame is sent: a rejected IDZ or an unmappable sale must
        // not leave anything on the device. The PTU slots come from the register's table, so that
        // is the one read the mapping needs before it can refuse a receipt.
        var eReceiptCustomerId = PosNetReceiptMapper.GetEReceiptCustomerId(request);
        var printout = PosNetPrintoutReader.Read(request);
        var commands = PosNetReceiptMapper.MapSale(request, await GetPtuSlotResolverAsync());

        // The numer unikatowy is a legal element of the fiscal document, so the response carries
        // it like the InMemory SCU does. Reading it before trinit keeps the order safe: a register
        // that cannot answer its status has not been asked to open a transaction either.
        await EnrichWithDeviceIdentityAsync(response);

        // The e-paragon binding goes out strictly before trinit: eparagonidznext binds the *next*
        // document, and a failed binding fails the sale while nothing has been printed yet. A
        // confirmed error needs no cleanup (no transaction is open); an ambiguous outcome
        // propagates without retry like every other command.
        // Footer codes (ftReceiptCaseData.PL.printout barcode/qrCode) are a printout configuration
        // for the receipt that follows. They go out before the e-receipt binding: a rejected
        // configuration then fails the sale with nothing armed and nothing printed.
        if (printout is { HasFooterCodes: true })
        {
            await ConfigureFooterCodesAsync(printout);
        }

        var eDocumentId = eReceiptCustomerId is null ? (uint?)null : await BindEReceiptAsync(eReceiptCustomerId);

        // Everything up to and including trend is the fiscal transaction; what follows (trftrln …
        // trftrend) is the additional-lines phase of an already closed receipt.
        var trendIndex = IndexOf(commands, "trend");
        var executed = 0;
        try
        {
            foreach (var command in commands)
            {
                await _client.ExecuteAsync(command);
                executed++;
            }
        }
        catch (PLDeviceErrorException exception) when (executed > trendIndex)
        {
            // The receipt is already fiscal — a rejected additional line must not fail it (the
            // queue would otherwise record a failure for a document the register has issued). The
            // footer is closed and the rejection travels in the response instead.
            await TryEndFooterAsync();
            response.AddSignatureItem(SignatureTypePL.AdditionalPrintoutNotPrinted, "Dodatkowe linie nie wydrukowane",
                $"?{exception.ErrorCode}: {exception.Message}");
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
    /// Sends the footer codes for the next receipt: the prepared 2D code (qrcode) and the footer
    /// configuration (ftrcfg bc/bb). Both are valid until the next printout, i.e. for the receipt
    /// this sale prints. Runs before the e-receipt binding and before trinit, so a rejection fails
    /// the sale with nothing on the device.
    /// </summary>
    private async Task ConfigureFooterCodesAsync(PosNetPrintout printout)
    {
        if (printout.QrCode is { } qrCode)
        {
            await _client.ExecuteAsync(PosNetCommands.Qrcode(qrCode.Data, qrCode.PixelSize, qrCode.ErrorCorrection));
        }
        await _client.ExecuteAsync(PosNetCommands.Ftrcfg(printout.Barcode, printout.QrCode is { } code ? (int)code.Position : 0));
    }

    /// <summary>
    /// Closes the additional-lines phase after a rejected line so the printout is finished. A
    /// confirmed rejection of the close itself is swallowed (the original error is reported);
    /// ambiguous or unreachable outcomes propagate — the printout state is then unknown.
    /// </summary>
    private async Task TryEndFooterAsync()
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.Trftrend());
        }
        catch (PLDeviceErrorException)
        {
        }
    }

    private static int IndexOf(IReadOnlyList<PosNetCommand> commands, string mnemonic)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            if (commands[i].Mnemonic == mnemonic)
            {
                return i;
            }
        }
        return commands.Count;
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
    /// A return is one non-fiscal printout of the amount handed back (<see cref="PosNetReturnMapper"/>).
    /// It gets no fiscal document number; the response says what was printed instead.
    /// </summary>
    private async Task ExecuteReturnAsync(ReceiptRequest request, ReceiptResponse response)
    {
        var (amountGrosze, command) = PosNetReturnMapper.MapReturn(request);
        await EnrichWithDeviceIdentityAsync(response);
        await _client.ExecuteAsync(command);
        response.AddSignatureItem(SignatureTypePL.NonFiscalPrintout, "Zwrot towaru (wydruk niefiskalny)", amountGrosze.GroszeToPlnText());
    }

    /// <summary>
    /// The daily (Z) report closes the register's day; its number is read back from the counters
    /// afterwards like the fiscal document number of a sale. The register validates the date
    /// against its own clock, so the day being closed is derived from the receipt moment in the
    /// register's zone (<see cref="RegisterTimeZoneId"/>) — never from the host's local date: a
    /// middleware host in UTC would otherwise close the wrong day for every closing between
    /// midnight and the zone's offset.
    /// </summary>
    private async Task ExecuteDailyClosingAsync(ReceiptRequest request, ReceiptResponse response)
    {
        await EnrichWithDeviceIdentityAsync(response);
        await _client.ExecuteAsync(PosNetCommands.Dailyrep(ToRegisterDate(request.cbReceiptMoment)));
        await TryReadCounterAsync("rd", number => response.AddSignatureItem(SignatureTypePL.ZReportNumber, PLReceiptCases.ZReportNumberCaption, number.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// A Polish register runs on Polish local time, and every receipt moment is UTC by contract,
    /// so the calendar day the register knows is the moment converted to Warsaw time.
    /// </summary>
    private static DateOnly ToRegisterDate(DateTime receiptMoment)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            new DateTimeOffset(DateTime.SpecifyKind(receiptMoment, DateTimeKind.Utc), TimeSpan.Zero),
            RegisterTimeZoneId).DateTime);

    /// <summary>
    /// Adds the register identity to the response, reading the status once and reusing it. An
    /// unreachable or silent register propagates: no sale may be recorded without a register.
    /// Only the numer unikatowy and the serial number reach the response, and scomm carries both,
    /// so the PTU table is deliberately left out of this read: a goods return and a daily closing
    /// resolve no PTU slot and must not fail on a table they never use.
    /// </summary>
    private async Task EnrichWithDeviceIdentityAsync(ReceiptResponse response)
    {
        _identity ??= ToDeviceInfo(await _client.ExecuteAsync(PosNetCommands.Scomm()), rateTable: []);
        response.EnrichWithDeviceIdentification(_identity);
    }

    private async Task<PtuSlotResolver> GetPtuSlotResolverAsync()
        => _ptuSlotResolver ??= new PtuSlotResolver(await GetRateTableAsync());

    /// <summary>
    /// The PTU table: pinned by configuration where one is configured, otherwise read off the
    /// register's fiscal memory status. Never guessed — a slot the device does not have would be
    /// refused with 2000, a rate on the wrong slot would be printed and totalized under it.
    /// </summary>
    private async Task<List<PLVatRateTableEntry>> GetRateTableAsync()
    {
        if (_rateTable is not null)
        {
            return _rateTable;
        }
        if (_configuration.VatRateTable is { Count: > 0 } configured)
        {
            return _rateTable = configured;
        }
        return _rateTable = PosNetRateTable.Parse(await _client.ExecuteAsync(PosNetCommands.Sfsk()));
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
    private Task TryReadFiscalDocumentNumberAsync(ReceiptResponse response)
        => TryReadCounterAsync("bt", response.EnrichWithFiscalDocumentNumber);

    private async Task TryReadCounterAsync(string counter, Action<long> report)
    {
        try
        {
            var counters = await _client.ExecuteAsync(PosNetCommands.Scnt());
            if (counters.Parameters.TryGetValue(counter, out var text)
                && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                report(number);
            }
        }
        catch (PLSSCDException)
        {
            // scnt is read-only — swallowing an ambiguous or failed readback is safe.
        }
    }

    private static PLDeviceInfo ToDeviceInfo(PosNetResponse status, List<PLVatRateTableEntry> rateTable) => new()
    {
        FiscalizationState = ToFiscalizationState(status),
        VatRateTable = rateTable,
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

    public void Dispose()
    {
        _client.Dispose();
        _deviceLock.Dispose();
    }
}
