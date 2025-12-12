using fiskaltrust.Middleware.Abstractions;

namespace fiskaltrust.Middleware.Test.Launcher.Helpers;

class InMemoryClientFactory<T> : IClientFactory<T>
{
    private readonly T _client;

    public InMemoryClientFactory(T client)
    {
        _client = client;
    }

    T IClientFactory<T>.CreateClient(ClientConfiguration configuration) => _client;
}