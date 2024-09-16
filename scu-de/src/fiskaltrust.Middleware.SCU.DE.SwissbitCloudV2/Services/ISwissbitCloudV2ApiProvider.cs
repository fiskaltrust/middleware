﻿using System;
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
        
        Task SetTseStateAsync(TseState request);

        Task<TseDto> GetTseStatusAsync();
        Task<List<int>> GetStartedTransactionsAsync();

        Task<StartExportResponse> StartExport();
        Task StoreDownloadResultAsync(string exportId);
        Task<ExportStateResponse> GetExportStateResponseByIdAsync(string exportId);
        Task<Stream> GetExportFromResponseUrlAsync(ExportStateResponse exportStateResponse);
        Task<ExportStateResponse> DeleteExportByIdAsync(string exportId);


        Task DeregisterClientAsync(ClientDto clientDto);

        void Dispose();
    }
}
