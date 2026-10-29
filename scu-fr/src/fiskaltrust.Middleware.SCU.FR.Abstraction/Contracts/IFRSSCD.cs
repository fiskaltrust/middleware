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
/// The signature is chained: every entry covers the hash of the previous entry of the same
/// receipt chain. Like <c>IPTSSCD</c> — the other software-signing market — the durable chain
/// state stays with the queue: it passes the previous hash in and persists the returned one.
/// France has several parallel chains (tickets, invoices, payment proofs, grand totals, logs, …),
/// so the queue resolves which chain a receipt belongs to and hands over that chain's hash.
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

    /// <param name="lastHash">
    /// The chain hash of the previous entry of the same receipt chain, or null for the first entry.
    /// </param>
    /// <returns>The signed response and the chain hash the queue has to persist for the next entry.</returns>
    Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string? lastHash);
}
