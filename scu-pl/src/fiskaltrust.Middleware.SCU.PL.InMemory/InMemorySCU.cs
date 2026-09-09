using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

namespace fiskaltrust.Middleware.SCU.PL.InMemory;

/// <summary>
/// A deterministic, hardware-free IPLSSCD implementation that mimics the behavior of a Polish
/// online register: it owns the fiscal document numbering and the daily (Z) report counter and
/// enriches responses with the device identity — enough to run QueuePL acceptance tests without
/// a printer. Validation hooks let tests inject device behaviors (e.g. tax-class blocking).
/// </summary>
public class InMemorySCU : IPLSSCD
{
    private readonly PLDeviceInfo _deviceInfo;
    private readonly List<Action<ProcessRequest>> _validators;
    private readonly object _syncRoot = new();
    private long _fiscalDocumentNumber;

    public InMemorySCU(PLDeviceInfo? deviceInfo = null, IEnumerable<Action<ProcessRequest>>? validators = null, long fiscalDocumentNumberSeed = 0)
    {
        _deviceInfo = deviceInfo ?? CreateDefaultDeviceInfo();
        _deviceInfo.CurrentZReportNumber ??= 0;
        _validators = validators?.ToList() ?? new List<Action<ProcessRequest>>();
        _fiscalDocumentNumber = fiscalDocumentNumberSeed;
    }

    public static PLDeviceInfo CreateDefaultDeviceInfo() => new()
    {
        DeviceSerialNumber = "INM1234567",
        UniqueDeviceNumber = "ZAS0000000001",
        RegistrationNumber = "0000000001",
        FiscalizationState = PLFiscalizationState.Fiscalized,
        VatRateTable = new List<PLVatRateTableEntry>
        {
            new() { PtuSlot = "A", VatRatePercent = 23m },
            new() { PtuSlot = "B", VatRatePercent = 8m },
            new() { PtuSlot = "C", VatRatePercent = 5m },
            new() { PtuSlot = "D", VatRatePercent = 0m },
            new() { PtuSlot = "G", IsExempt = true },
        },
        CrkReachable = true,
        CrkLastTransmission = null,
        EReceiptCapable = true,
        CurrentZReportNumber = 0,
    };

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
        => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<PLSSCDInfo> GetInfoAsync()
        => Task.FromResult(_deviceInfo.ToPLSSCDInfo());

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        foreach (var validator in _validators)
        {
            validator(request);
        }

        var receiptCase = request.ReceiptRequest.ftReceiptCase;
        var response = request.ReceiptResponse;

        if (receiptCase.IsType(ReceiptCaseType.Invoice))
        {
            throw new PLValidationException(PLReceiptCases.InvoiceCaseRefusal);
        }

        response.EnrichWithDeviceIdentification(_deviceInfo);

        if (receiptCase.IsFiscalReceipt())
        {
            long fiscalDocumentNumber;
            lock (_syncRoot)
            {
                fiscalDocumentNumber = ++_fiscalDocumentNumber;
            }
            response.EnrichWithFiscalDocumentNumber(fiscalDocumentNumber);
        }
        else if (receiptCase.IsCase(ReceiptCase.DailyClosing0x2011))
        {
            // Only the daily closing advances the Z counter — monthly/yearly closings are
            // periodic reports over already-closed days and do not create a new Z report.
            int zReportNumber;
            lock (_syncRoot)
            {
                zReportNumber = (_deviceInfo.CurrentZReportNumber ?? 0) + 1;
                _deviceInfo.CurrentZReportNumber = zReportNumber;
            }
            response.AddSignatureItem(Abstraction.Cases.SignatureTypePL.ZReportNumber, PLReceiptCases.ZReportNumberCaption, zReportNumber.ToString(CultureInfo.InvariantCulture));
        }

        return Task.FromResult(new ProcessResponse { ReceiptResponse = response });
    }
}
