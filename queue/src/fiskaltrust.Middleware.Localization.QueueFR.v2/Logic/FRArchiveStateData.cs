using System.Globalization;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Writes into the receipt response what the fiscal archive reads back as columns: the totals, the
/// computed values of each charge item and the VAT summary per rate (archive-v2 §1.2, §1.3, §2, §4).
/// They are written once, when the receipt is signed, so the archive exports stored data and never
/// recomputes it.
/// </summary>
public static class FRArchiveStateData
{
    public static void Apply(ReceiptRequest request, ReceiptResponse response, FRReceiptChain chain, FRPeriodTotals? periodTotals)
    {
        var totals = TotalsOf(request, chain, periodTotals);
        var lines = request.cbChargeItems is { Count: > 0 } items ? ChargeItemsOf(items) : null;
        if (totals is null && lines is null)
        {
            return;
        }

        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.Totals = totals;
        stateData.FR.ChargeItems = lines;
        stateData.FR.VATSummary = lines is null ? null : VatSummaryOf(request.cbChargeItems!, lines);
        if (IsSalesChain(chain))
        {
            stateData.FR.DocumentType = DocumentTypeOf(request);
            stateData.FR.OperationType = OperationTypeOf(request);
            stateData.FR.CustomerEndpointId = chain == FRReceiptChain.Invoice ? FRCustomerEndpoint.Infer(request) : null;
        }
        if (lines is not null)
        {
            stateData.FR.DiscountTotal = request.cbChargeItems!.Where(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount)).Sum(x => x.Amount);
            stateData.FR.LineCount = lines.Count(x => x.CountsAsLine);
        }
        response.ftStateData = stateData;
    }

    private static bool IsSalesChain(FRReceiptChain chain)
        => chain is FRReceiptChain.Ticket or FRReceiptChain.PaymentProof or FRReceiptChain.Invoice or FRReceiptChain.Bill or FRReceiptChain.Training;

    /// <summary>The kind of document (archive-v2 §1.2 <c>DocumentType</c>); a training document has the type of the one it simulates.</summary>
    public static string DocumentTypeOf(ReceiptRequest request)
    {
        if (request.ftReceiptCase.IsType(ReceiptCaseType.Invoice))
        {
            return "invoice";
        }

        return request.ftReceiptCase.Case() switch
        {
            ReceiptCase.PaymentTransfer0x0002 or ReceiptCase.Pay0x3005 => "payment-proof",
            ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => "receipt-without-obligation",
            ReceiptCase.DeliveryNote0x0005 => "delivery-note",
            (ReceiptCase) 0x0006 /* Table check */ => "note",
            (ReceiptCase) 0x0007 /* Pro forma */ => "pro-forma",
            ReceiptCase.Order0x3004 => "order",
            _ => "ticket",
        };
    }

    /// <summary><c>void</c>, <c>refund</c> or <c>sale</c> (archive-v2 §1.2 <c>OperationType</c>): a void wins over a refund flag.</summary>
    public static string OperationTypeOf(ReceiptRequest request)
        => request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) ? "void"
            : request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? "refund"
            : "sale";

    private static FRStateTotals? TotalsOf(ReceiptRequest request, FRReceiptChain chain, FRPeriodTotals? periodTotals)
    {
        if (periodTotals is not null)
        {
            var closing = Values<FRStateTotals>(periodTotals.Current);
            closing.Perpetual = Values<FRStateTotalsValues>(periodTotals.Perpetual);
            return closing;
        }

        return chain is FRReceiptChain.Ticket or FRReceiptChain.PaymentProof or FRReceiptChain.Invoice or FRReceiptChain.Bill or FRReceiptChain.Training
            ? Values<FRStateTotals>(FRTotalsCalculator.From(request))
            : null;
    }

    private static T Values<T>(FRTotals totals) where T : FRStateTotalsValues, new() => new()
    {
        Totalizer = totals.Totalizer,
        CINormal = totals.CINormal,
        CIDiscounted1 = totals.CIReduced1,
        CIDiscounted2 = totals.CIReduced2,
        CISuperReduced1 = totals.CIReducedS,
        CIZero = totals.CIZero,
        CIUnknown = totals.CIUnknown,
        PICash = totals.PICash,
        PINonCash = totals.PINonCash,
        PIInternal = totals.PIInternal,
        PIUnknown = totals.PIUnknown,
    };

    /// <summary>
    /// Per line: the VAT as sent, or computed from the rate when absent and rounded to the cent away from
    /// zero; HT is the amount minus that VAT. A discount or extra applies to the line its case data names
    /// (<c>FR.AppliesTo</c>), else to the nearest preceding sales line; one before any sales line is
    /// receipt-level (D9).
    /// </summary>
    public static List<FRStateChargeItem> ChargeItemsOf(IReadOnlyList<ChargeItem> items)
    {
        var lines = new List<FRStateChargeItem>(items.Count);
        var salesLines = new List<FRStateChargeItem>();
        var targets = new List<(ChargeItem Item, FRStateChargeItem Target)>();
        FRStateChargeItem? lastSalesLine = null;
        foreach (var item in items)
        {
            var vat = item.VATAmount ?? Math.Round(item.Amount * item.VATRate / (100 + item.VATRate), 2, MidpointRounding.AwayFromZero);
            var amountHT = item.Amount - vat;
            var line = new FRStateChargeItem
            {
                Position = item.Position,
                VATAmount = vat,
                AmountHT = amountHT,
                UnitPriceHT = item.Quantity == 0 ? null : Math.Round(amountHT / item.Quantity, 6, MidpointRounding.AwayFromZero),
                VATNature = (((long) item.ftChargeItemCase >> 8) & 0xFF).ToString("X2", CultureInfo.InvariantCulture),
                // As the SCU counts the lines it signs (FRSalesLines, #148), so the archive agrees with the signature.
                CountsAsLine = FRSalesLines.IsSalesLine(item),
            };

            if (item.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount))
            {
                // The target is the line itself, never looked up again by a Position that may repeat. A
                // named FR.AppliesTo picks the first sales line at that Position; a discount before any sales line,
                // or naming none, is receipt-level.
                var named = AppliesTo(item);
                var target = named is { } position ? salesLines.FirstOrDefault(l => l.Position == position) : lastSalesLine;
                line.AppliesTo = named ?? lastSalesLine?.Position;
                if (target is not null)
                {
                    targets.Add((item, target));
                }
            }
            else
            {
                lastSalesLine = line;
                salesLines.Add(line);
            }
            lines.Add(line);
        }

        foreach (var (item, target) in targets)
        {
            target.DiscountAmount = (target.DiscountAmount ?? 0) + item.Amount;
            target.DiscountCode = Join(target.DiscountCode, CaseDataFR(item, "DiscountCode"));
            target.DiscountRate = Join(target.DiscountRate, CaseDataFR(item, "DiscountRate"));
        }
        return lines;
    }

    private static string? Join(string? current, string? value)
        => string.IsNullOrEmpty(value) ? current : string.IsNullOrEmpty(current) ? value : current + "," + value;

    private static string? CaseDataFR(ChargeItem item, string name)
    {
        if (CaseData(item) is not { ValueKind: JsonValueKind.Object } data
            || !TryGet(data, "FR", out var fr) || fr.ValueKind != JsonValueKind.Object
            || !TryGet(fr, name, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static JsonElement? CaseData(ChargeItem item) => item.ftChargeItemCaseData switch
    {
        JsonElement json => json,
        string text when !string.IsNullOrWhiteSpace(text) => TryParse(text),
        null => (JsonElement?) null,
        var other => JsonSerializer.SerializeToElement(other),
    };

    /// <summary>
    /// Every charge item, grouped by rate, highest rate first: discounts, refunds and down payments included.
    /// The same rule as the summary the InfoCert SCU prints (<c>InfoCertVatSummary</c>, fiskaltrust/middleware#828),
    /// so the two agree to the cent: the VAT of a rate is the sum of its items' unrounded VAT (the sent
    /// <c>VATAmount</c>, else the share implied by the rate), rounded once to the cent away from zero. HT is TTC
    /// minus VAT. The per-line <see cref="FRStateChargeItem.VATAmount"/> is rounded per line, so the lines of a
    /// rate can differ from it by a cent.
    /// </summary>
    private static Dictionary<string, FRStateVatSummaryEntry> VatSummaryOf(IReadOnlyList<ChargeItem> items, IReadOnlyList<FRStateChargeItem> lines)
        => items.Select((item, index) => (item, line: lines[index]))
            .GroupBy(x => x.item.VATRate.ToString("0.##########", CultureInfo.InvariantCulture))
            .OrderByDescending(g => g.Max(x => x.item.VATRate))
            .ToDictionary(g => g.Key, g =>
            {
                var ttc = g.Sum(x => x.item.Amount);
                var vat = Math.Round(g.Sum(x => x.item.VATAmount ?? x.item.Amount / (100 + x.item.VATRate) * x.item.VATRate), 2, MidpointRounding.AwayFromZero);
                return new FRStateVatSummaryEntry
                {
                    AmountTTC = ttc,
                    VATAmount = vat,
                    AmountHT = ttc - vat,
                    VATNature = string.Join(",", g.Select(x => x.line.VATNature).Distinct().OrderBy(n => n, StringComparer.Ordinal)),
                };
            });

    private static decimal? AppliesTo(ChargeItem item)
    {
        var caseData = CaseData(item);
        if (caseData is not { ValueKind: JsonValueKind.Object } data
            || !TryGet(data, "FR", out var fr) || fr.ValueKind != JsonValueKind.Object
            || !TryGet(fr, "AppliesTo", out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var position) => position,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var position) => position,
            _ => null,
        };
    }

    private static JsonElement? TryParse(string text)
    {
        try
        {
            return JsonDocument.Parse(text).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
