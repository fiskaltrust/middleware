using System;
using System.Globalization;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Client;

/// <summary>
/// Reading status commands after a document is printed: what they report is welcome, but none of
/// them may fail the document — they cannot change the register, so dropping a failed one is safe.
/// </summary>
internal static class PosNetStatus
{
    /// <summary>A counter field as a whole number, or null when the register does not report it.</summary>
    public static long? ReadNumber(PosNetResponse status, string field)
        => status.Parameters.TryGetValue(field, out var text)
            && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;

    /// <summary>Executes a status command and reads its answer, or yields the default when the read fails.</summary>
    public static async Task<T?> TryReadAsync<T>(PosNetClient client, PosNetCommand command, Func<PosNetResponse, T?> read)
    {
        try
        {
            return read(await client.ExecuteAsync(command));
        }
        catch (PLSSCDException)
        {
            // A failed or ambiguous status read leaves the register as it was; the value is
            // simply not reported.
            return default;
        }
    }
}
