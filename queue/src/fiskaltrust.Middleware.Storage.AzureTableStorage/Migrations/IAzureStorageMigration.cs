using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations
{
    public interface IAzureStorageMigration
    {
        int Version { get; }
        Task ExecuteAsync();
    }
}