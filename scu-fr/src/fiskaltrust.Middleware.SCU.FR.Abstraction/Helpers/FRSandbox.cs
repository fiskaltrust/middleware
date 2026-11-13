using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Whether the SCU runs in a sandbox. The Launcher adds a <c>sandbox</c> key to every package's
/// configuration, SCUs included. A sandbox SCU is not certified, so it must not print a certification
/// claim: LNE Rev. 1.8 §III.10 forbids any reference to a certification that was not notified, and the
/// NF525 certification rules treat a misused mark as misleading advertising (market-fr#221, 2026-11-07).
/// </summary>
public static class FRSandbox
{
    /// <summary>The wording that replaces the certificate or attestation number in a sandbox.</summary>
    public const string PendingCertification = "en cours de certif.";

    /// <summary>
    /// Reads the <c>sandbox</c> key, case-insensitively, as a boolean or a boolean string. A missing or
    /// unreadable value means production.
    /// </summary>
    public static bool IsSet(Dictionary<string, object>? configuration)
    {
        if (configuration is null)
        {
            return false;
        }

        foreach (var entry in configuration)
        {
            if (!string.Equals(entry.Key, "sandbox", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Read like every other configuration value (FRScuConfiguration), so a Newtonsoft value counts too.
            return bool.TryParse(FRScuConfiguration.Text(entry.Value), out var parsed) && parsed;
        }

        return false;
    }
}
