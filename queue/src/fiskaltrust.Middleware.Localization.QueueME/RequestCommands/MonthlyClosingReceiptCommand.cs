using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class MonthlyClosingReceiptCommand : RequestCommand
    {
        public MonthlyClosingReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IMasterDataRepository<OutletMasterData> outletMasterDataRepository) : base(logger, signatureFactory, configurationRepository, outletMasterDataRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {

                return await Task.FromResult(new RequestCommandResponse()
                {
                });
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }

        }
    }
}
