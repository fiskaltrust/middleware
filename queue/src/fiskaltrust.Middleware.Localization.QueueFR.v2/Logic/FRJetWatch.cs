using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The JET events a change of installation calls for, found once per process (market-fr#200): a new
/// middleware version (250 major, 255 minor), a new signing key pair (450, SCU switch or certificate
/// renewal) and a new SCU kind (270, e.g. InfoCert ↔ LNE ↔ self-certified). Each of these changes needs
/// a restart (new package, or portal → rebuild configuration → restart), so the first signed,
/// non-training receipt after start-up is compared with the last signed entry stored before it, and
/// its response carries the JET items. No column is added: the previous values are read from that
/// stored queue item (its <c>ProcessingVersion</c> and its receipt QR code).
/// </summary>
public class FRJetWatch
{
    /// <summary>How far back the last signed entry is looked for, past unsigned and failed queue items.</summary>
    public const int MaxLookBack = 50;

    private readonly AsyncLazy<IMiddlewareQueueItemRepository>? _queueItems;
    private bool _done;

    public FRJetWatch(AsyncLazy<IMiddlewareQueueItemRepository>? queueItems)
    {
        _queueItems = queueItems;
        _done = queueItems is null;
    }

    /// <summary>
    /// On the first signed, non-training receipt of the process, adds the JET items for what changed
    /// since the last signed entry. Called under the queue's chain lock, after a successful signature.
    /// </summary>
    public async Task CheckOnceAsync(ReceiptRequest request, ReceiptResponse response)
    {
        if (_done || request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return;
        }

        _done = true;
        var repository = await _queueItems!;
        var current = await repository.GetLastQueueItemAsync().ConfigureAwait(false);
        if (current is null || current.ftQueueItemId != response.ftQueueItemID)
        {
            return;
        }

        var previous = await LastSignedBeforeAsync(repository, current.ftQueueRow).ConfigureAwait(false);
        if (previous is null)
        {
            return;
        }

        foreach (var item in Compare(previous, Installation.Of(current.ProcessingVersion, response)))
        {
            response.ftSignatures.Add(item);
        }
    }

    /// <summary>The JET items for the differences between two installations.</summary>
    public static IEnumerable<SignatureItem> Compare(Installation previous, Installation current)
    {
        var versionCode = VersionChange(previous.Version, current.Version);
        if (versionCode is { } code)
        {
            yield return SignaturItemFactory.CreateJetEventSignature(code,
                code == FRJetItems.MajorVersionInstalled ? FRJetItems.MajorVersionInstalledLabel : FRJetItems.MinorVersionInstalledLabel,
                FRJetItems.Information(("previousVersion", previous.Version), ("version", current.Version)));
        }

        if (!string.IsNullOrEmpty(previous.CertificateSerialNumber) && !string.IsNullOrEmpty(current.CertificateSerialNumber)
            && !string.Equals(previous.CertificateSerialNumber, current.CertificateSerialNumber, StringComparison.OrdinalIgnoreCase))
        {
            yield return SignaturItemFactory.CreateJetEventSignature(FRJetItems.SigningKeyChange, FRJetItems.SigningKeyChangeLabel,
                FRJetItems.Information(("previousCertificateSerialNumber", previous.CertificateSerialNumber), ("certificateSerialNumber", current.CertificateSerialNumber)));
        }

        if (previous.ScuKind is not null && current.ScuKind is not null && previous.ScuKind != current.ScuKind)
        {
            yield return SignaturItemFactory.CreateJetEventSignature(FRJetItems.ComplianceSettingChange, FRJetItems.ComplianceSettingChangeLabel,
                FRJetItems.Information(("previousScu", previous.ScuKind), ("scu", current.ScuKind)));
        }
    }

    /// <summary>
    /// 250 when the major version differs, 255 when anything else of the version number does, none
    /// when only the build metadata (after '+') changed or a version is unknown.
    /// </summary>
    public static int? VersionChange(string? previous, string? current)
    {
        var (previousNumber, currentNumber) = (Number(previous), Number(current));
        if (previousNumber is null || currentNumber is null || previousNumber == currentNumber)
        {
            return null;
        }

        return Major(previousNumber) == Major(currentNumber) ? FRJetItems.MinorVersionInstalled : FRJetItems.MajorVersionInstalled;

        static string? Number(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            var plus = version!.IndexOf('+');
            return (plus < 0 ? version : version.Substring(0, plus)).Trim();
        }

        static string Major(string number)
        {
            var dot = number.IndexOf('.');
            return dot < 0 ? number : number.Substring(0, dot);
        }
    }

    private static async Task<Installation?> LastSignedBeforeAsync(IMiddlewareQueueItemRepository repository, long currentRow)
    {
        for (var row = currentRow - 1; row > 0 && row >= currentRow - MaxLookBack; row--)
        {
            var item = await repository.GetByQueueRowAsync(row).ConfigureAwait(false);
            if (item is null || string.IsNullOrEmpty(item.response))
            {
                continue;
            }

            // A v1.2 entry (half migration) is not a v2 archive entry, but its version still counts.
            ArchiveEntry? entry;
            try
            {
                entry = ArchiveEntry.Read(item.ftQueueItemId, item.ftQueueId, item.ftQueueRow, item.request, item.response);
            }
            catch (ArchiveEntryException)
            {
                // An unreadable entry tells nothing about the installation; the export reports it.
                continue;
            }
            if (entry is not null)
            {
                return new Installation(item.ProcessingVersion, entry.CertificateSerialNumber, ScuKindOf(entry.Signature, HasCertificationBody(item.response)));
            }

            if (!string.Equals(item.version, "v2", StringComparison.OrdinalIgnoreCase) && !IsError(item.response))
            {
                return new Installation(item.ProcessingVersion, null, null);
            }
        }

        return null;
    }

    /// <summary>
    /// The SCU kind as the receipt shows it: a JWS QR code is InfoCert, a data set with a
    /// certification-body item is LNE, a data set without one is self-certified (or in-memory).
    /// </summary>
    public static string? ScuKindOf(string? qrCode, bool hasCertificationBody)
    {
        if (string.IsNullOrEmpty(qrCode))
        {
            return null;
        }

        var dot = qrCode!.LastIndexOf('.');
        var signedContent = dot < 0 ? "" : qrCode.Substring(0, dot);
        if (signedContent.IndexOf('|') < 0)
        {
            return "InfoCert";
        }

        return hasCertificationBody ? "LNE" : "SelfCertified";
    }

    private static bool HasCertificationBody(ReceiptResponse response)
        => response.ftSignatures.Any(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody));

    private static bool HasCertificationBody(string response)
    {
        try
        {
            using var json = JsonDocument.Parse(response);
            if (!json.RootElement.TryGetProperty("ftSignatures", out var signatures) || signatures.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            return signatures.EnumerateArray().Any(s => s.TryGetProperty("ftSignatureType", out var type)
                && type.TryGetInt64(out var value)
                && value >> 48 == 0x4652
                && (value & 0xFFFF) == ((long) SignatureTypeFR.CertificationBody & 0xFFFF));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsError(string response)
    {
        try
        {
            using var json = JsonDocument.Parse(response);
            return json.RootElement.TryGetProperty("ftState", out var state) && state.TryGetInt64(out var value)
                && (value & 0xFFFF_FFFF) is 0xEEEE_EEEE or 0xFFFF_FFFF;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    /// <summary>What a signed entry says about the installation that signed it.</summary>
    public sealed record Installation(string? Version, string? CertificateSerialNumber, string? ScuKind)
    {
        public static Installation Of(string? version, ReceiptResponse response)
        {
            var qrCode = response.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature))?.Data;
            var serial = qrCode is null ? null : ArchiveEntry.CertificateSerialNumberOf(qrCode);
            return new Installation(version, serial, ScuKindOf(qrCode, HasCertificationBody(response)));
        }
    }
}
