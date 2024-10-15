using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.QueueAT.Services
{
    public interface IExportService
    {
        public Task PerformRksvJournalExportAsync(long fromTimestamp, long toTimestamp, string targetFilePath);
    }
}