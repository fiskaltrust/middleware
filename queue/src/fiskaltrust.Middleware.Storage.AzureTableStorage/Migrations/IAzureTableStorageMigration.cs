using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations
{
    public interface IAzureTableStorageMigration
    {
        int Version { get; }
        Task ExecuteAsync();
    }
}

