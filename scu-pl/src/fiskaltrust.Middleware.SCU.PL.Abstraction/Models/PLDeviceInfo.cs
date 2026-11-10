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
/// <see cref="PLSSCDInfo"/>.
/// <para>
/// A register carries three numbers that behave differently. On a hardware register the numer
/// unikatowy belongs to the exchangeable fiscal memory and changes when that is exchanged; the numer
/// fabryczny and the numer ewidencyjny stay the same. On a software register (kasa wirtualna) a numer
/// fabryczny, where it has one, is identical to the numer unikatowy (Dz.U. 2025 poz. 861, Załącznik
/// nr 1 §8 pkt 11). Each field is null where the register does not report it.
/// </para>
/// </summary>
public class PLDeviceInfo
{
    /// <summary>
    /// The manufacturing serial number (numer fabryczny). It stays with the device when the fiscal
    /// memory is exchanged; on a software register that has one, it equals <see cref="UniqueDeviceNumber"/>.
    /// </summary>
    public string? DeviceSerialNumber { get; set; }

    /// <summary>
    /// The numer unikatowy, printed on every fiscal document. The manufacturer writes it into the
    /// fiscal memory (Dz.U. 2021 poz. 1759 §60), so it belongs to the fiscal memory and changes when
    /// that is exchanged.
    /// </summary>
    public string? UniqueDeviceNumber { get; set; }

    /// <summary>
    /// The registration number assigned during fiscalization (numer ewidencyjny), identifying the
    /// register in the CRK. It stays the same when the fiscal memory is exchanged.
    /// </summary>
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
