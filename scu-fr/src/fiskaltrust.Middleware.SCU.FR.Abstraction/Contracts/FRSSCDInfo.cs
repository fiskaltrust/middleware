using System.Collections.Generic;

namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// State of a French SCU. <see cref="InfoData"/> carries the SCU-specific state as a json blob so
/// the queue can read individual properties without referencing the SCU package — the same
/// pattern the other markets use.
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class FRSSCDInfo
{
    public string? Description { get; set; }

    public string? Version { get; set; }

    /// <summary>Json-serialized SCU state. The blob's contract is owned by the SCU implementation.</summary>
    public string? InfoData { get; set; }

    public Dictionary<string, object> ExtraData { get; set; } = new();
}
