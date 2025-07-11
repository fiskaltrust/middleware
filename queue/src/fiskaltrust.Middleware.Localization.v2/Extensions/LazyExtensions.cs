namespace fiskaltrust.Middleware.Localization.v2.Extensions;

public static class LazyExt
{
    public static Lazy<Task<U>> Cast<T, U>(this Lazy<Task<T>> self)
        where T : class, U
        where U : class
        => new(async () => await self.Value.ConfigureAwait(false));
}