using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Helpers
{
    public static class RequestCommandFactoryHelper
    {
        public static SignProcessorFR ConstructSignProcessor(
            ILogger<SignProcessorFR> logger,
            IConfigurationRepository configurationRepository,
            IMiddlewareJournalFRRepository journalFRRepository,
            IActionJournalRepository actionJournalRepository,
            IReadOnlyQueueItemRepository queueItemRepository,
            SignatureFactoryFR signatureFactory,
            IJournalFRCopyPayloadRepository copyPayloadRepository
        )

        {
            var services = new ServiceCollection();
            services.AddScoped<CopyCommand>();
            services.AddSingleton(_ => logger);
            services.AddSingleton(_ => Mock.Of<ILogger<RequestCommand>>());
            services.AddSingleton<ISignatureFactoryFR>(_ => signatureFactory);
            services.AddSingleton(_ => queueItemRepository);
            services.AddSingleton(_ => journalFRRepository);
            services.AddSingleton<IJournalFRRepository>(_ => journalFRRepository);
            services.AddSingleton(_ => copyPayloadRepository);

            var signProcessor =  new SignProcessorFR(
                configurationRepository,
                actionJournalRepository,
                new RequestCommandFactory(services.BuildServiceProvider()),
                signatureFactory
            );

            return signProcessor;
        }
    }
}
