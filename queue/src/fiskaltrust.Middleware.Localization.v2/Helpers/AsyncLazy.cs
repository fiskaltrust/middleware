using System.Runtime.CompilerServices;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public class AsyncLazy<T>
{
    private readonly Lazy<Task<T>> _value;

    public AsyncLazy(Func<Task<T>> value)
    {
        _value = new Lazy<Task<T>>(Task.Run(value));
    }

    public Task<T> Value => _value.Value;
    public bool IsValueCreated => _value.IsValueCreated;

    public TaskAwaiter<T> GetAwaiter() => _value.Value.GetAwaiter();
}

public static class AsyncLazyExt
{
    public static AsyncLazy<U> Cast<T, U>(this AsyncLazy<T> self)
        where T : class, U
        where U : class
        => new(async () => await self.Value.ConfigureAwait(false));
}