using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.Azure.Migrations
{
    public interface IAzureStorageMigration
    {
        int Version { get; }
        Task ExecuteAsync();
    }
}