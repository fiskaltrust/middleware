using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Checks the chains of stored, signed entries (market-fr ADR 0001, 0009, #208): within each chain the
/// counters follow on and each entry's signed previous signature is the signature of the entry before
/// it, and the last entry of each chain is the one the chain state in ftQueueFR points at. A broken
/// link is an integrity defect (R19 code 90: secured data changed afterwards); a gap or a repeat in
/// the counters is a break of the continuous sequence (R19 code 95).
/// </summary>
public static class FRChainIntegrity
{
    public const int IntegrityDefect = 90;
    public const string IntegrityDefectLabel = "Détection d’un défaut d’intégrité dans les données sécurisées ou dans une archive fiscale";

    public const int SequenceBreak = 95;
    public const string SequenceBreakLabel = "Détection d'une rupture de la séquence continue";

    /// <summary>
    /// The defects grouped by code, one JET per code (ADR 0009, #208): the code, its R19 label, and the
    /// <c>Information</c> of every defect of that code.
    /// </summary>
    public static IEnumerable<(int Code, string Label, string Information)> PerCode(IEnumerable<Defect> defects)
        => defects.GroupBy(x => x.Code).OrderBy(x => x.Key).Select(g => (g.Key, Label(g.Key), string.Join(" | ", g.Select(x => x.Information))));

    /// <summary>The R19 label of <paramref name="code"/>, 90 or 95.</summary>
    public static string Label(int code) => code == IntegrityDefect ? IntegrityDefectLabel : SequenceBreakLabel;

    /// <summary>One defect: the R19 code it calls for and what was found, as JET <c>Information</c>.</summary>
    public sealed record Defect(int Code, string Information);

    /// <param name="entriesInQueueOrder">Signed entries in queue order; an entry that could not be read is passed as its failure.</param>
    /// <param name="queueFR">The chain state the last entry of each chain is compared with; null for a past period, whose last entries the state has moved on from.</param>
    public static List<Defect> Check(IEnumerable<(ArchiveEntry? Entry, ArchiveEntryException? Unreadable)> entriesInQueueOrder, ftQueueFR? queueFR)
    {
        var defects = new List<Defect>();
        var last = new Dictionary<string, ArchiveEntry>(StringComparer.Ordinal);
        foreach (var (entry, unreadable) in entriesInQueueOrder)
        {
            if (unreadable is not null)
            {
                defects.Add(new Defect(IntegrityDefect, FRJetItems.Information(("ftQueueItemId", unreadable.QueueItemId.ToString()), ("defect", unreadable.Message))));
                continue;
            }

            if (entry is null)
            {
                continue;
            }

            if (last.TryGetValue(entry.Chain, out var previous))
            {
                if (entry.ChainCounter != previous.ChainCounter + 1)
                {
                    defects.Add(new Defect(SequenceBreak, Describe(entry, $"{previous.ChainCounter + 1}", $"{entry.ChainCounter}")));
                }
                else if ((entry.PreviousSignature ?? "") != previous.ChainedSignature)
                {
                    defects.Add(new Defect(IntegrityDefect, Describe(entry, $"previous signature of {previous.ReceiptIdentification}", "another previous signature")));
                }
            }

            last[entry.Chain] = entry;
        }

        foreach (var (letter, entry) in queueFR is null ? [] : last)
        {
            if (Chains.TryGetValue(letter, out var chain) is false)
            {
                continue;
            }

            var counter = queueFR!.GetNumerator(chain);
            if (counter != entry.ChainCounter)
            {
                defects.Add(new Defect(SequenceBreak, Describe(entry, $"{counter} in the chain state", $"{entry.ChainCounter} as the last stored entry")));
            }
            else if ((queueFR.GetLastSignature(chain) ?? "") != entry.ChainedSignature)
            {
                defects.Add(new Defect(IntegrityDefect, Describe(entry, "the chain state's last signature", "another signature on the last stored entry")));
            }
        }

        return defects;
    }

    private static readonly Dictionary<string, FRReceiptChain> Chains = Enum.GetValues<FRReceiptChain>().ToDictionary(x => x.Identifier(), StringComparer.Ordinal);

    private static string Describe(ArchiveEntry entry, string expected, string found) => FRJetItems.Information(
        ("chain", entry.Chain),
        ("ftReceiptIdentification", entry.ReceiptIdentification),
        ("ftQueueItemId", entry.QueueItemId.ToString()),
        ("expected", expected),
        ("found", found));
}
