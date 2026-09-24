namespace fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

/// <summary>
/// Filling in the <c>init_</c> tables a cashbox configuration did not bring.
/// </summary>
static class InitTableExt
{
    /// <summary>
    /// Adds an init table unless the configuration already carries one under that name. What the
    /// configuration brings wins, so a portal export keeps its own tables — an <c>Add</c> would throw
    /// on the duplicate key, and overwriting would silently discard the configured cashbox.
    /// </summary>
    /// <param name="table">
    /// Built only when it is actually needed, so filling a gap and honouring a configured table cost
    /// the same at the call site.
    /// </param>
    /// <returns>Whether the table was added.</returns>
    public static bool AddUnlessConfigured(this Dictionary<string, object> configuration, string key, Func<object> table)
    {
        if (configuration.ContainsKey(key))
        {
            return false;
        }

        configuration.Add(key, table());
        return true;
    }
}
