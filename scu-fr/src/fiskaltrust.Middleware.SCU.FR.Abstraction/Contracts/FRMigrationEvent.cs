namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// The migration event a finish-migration receipt (0x4022) signs when a v1.2 queue moves to v2
/// (market-fr ADR 0003): for each v1 receipt chain, its last counter and the last hash v1 kept, and
/// the last signature when v1 storage still holds it. It is the signed link from the v1 chains to
/// the v2 ones; a v2 chain whose v1 signature could not be read starts with no previous signature.
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>. The queue hands it over in <c>ftStateData.FR.Migration</c>.</remarks>
public class FRMigrationEvent
{
    public List<FRV1ChainHead> Chains { get; set; } = new();
}

/// <summary>Where one v1 chain stopped: its v1 letter, counter, last hash and, when readable, last signature.</summary>
public class FRV1ChainHead
{
    /// <summary>The v1 group letter (T, P, I, B, L, C, X, G, A).</summary>
    public string Chain { get; set; } = "";

    public long Counter { get; set; }

    public string? LastHash { get; set; }

    /// <summary>The Base64Url signature of the chain's last v1 entry, when v1 storage still holds it.</summary>
    public string? LastSignature { get; set; }
}
