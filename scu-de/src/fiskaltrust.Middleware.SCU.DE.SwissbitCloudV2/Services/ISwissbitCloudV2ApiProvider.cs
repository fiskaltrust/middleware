using System;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;


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

        Task<StartExportResponseDto> StartExport();
        Task StoreDownloadResultAsync(string exportId);
        Task<ExportStateResponseDto> GetExportStateResponseByIdAsync(string exportId);
        Task<Stream> GetExportFromResponseUrlAsync(ExportStateResponseDto exportStateResponse);
        Task<ExportStateResponseDto> DeleteExportByIdAsync(string exportId);


        Task DeregisterClientAsync(ClientDto clientDto);

        void Dispose();
    }
}
