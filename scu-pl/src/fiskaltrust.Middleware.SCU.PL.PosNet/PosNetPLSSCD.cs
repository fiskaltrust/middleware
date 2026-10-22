using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
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
/// into the register's commands: the trinit → trline → trpayment → trend flow for a sale and the
/// readback that anchors it (<see cref="PosNetReceiptReadback"/>), the goods return printout for a
/// return, the daily report for a daily closing, the periodic reports for a monthly and a yearly
/// closing, and the status read behind the zero receipt. Non-fiscal forms and device setup are follow-ups.
/// </summary>
public class PosNetPLSSCD : IPLSSCD, IDisposable
{
    /// <summary>
    /// The zone a Polish fiscal register keeps its clock and its fiscal day in. A register is
    /// installed in Poland by law, so this is a property of the market, not of the deployment.
    /// </summary>
    private const string RegisterTimeZoneId = "Europe/Warsaw";

    /// <summary>
    /// The register's zone for reading its wall clock back after a receipt, resolved once. A host
    /// without zone data yields null rather than an exception: by the time the clock is read the
    /// receipt is printed, and a moment that cannot be told is left out instead of failing it.
    /// </summary>
    private static readonly TimeZoneInfo? s_registerZone = FindRegisterZone();

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

    /// <summary>
    /// Never throws: every failure comes back as an error response (<see cref="PLReceiptResponseExtensions.SetError"/>),
    /// so a queue reads the outcome off the response alone — also when this SCU runs remotely, where
    /// exception types do not survive the transport.
    /// </summary>
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            return await ProcessReceiptOnDeviceAsync(request);
        }
        catch (Exception exception)
        {
            request.ReceiptResponse.SetError(exception);
            return new ProcessResponse { ReceiptResponse = request.ReceiptResponse };
        }
    }

    private async Task<ProcessResponse> ProcessReceiptOnDeviceAsync(ProcessRequest request)
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
            // Deliberately outside the device lock: the zero receipt is how a POS sees why a sale is
            // waiting (a receipt held by an open cover), and that sale holds the lock until the
            // register has decided. The asynchronous reads it uses cannot disturb a running sequence.
            await ReportPrinterStateAsync(response);
        }
        else if (receiptCase.IsCase(ReceiptCase.DailyClosing0x2011))
        {
            await _deviceLock.WaitAsync();
            try
            {
                await ExecuteDailyClosingAsync(request.ReceiptRequest, response);
            }
            finally
            {
                _deviceLock.Release();
            }
        }
        else if (receiptCase.IsCase(ReceiptCase.MonthlyClosing0x2012))
        {
            await ExecutePeriodicReportAsync(response, PosNetCommands.Monthlyrep(ToRegisterDate(request.ReceiptRequest.cbReceiptMoment)));
        }
        else if (receiptCase.IsCase(ReceiptCase.YearlyClosing0x2013))
        {
            // The register has no yearly report: the year is a periodic report from 1 January up to
            // the day being closed, so a closing sent on 31 December covers the whole year.
            var day = ToRegisterDate(request.ReceiptRequest.cbReceiptMoment);
            await ExecutePeriodicReportAsync(response, PosNetCommands.Periodicrepbydates(new DateOnly(day.Year, 1, 1), day));
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
        var paymentSlips = PosNetPaymentSlip.Read(request);
        var commands = PosNetReceiptMapper.MapSale(request, await GetPtuSlotResolverAsync());

        // The numer unikatowy is a legal element of the fiscal document, so the response carries
        // it like the InMemory SCU does. Reading it before trinit keeps the order safe: a register
        // that cannot answer its status has not been asked to open a transaction either. The same
        // holds for the receipt counter the readback is checked against.
        PosNetReceiptCounters countersBefore;
        uint? eDocumentId;
        try
        {
            await EnrichWithDeviceIdentityAsync(response);
            countersBefore = await PosNetReceiptReadback.ReadCountersAsync(_client);

            // A register that is not idle — a fault it already knows about, an operator prompt, an
            // earlier receipt still waiting in its queue — would hold this receipt too and fiscalize it
            // whenever the fault is cleared, long after the caller was told the sale failed.
            await RequirePrinterReadyAsync("before the receipt was opened");

        // The e-paragon binding goes out strictly before trinit: eparagonidznext binds the *next*
        // document, and a failed binding fails the sale while nothing has been printed yet. A
        // confirmed error needs no cleanup (no transaction is open); an ambiguous outcome
        // propagates without retry like every other command.
        // Footer codes (ftReceiptCaseData.PL.printout barcode/qrCode) are a printout configuration
        // for the receipt that follows. They go out before the e-receipt binding: a rejected
        // configuration then fails the sale with nothing armed and nothing printed.
            if (printout is { HasFooterConfiguration: true })
            {
                await ConfigureFooterCodesAsync(printout);
            }

            eDocumentId = eReceiptCustomerId is null ? null : await BindEReceiptAsync(eReceiptCustomerId);
        }
        catch (PLDeviceOutcomeUnknownException exception)
        {
            // Nothing that can print has been sent yet (no trinit), so an unanswered command here —
            // e.g. a late answer to an earlier command arriving in its place — cannot have issued a
            // receipt: the outcome of the sale is known even though the command's is not.
            throw new PosNetReceiptNotIssuedException($"the register did not answer before the receipt was opened: {exception.Message}", exception);
        }

        // Everything up to and including trend is the fiscal transaction; what follows (trftrln …
        // trftrend) is the additional-lines phase of an already closed receipt.
        var trendIndex = IndexOf(commands, "trend");
        var executed = 0;

        // Phase 1 — the open transaction, up to the last command before trend. A receipt only becomes
        // fiscal when trend closes it, so whatever fails here, the receipt was not issued: the open
        // transaction is canceled where the register still listens, and a transaction the register
        // could not be told to cancel stays open without a trend — it can never close itself.
        try
        {
            for (; executed < trendIndex; executed++)
            {
                await _client.ExecuteAsync(commands[executed]);
            }
            // The cover can be opened while the receipt is being built: checked again right before
            // the one command that fiscalizes, for the same reason as before trinit.
            if (trendIndex < commands.Count)
            {
                await RequirePrinterReadyAsync("before the receipt was closed");
            }
        }
        catch (Exception exception)
        {
            if (executed > 0)
            {
                await TryCancelAsync();
            }
            throw exception as PosNetReceiptNotIssuedException
                ?? new PosNetReceiptNotIssuedException($"the sale failed before the receipt was closed: {exception.Message}", exception);
        }

        // Phase 2 — trend. A confirmed rejection means the register did not close the receipt; any
        // other failure means it may have, possibly only later (the receipt waits in the printer until
        // it can print), so the outcome is read off the register's counters before anything is reported.
        if (trendIndex < commands.Count)
        {
            try
            {
                await _client.ExecuteAsync(commands[trendIndex]);
            }
            catch (PLDeviceErrorException exception)
            {
                await TryCancelAsync();
                throw new PosNetReceiptNotIssuedException($"the register refused to close the receipt: {exception.Message}", exception);
            }
            catch (Exception exception)
            {
                await ResolveSilentTrendAsync(countersBefore, exception);
            }
            executed = trendIndex + 1;
        }

        // Phase 3 — the additional lines of the closed, fiscal receipt.
        try
        {
            for (; executed < commands.Count; executed++)
            {
                await _client.ExecuteAsync(commands[executed]);
            }
        }
        catch (Exception exception)
        {
            // The receipt is already fiscal — no failure of an additional line may fail it, be it
            // a rejection, an ambiguous answer, a lost connection or one nobody anticipated: the queue would otherwise
            // record a failure for a document the register has issued, and a resend would print a
            // second fiscal receipt. The footer is closed where possible and the failure travels
            // in the response instead.
            await TryEndFooterAsync();
            response.AddSignatureItem(SignatureTypePL.AdditionalPrintoutNotPrinted, "Dodatkowe linie nie wydrukowane",
                exception is PLDeviceErrorException deviceError ? $"?{deviceError.ErrorCode}: {deviceError.Message}" : exception.Message);
        }

        var anchor = await PosNetReceiptReadback.ReadAsync(_client, countersBefore, s_registerZone);
        if (anchor is not null)
        {
            EnrichWithReceiptAnchor(response, anchor);
        }

        if (eDocumentId is { } documentId)
        {
            response.EnrichWithEDocumentId(documentId);
            await TryReadEDocumentDeliveryStateAsync(response, documentId);
        }

        foreach (var slip in paymentSlips)
        {
            await PrintPaymentSlipAsync(slip, response);
        }
    }

    /// <summary>
    /// Prints a payment slip as a non-fiscal super-form after the receipt. The receipt is already
    /// fiscal, so a rejected slip must not fail it: the printout is cancelled and the rejection
    /// travels in the response, as a rejected footer line does.
    /// </summary>
    private async Task PrintPaymentSlipAsync(PosNetPaymentSlip slip, ReceiptResponse response)
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.Formstart(PosNetPaymentSlip.SuperForm, PosNetPaymentSlip.CardPaymentHeader));
            foreach (var line in slip.Lines)
            {
                await _client.ExecuteAsync(PosNetCommands.Formformattedline(PosNetPaymentSlip.SuperForm, PosNetPaymentSlip.ToFormLine(line)));
            }
            await _client.ExecuteAsync(PosNetCommands.Formend(PosNetPaymentSlip.SuperForm));
        }
        catch (Exception exception)
        {
            await TryCancelAsync();
            response.AddSignatureItem(SignatureTypePL.AdditionalPrintoutNotPrinted, "Potwierdzenie płatności nie wydrukowane",
                exception is PLDeviceErrorException deviceError ? $"?{deviceError.ErrorCode}: {deviceError.Message}" : exception.Message);
        }
    }

    /// <summary>
    /// Signs what the register contributed to the receipt. The receipt number is the one printed
    /// on the document, but it is no key on its own; the daily report, the device moment and the
    /// protected-memory document number make it one (<see cref="PosNetReceiptAnchor"/>).
    /// </summary>
    private static void EnrichWithReceiptAnchor(ReceiptResponse response, PosNetReceiptAnchor anchor)
    {
        response.EnrichWithFiscalDocumentNumber(anchor.ReceiptNumber);
        if (anchor.DailyReportNumber is { } dailyReportNumber)
        {
            response.AddSignatureItem(SignatureTypePL.CurrentDailyReportNumber, PLReceiptCases.CurrentDailyReportNumberCaption, dailyReportNumber.ToString(CultureInfo.InvariantCulture));
        }
        if (anchor.DocumentNumber is { } documentNumber)
        {
            response.AddSignatureItem(SignatureTypePL.ProtectedMemoryDocumentNumber, PLReceiptCases.ProtectedMemoryDocumentNumberCaption, documentNumber.ToString(CultureInfo.InvariantCulture));
        }
        if (anchor.DeviceMoment is { } deviceMoment)
        {
            response.AddSignatureItem(SignatureTypePL.DeviceMoment, PLReceiptCases.DeviceMomentCaption, deviceMoment);
        }
    }

    /// <summary>
    /// Sends the printout configuration for the next receipt: the prepared 2D code (qrcode) and the
    /// footer configuration (ftrcfg: cashier from cbUser, register from cbTerminalID, the codes and the
    /// info lines). All of it is valid until the next printout, i.e. for the receipt this sale prints.
    /// Runs before the e-receipt binding and before trinit, so a rejection fails the sale with nothing
    /// on the device.
    /// </summary>
    private async Task ConfigureFooterCodesAsync(PosNetPrintout printout)
    {
        if (printout.QrCode is { } qrCode)
        {
            await _client.ExecuteAsync(PosNetCommands.Qrcode(qrCode.Data, qrCode.PixelSize, qrCode.ErrorCorrection));
        }
        await _client.ExecuteAsync(PosNetCommands.Ftrcfg(printout.Barcode, printout.QrCode is { } code ? (int)code.Position : 0,
            printout.Cashier, printout.Register, printout.FormattedInfoLines));
    }

    /// <summary>
    /// Closes the additional-lines phase after a failed line so the printout is finished. It runs
    /// after trend, so every failure of the close itself is swallowed (the original failure is
    /// reported): trftrend cannot print a fiscal document, so sending it after an ambiguous line is
    /// safe, and a footer left open is for the operator to see — not a reason to fail a fiscal receipt.
    /// </summary>
    private async Task TryEndFooterAsync()
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.Trftrend());
        }
        catch (Exception)
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
        catch (Exception)
        {
            // eparagonbufferget is read-only and runs after trend — swallowing any failed readback
            // is safe, failing the receipt is not.
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
    /// <remarks>
    /// The daily report is fiscal like a receipt — it writes the day into fiscal memory — and a POSNET
    /// holds it like a receipt: sent to a printer that cannot print, it is printed and written the
    /// moment the fault is cleared. So it gets the same guarantee: refused unless the register is idle,
    /// and an unanswered dailyrep is settled from the register's counters once it is idle again
    /// (<see cref="ResolveSilentDailyReportAsync"/>), however long that takes.
    /// </remarks>
    private async Task ExecuteDailyClosingAsync(ReceiptRequest request, ReceiptResponse response)
    {
        PosNetResponse countersBefore;
        try
        {
            await EnrichWithDeviceIdentityAsync(response);
            countersBefore = await _client.ExecuteAsync(PosNetCommands.Scnt());
            await RequirePrinterReadyAsync("before the daily report", "daily report");
        }
        catch (PLDeviceOutcomeUnknownException exception)
        {
            throw new PosNetReceiptNotIssuedException($"the register did not answer before the daily report was requested: {exception.Message}", exception, "daily report");
        }

        try
        {
            await _client.ExecuteAsync(PosNetCommands.Dailyrep(ToRegisterDate(request.cbReceiptMoment)));
        }
        catch (PLDeviceErrorException)
        {
            // A confirmed refusal (wrong day, a second zero report in fiscal mode): nothing was printed.
            throw;
        }
        catch (Exception exception)
        {
            await ResolveSilentDailyReportAsync(countersBefore, exception);
        }

        var reportsDone = await PosNetStatus.TryReadAsync(_client, PosNetCommands.Scnt(), counters => PosNetStatus.ReadNumber(counters, "rd"));
        if (reportsDone is { } number)
        {
            response.AddSignatureItem(SignatureTypePL.ZReportNumber, PLReceiptCases.ZReportNumberCaption, number.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Prints a periodic report for a monthly or yearly closing. The period is taken from the receipt
    /// moment in the register's zone, as for the daily closing. A periodic report only reads fiscal
    /// memory, so no report number is signed: unlike the daily report it advances no counter.
    /// </summary>
    private async Task ExecutePeriodicReportAsync(ReceiptResponse response, PosNetCommand report)
    {
        await _deviceLock.WaitAsync();
        try
        {
            await EnrichWithDeviceIdentityAsync(response);
            await _client.ExecuteAsync(report);
        }
        finally
        {
            _deviceLock.Release();
        }
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

    /// <summary>
    /// Refuses the sale unless the register is idle: mechanism ready, no operator prompt, every earlier
    /// command executed (<see cref="PosNetPrinterState"/>). The check runs before trend, so refusing is
    /// final — the receipt has not been issued. It cannot see everything: a POSNET only notices an
    /// open cover once it tries to print, which is why a silent trend is still settled from the
    /// register's counters (<see cref="ResolveSilentTrendAsync"/>).
    /// </summary>
    private async Task RequirePrinterReadyAsync(string stage, string document = "receipt")
    {
        var state = await PosNetPrinterState.ReadAsync(_client);
        if (state.DescribeProblem() is { } problem)
        {
            throw new PosNetReceiptNotIssuedException($"the printer is not ready {stage}: {problem}.", document: document);
        }
    }

    /// <summary>
    /// The zero receipt's answer: the printer's live state in <c>ftStateData</c> (<c>PL.PrinterStatus</c>),
    /// like the Italian RT printer SCUs return theirs. A register that is not ready does not fail the
    /// zero receipt — the state is the answer; only a register that does not answer at all does.
    /// Fiscal mode and read-only mode come from scomm, a synchronous read, which is only sent to an
    /// idle register: behind a stuck command it would wait for that command.
    /// </summary>
    private async Task ReportPrinterStateAsync(ReceiptResponse response)
    {
        var state = await PosNetPrinterState.ReadAsync(_client);
        if (!state.Answered)
        {
            throw new PLDeviceUnreachableException("The POSNET printer did not answer its status (!sprn, !sdev).");
        }

        var status = state.IsIdle ? await PosNetStatus.TryReadAsync(_client, PosNetCommands.Scomm(), answer => answer) : null;
        response.ftStateData = JsonSerializer.SerializeToElement(new
        {
            PL = new
            {
                PrinterStatus = new
                {
                    Ready = state.IsIdle,
                    Problem = state.DescribeProblem(),
                    state.Mechanism,
                    MechanismText = PosNetPrinterState.DescribeMechanism(state.Mechanism),
                    state.DeviceStatus,
                    DeviceStatusText = PosNetPrinterState.DescribeDeviceStatus(state.DeviceStatus),
                    state.QueueEmpty,
                    state.PaperNearEnd,
                    TransactionOpen = state.TransactionResources is null ? (bool?)null : state.TransactionResources != 0,
                    FiscalMode = status is null ? (bool?)null : PosNetStatus.ReadFlag(status, "fs"),
                    ReadOnlyMode = status is null ? (bool?)null : PosNetStatus.ReadFlag(status, "td"),
                    UniqueDeviceNumber = status is not null && status.Parameters.TryGetValue("nu", out var nu) && nu.Trim().Length > 0 ? nu.Trim() : null,
                },
            },
        });
    }

    /// <summary>
    /// Settles a trend that was sent but not confirmed. A POSNET accepts trend even when it cannot
    /// print — cover open, no paper — holds the closed receipt and prints and fiscalizes it the moment
    /// the fault is cleared, however long that takes; no command withdraws it. So the SCU never answers
    /// before the register has: it watches the asynchronous state until the register is idle again,
    /// then its completed-receipt counter (<c>scnt bn</c>) says for certain whether the receipt exists.
    /// Returns when it does, so the sale completes like any other; throws
    /// <see cref="PosNetReceiptNotIssuedException"/> when it does not. There is deliberately no time
    /// limit: any answer given while the register still holds the receipt could be wrong, and an error
    /// for a receipt that is printed later is a duplicate waiting to happen. While it waits, the zero
    /// receipt reports what the register is waiting for.
    /// </summary>
    private async Task ResolveSilentTrendAsync(PosNetReceiptCounters before, Exception trendFailure)
    {
        var counters = await WaitUntilIdleAsync();
        if (PosNetStatus.ReadNumber(counters, "bn") is { } completedNow && before.CompletedReceipts is { } completedBefore)
        {
            if (completedNow > completedBefore)
            {
                return;
            }
            await TryCancelAsync();
            throw new PosNetReceiptNotIssuedException(
                $"the register did not confirm closing the receipt ({trendFailure.Message}), and once it was idle again its counters showed that no receipt was completed.", trendFailure);
        }
        // Without a baseline the counters cannot tell; the register itself is idle, so the receipt is
        // either done or not coming — the one case left for the operator.
        throw new PLDeviceOutcomeUnknownException(
            $"The register did not confirm closing the receipt ({trendFailure.Message}) and does not report a completed-receipt counter to settle it with.", trendFailure);
    }

    /// <summary>
    /// Settles a dailyrep that was sent but not confirmed, like <see cref="ResolveSilentTrendAsync"/>
    /// settles a receipt: once the register is idle again, a daily report it printed shows in its
    /// counters — <c>rd</c> (daily reports in fiscal memory) on a fiscalized register, <c>hn</c> (every
    /// printout, the only one that moves on a register in training mode). Measured on a POSNET: a
    /// daily report advances both, e.g. <c>rd3 hn54</c> → <c>rd4 hn55</c>.
    /// </summary>
    private async Task ResolveSilentDailyReportAsync(PosNetResponse before, Exception reportFailure)
    {
        var after = await WaitUntilIdleAsync();
        var known = false;
        foreach (var counter in new[] { "rd", "hn" })
        {
            if (PosNetStatus.ReadNumber(before, counter) is { } then && PosNetStatus.ReadNumber(after, counter) is { } now)
            {
                if (now > then)
                {
                    return;
                }
                known = true;
            }
        }
        if (known)
        {
            throw new PosNetReceiptNotIssuedException(
                $"the register did not confirm the daily report ({reportFailure.Message}), and once it was idle again its counters showed that no report was printed.", reportFailure, "daily report");
        }
        throw new PLDeviceOutcomeUnknownException(
            $"The register did not confirm the daily report ({reportFailure.Message}) and does not report the counters to settle it with.", reportFailure);
    }

    /// <summary>
    /// Waits — without a time limit, see <see cref="ResolveSilentTrendAsync"/> — until the register is
    /// idle (<see cref="PosNetPrinterState.IsIdle"/>: nothing held, nothing queued) and its counters can
    /// be read, and returns them. A register answers a synchronous read only after everything sent
    /// before it, so counters read while it is idle are final.
    /// </summary>
    private async Task<PosNetResponse> WaitUntilIdleAsync()
    {
        while (true)
        {
            var state = await PosNetPrinterState.ReadAsync(_client);
            if (state.IsIdle && await PosNetStatus.TryReadAsync(_client, PosNetCommands.Scnt(), status => status) is { } counters)
            {
                return counters;
            }
            await Task.Delay(_configuration.OutcomeResolutionPollIntervalMs);
        }
    }

    /// <summary>
    /// Cancels the open transaction where the register still listens. Never throws: it only runs on
    /// the way to reporting a failure, before trend or after the register's counters showed that
    /// trend issued nothing — a transaction it cannot cancel stays open without a trend, and an open
    /// transaction never becomes a fiscal receipt on its own.
    /// </summary>
    private async Task TryCancelAsync()
    {
        try
        {
            await _client.ExecuteAsync(PosNetCommands.Prncancel());
        }
        catch (Exception)
        {
        }
    }

    private static TimeZoneInfo? FindRegisterZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(RegisterTimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
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
