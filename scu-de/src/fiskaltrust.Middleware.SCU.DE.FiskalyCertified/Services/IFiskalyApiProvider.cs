using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services
{
    public interface IFiskalyApiProvider
    {
        Task CreateClientAsync(Guid tssId, string serialNumber, Guid clientId);
        Task<List<ClientDto>> GetClientsAsync(Guid tssId);
        Task<byte[]> GetExportByExportStateAsync(ExportStateInformationDto exportStateInformation);
        Task<Dictionary<string, object>> GetExportMetadataAsync(Guid tssId, Guid exportId);
        Task<ExportStateInformationDto> GetExportStateInformationByIdAsync(Guid tssId, Guid exportId);
        Task<IEnumerable<TransactionDto>> GetStartedTransactionsAsync(Guid tssId);
        Task<TransactionDto> GetTransactionDtoAsync(Guid tssId, ulong transactionNumber);
        Task<TssDto> GetTseByIdAsync(Guid tssId);
        Task<Dictionary<string, object>> GetTseMetadataAsync(Guid tssId);
        Task<TransactionDto> PutTransactionRequestAsync(Guid tssId, Guid transactionId, TransactionRequestDto transactionRequest);
        Task<TransactionDto> PutTransactionRequestWithStateAsync(Guid tssId, ulong transactionNumber, long lastRevision, TransactionRequestDto transactionRequest);
        Task<TssDto> PatchTseStateAsync(Guid tssId, TseStateRequestDto tseState);
        Task RequestExportAsync(Guid tssId, ExportTransactions exportRequest, Guid exportId, long? fromTransactionNumber, long toTransactionNumber);
        Task RequestExportAsync(Guid tssId, ExportTransactionsWithTransactionNumberDto exportRequest, Guid exportId);
        Task RequestExportAsync(Guid tssId, ExportTransactionsWithDatesDto exportRequest, Guid exportId);
        Task SetExportMetadataAsync(Guid tssId, Guid exportId, long? fromTransactionNumber, long toTransactionNumber);
        Task StoreDownloadResultAsync(Guid tssId, Guid exportId);
        Task PatchTseMetadataAsync(Guid tssId, Dictionary<string, object> metadata);
        Task DisableClientAsync(Guid tssId, string serialNumber, Guid clientId);
    }
}
