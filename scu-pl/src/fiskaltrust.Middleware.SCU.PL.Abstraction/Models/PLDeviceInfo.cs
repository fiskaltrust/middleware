using System;
using System.Collections.Generic;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.pl;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

/// <summary>
/// The fiscalization state of the connected Polish register. Fiscalizing a device is a
/// certified-technician (serwis) act and can never be triggered through the middleware.
/// </summary>
public enum PLFiscalizationState
{
    Unknown = 0,
    NonFiscal = 1,
    Fiscalized = 2,
    ReadOnly = 3,
}

/// <summary>
/// A single slot of the PTU VAT rate table programmed on the register. A slot either carries a
/// percentage rate or is marked tax-exempt (zwolniona, "zw.").
/// </summary>
public class PLVatRateTableEntry
{
    /// <summary>The PTU slot letter ("A" to "G").</summary>
    public string PtuSlot { get; set; } = "";

    /// <summary>The VAT rate in percent (e.g. 23 for the standard rate), or null if the slot is exempt.</summary>
    public decimal? VatRatePercent { get; set; }

    /// <summary>True if the slot is programmed as tax-exempt (zwolniona, "zw.").</summary>
    public bool IsExempt { get; set; }
}

/// <summary>
/// Typed state of the Polish register as reported by the device. In Poland the certified register
/// owns the compliance state; this model travels as the json-serialized <c>InfoData</c> blob of
/// <see cref="PLSSCDInfo"/>. Software registers (kasy wirtualne) have no numer fabryczny — for them
/// <see cref="DeviceSerialNumber"/> stays null and <see cref="UniqueDeviceNumber"/> identifies the register.
/// </summary>
public class PLDeviceInfo
{
    /// <summary>The manufacturing serial number (numer fabryczny); null for software registers.</summary>
    public string? DeviceSerialNumber { get; set; }

    /// <summary>The unique number assigned from the MF pool (numer unikatowy), printed on every fiscal document.</summary>
    public string? UniqueDeviceNumber { get; set; }

    /// <summary>The registration number assigned during fiscalization (numer ewidencyjny), identifying the register in the CRK.</summary>
    public string? RegistrationNumber { get; set; }

    public PLFiscalizationState FiscalizationState { get; set; }

    /// <summary>The PTU VAT rate table (slots A–G) currently programmed on the register.</summary>
    public List<PLVatRateTableEntry> VatRateTable { get; set; } = new();

    /// <summary>Whether the register can currently reach the CRK (Central Repository of Cash Registers).</summary>
    public bool? CrkReachable { get; set; }

    /// <summary>The moment of the last successful transmission to the CRK, as reported by the register.</summary>
    public DateTime? CrkLastTransmission { get; set; }

    /// <summary>Whether the register supports issuing e-receipts (e-paragony).</summary>
    public bool? EReceiptCapable { get; set; }

    /// <summary>The number of the current daily (Z) report period.</summary>
    public int? CurrentZReportNumber { get; set; }

    public PLSSCDInfo ToPLSSCDInfo() => new()
    {
        InfoData = JsonSerializer.Serialize(this)
    };

    public static PLDeviceInfo? FromPLSSCDInfo(PLSSCDInfo info)
        => info.InfoData is null ? null : JsonSerializer.Deserialize<PLDeviceInfo>(info.InfoData);
}
