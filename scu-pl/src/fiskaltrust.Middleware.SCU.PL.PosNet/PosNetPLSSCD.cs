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
            await ExecuteSaleAsync(request.ReceiptRequest, response);
        }
        else if (receiptCase.IsCase(ReceiptCase.ZeroReceipt0x2000))
        {
            // The zero receipt is the operator's connectivity/state probe: one status read must
            // succeed. The printer state itself is returned via GetInfoAsync.
            await _client.ExecuteAsync(PosNetCommands.Scomm());
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
        // The slots come from the register's table, so that is the one read the mapping needs
        // before it can refuse a receipt — everything else is still validated before the first
        // transaction command goes out.
        var commands = PosNetReceiptMapper.MapSale(request, await GetPtuSlotResolverAsync());

        // The numer unikatowy is a legal element of the fiscal document, so the response carries
        // it like the InMemory SCU does. Reading it before trinit keeps the order safe: a register
        // that cannot answer its status has not been asked to open a transaction either.
        await EnrichWithDeviceIdentityAsync(response);

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

    public void Dispose() => _client.Dispose();
}
