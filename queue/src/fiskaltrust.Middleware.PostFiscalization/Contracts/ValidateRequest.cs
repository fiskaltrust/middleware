using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.PostFiscalization.Contracts;

/// <summary>
/// Body of the preflight call (<c>POST {endpoint}/validate</c>) to an eInvoicing or eReporting service.
/// No fiscal data exists yet when this call is made, so the service only receives the <see cref="ReceiptRequest"/>.
/// </summary>
/// <remarks>
/// The types in this namespace mirror the market-agnostic contract of RFC 712
/// (<c>rfcs/712-queue-einvoicing-ereporting.md</c>). They are destined for the <c>fiskaltrust.ifPOS.v2</c>
/// namespace of the <c>fiskaltrust.interface</c> package and live here only until a package version that
/// ships them is referenced; the JSON wire format is identical, so swapping them is a using-directive change.
/// They are shared by the v2 and the legacy queue stack, which is why this assembly targets netstandard2.0.
/// </remarks>
public class ValidateRequest
{
    public ReceiptRequest ReceiptRequest { get; set; } = null!;
}
