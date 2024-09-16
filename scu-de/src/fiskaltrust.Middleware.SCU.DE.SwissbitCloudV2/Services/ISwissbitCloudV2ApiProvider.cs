using System;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using TseInfo = fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models.TseInfo;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public interface ISwissbitCloudV2ApiProvider
    {
        Task CreateClientAsync(ClientDto client);
        Task<List<string>> GetClientsAsync();

        Task<TransactionResponseDto> TransactionAsync(string transactionType, TransactionRequestDto transactionDto);
        
        Task<TseInfo> GetTseInfoAsync();
        Task SetTseStateAsync(TseState request);

        Task<TseDto> GetTseStatusAsync();
        Task<List<int>> GetStartedTransactionsAsync();
        Task<StartExportResponse> StartExport();
        Task StoreDownloadResultAsync(string exportId);
        Task<ExportStateResponse> GetExportStateResponseByIdAsync(string exportId);


        Task DeregisterClientAsync(ClientDto clientDto);

        void Dispose();
    }
}
