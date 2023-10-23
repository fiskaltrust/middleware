using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
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
            IMiddlewareJournalDERepository journalDERepository,
            IActionJournalRepository actionJournalRepository,
            IDESSCDProvider dESSCDProvider,
            ITransactionPayloadFactory transactionPayloadFactory,
            IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo,
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo,
            IMasterDataService masterDataService,
            MiddlewareConfiguration middlewareConfiguration,
            IReadOnlyQueueItemRepository queueItemRepository,
            SignatureFactoryDE signatureFactory,
            ITarFileCleanupService tarFileCleanupService = null)
        {
            var services = new ServiceCollection();
            services.ConfigureReceiptCommands();
            services.AddSingleton(_ => logger);
            services.AddSingleton(_ => Mock.Of<ILogger<RequestCommand>>());
            services.AddSingleton(_ => signatureFactory);
            services.AddSingleton(_ => failedFinishTransactionRepo);
            services.AddSingleton(_ => failedStartTransactionRepo);
            services.AddSingleton(_ => openTransactionRepo);
            services.AddSingleton(_ => dESSCDProvider);
            services.AddSingleton(_ => transactionPayloadFactory);
            services.AddSingleton(_ => queueItemRepository);
            services.AddSingleton(_ => configurationRepository);
            services.AddSingleton(_ => actionJournalRepository);
            services.AddSingleton(_ => journalDERepository);
            services.AddSingleton<IJournalDERepository>(_ => journalDERepository);
            services.AddSingleton(_ => middlewareConfiguration);
            services.AddSingleton(_ => QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), middlewareConfiguration));
            services.AddSingleton(_ => masterDataService);

            if(tarFileCleanupService == null)
            {
                tarFileCleanupService = Mock.Of<ITarFileCleanupService>();
            }

            services.AddSingleton(tarFileCleanupService);

            var signProcessor =  new SignProcessorDE(
                configurationRepository,
                transactionPayloadFactory,
                new RequestCommandFactory(services.BuildServiceProvider()),
                logger
            );

            return signProcessor;
        }
    }
}
