using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

namespace fiskaltrust.Middleware.Localization.v2.QueueES.Storage
{
    public interface ISCUStateProvider
    {
        Task<StateData> LoadAsync();
        Task SaveAsync(StateData stateData);
    }
}