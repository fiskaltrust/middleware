using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public interface ISwissbitCloudV2ApiProvider
    {
        Task CreateClientAsync(ClientDto client);
        Task<List<string>> GetClientsAsync();

        Task<TransactionResponseDto> TransactionAsync(string transactionType, TransactionRequestDto transactionDto);
        
        Task<TseDto> DisableTseAsync();

        Task<TseDto> GetTseStatusAsync();
        Task<List<int>> GetStartedTransactionsAsync();

        Task<ExportDto> StartExport();
        Task<List<ExportDto>> GetExports();
        Task StoreDownloadResultAsync(ExportDto exportDto);
        Task<ExportDto> GetExportStateResponseByIdAsync(string exportId);
        Task<Stream> GetExportFromResponseUrlAsync(ExportDto exportDto);
        Task<ExportDto> DeleteExportByIdAsync(string exportId);


        Task DeregisterClientAsync(ClientDto clientDto);

        void Dispose();
    }
}
