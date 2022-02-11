using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers
{
    public static class RequestCommandFactoryHelper
    {
        public static SignProcessorDE ConstructSignProcessor(
            ILogger<SignProcessorDE> logger,
            IConfigurationRepository configurationRepository,
            IJournalDERepository journalDERepository,
            IActionJournalRepository actionJournalRepository,
            IDESSCDProvider dESSCDProvider,
            ITransactionPayloadFactory transactionPayloadFactory,
            IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo,
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo,
            IMasterDataService masterDataService,
            MiddlewareConfiguration middlewareConfiguration,
            IReadOnlyQueueItemRepository queueItemRepository,
            SignatureFactoryDE signatureFactory)
        {
            var servcies = new ServiceCollection();
            servcies.ConfigureReceiptCommands();
            servcies.AddSingleton(_ => logger);
            servcies.AddSingleton(_ => Mock.Of<ILogger<RequestCommand>>());
            servcies.AddSingleton(_ => signatureFactory);
            servcies.AddSingleton(_ => failedFinishTransactionRepo);
            servcies.AddSingleton(_ => failedStartTransactionRepo);
            servcies.AddSingleton(_ => openTransactionRepo);
            servcies.AddSingleton(_ => dESSCDProvider);
            servcies.AddSingleton(_ => transactionPayloadFactory);
            servcies.AddSingleton(_ => queueItemRepository);
            servcies.AddSingleton(_ => configurationRepository);
            servcies.AddSingleton(_ => actionJournalRepository);
            servcies.AddSingleton(_ => journalDERepository);
            servcies.AddSingleton(_ => middlewareConfiguration);
            servcies.AddSingleton(_ => masterDataService);

            return new SignProcessorDE(
                configurationRepository,
                dESSCDProvider,
                transactionPayloadFactory,
                new RequestCommandFactory(servcies.BuildServiceProvider())
            );
        }
    }
}
