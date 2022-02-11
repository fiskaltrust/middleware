namespace fiskaltrust.Middleware.Abstractions
{
    public interface IClientFactory<T>
    {
        T CreateClient(ClientConfiguration configuration);
    }
}
