using System.Threading.Tasks;

namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// The contract every French SCU implements. NF525 fiscalization happens in software
/// (art. 286-I-3° bis CGI), so an "SCU" for France is the component that owns the signature
/// creation data — SIRET, certificate and private key — and turns a receipt into a signed,
/// chained entry. It is not a piece of hardware.
/// </summary>
/// <remarks>
/// <para>
/// The signature is chained (ADR 0001, market-fr#122): the signed content of every entry holds the
/// chain counter (<see cref="ProcessRequest.ChainCounter"/>), the start-of-chain flag and the
/// <em>previous signature</em> of the same receipt chain — the exact Base64Url string the previous
/// entry's signature field holds, never a hash of it and never a re-encoding. Like <c>IPTSSCD</c> —
/// the other software-signing market — the durable chain state stays with the queue: it passes the
/// previous signature in and keeps the returned one for the next entry. France has several parallel
/// chains (tickets, invoices, payment proofs, grand totals, logs, …), so the queue resolves which
/// chain a receipt belongs to and hands over that chain's last signature. Because the rule is the
/// same for every SCU, a chain carries on unbroken when the SCU behind the queue changes.
/// </para>
/// <para>
/// This type is declared here on purpose. The country SSCD contracts normally live in the
/// <c>fiskaltrust.interface</c> package (<c>ifPOS.v2.be</c>, <c>.es</c>, <c>.gr</c>, <c>.pl</c>,
/// <c>.pt</c>), but that package does not ship an <c>ifPOS.v2.fr</c> yet. Namespace and member
/// names match the convention of the other markets, so once the package adds them this file,
/// <see cref="FRSSCDInfo"/> and <see cref="ProcessRequest"/>/<see cref="ProcessResponse"/> are
/// deleted and the <c>ProjectReference</c> from the QueueFR.v2 localization to this project goes
/// away — no call site changes.
/// </para>
/// </remarks>
public interface IFRSSCD
{
    Task<EchoResponse> EchoAsync(EchoRequest echoRequest);

    Task<FRSSCDInfo> GetInfoAsync();

    /// <param name="previousSignature">
    /// The Base64Url signature of the previous entry of the same receipt chain, or null for the
    /// first entry — which then carries the start-of-chain flag.
    /// </param>
    /// <returns>
    /// The signed response and this entry's own Base64Url signature, which the next entry of the
    /// chain covers.
    /// </returns>
    Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature);
}
