using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.EFCore.Repositories;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.AT;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.DE;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.FR;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.IT;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.ME;
using fiskaltrust.storage.encryption.V0;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable
namespace fiskaltrust.Middleware.Storage.EFCore.SQLServer
{
    public class EFCoreSQLServerStorageBootstrapper : BaseStorageBootStrapper
    {
        private string _connectionString;
        private DbContextOptionsBuilder<SQLServerMiddlewareDbContext> _optionsBuilder;
        private readonly Dictionary<string, object> _configuration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly Guid _queueId;

        public EFCoreSQLServerStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _queueId = queueId;
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_queueId, _configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            if (!configuration.ContainsKey("connectionstring"))
            {
                throw new Exception("Database connectionstring not defined");
            }

            if (((string) configuration["connectionstring"]).StartsWith("raw:"))
            {
                _connectionString = ((string) configuration["connectionstring"]).Substring("raw:".Length);
            }
            else
            {
                _connectionString = Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String((string) configuration["connectionstring"]), queueId.ToByteArray()));
            }

            _optionsBuilder = new DbContextOptionsBuilder<SQLServerMiddlewareDbContext>();
            _optionsBuilder.UseSqlServer(_connectionString);

            var newlyAppliedMigrations = Update(_optionsBuilder.Options, queueId, logger);
            var baseMigrations = ConvertAppliedMigrationsToEnum(newlyAppliedMigrations);

            var journalFRCopyPayloadRepository = new EFCoreJournalFRCopyPayloadRepository(
                new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId));
            var journalFRRepository = new EFCoreJournalFRRepository(
                new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId));

            await PerformMigrationInitialization(baseMigrations, journalFRCopyPayloadRepository, journalFRRepository);

            var configurationRepository = new EFCoreConfigurationRepository(new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId));
            var baseStorageConfig = ParseStorageConfiguration(configuration);
            var context = new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId);

            await PersistMasterDataAsync(baseStorageConfig, configurationRepository,
                new EFCoreAccountMasterDataRepository(context), new EFCoreOutletMasterDataRepository(context),
                new EFCoreAgencyMasterDataRepository(context), new EFCorePosSystemMasterDataRepository(context));

            await PersistConfigurationAsync(baseStorageConfig, configurationRepository, logger);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddTransient(x => new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId));

            services.AddTransient<IConfigurationRepository>(_ => new EFCoreConfigurationRepository(new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyConfigurationRepository>(_ => new EFCoreConfigurationRepository(new SQLServerMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IQueueItemRepository, EFCoreQueueItemRepository>();
            services.AddTransient<IReadOnlyQueueItemRepository, EFCoreQueueItemRepository>();
            services.AddTransient<IMiddlewareQueueItemRepository, EFCoreQueueItemRepository>();
            services.AddTransient<IMiddlewareRepository<ftQueueItem>, EFCoreQueueItemRepository>();

            services.AddTransient<IJournalATRepository, EFCoreJournalATRepository>();
            services.AddTransient<IReadOnlyJournalATRepository, EFCoreJournalATRepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalAT>, EFCoreJournalATRepository>();

            services.AddTransient<IJournalDERepository, EFCoreJournalDERepository>();
            services.AddTransient<IReadOnlyJournalDERepository, EFCoreJournalDERepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalDE>, EFCoreJournalDERepository>();

            services.AddTransient<IJournalFRRepository, EFCoreJournalFRRepository>();
            services.AddTransient<IReadOnlyJournalFRRepository, EFCoreJournalFRRepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalFR>, EFCoreJournalFRRepository>();

            services.AddTransient<IJournalFRCopyPayloadRepository, EFCoreJournalFRCopyPayloadRepository>();

            services.AddTransient<IJournalMERepository, EFCoreJournalMERepository>();
            services.AddTransient<IReadOnlyJournalMERepository, EFCoreJournalMERepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalME>, EFCoreJournalMERepository>();

            services.AddTransient<IJournalITRepository, EFCoreJournalITRepository>();
            services.AddTransient<IReadOnlyJournalITRepository, EFCoreJournalITRepository>();
            services.AddTransient<IMiddlewareJournalITRepository, EFCoreJournalITRepository>();

            services.AddTransient<IReceiptJournalRepository, EFCoreReceiptJournalRepository>();
            services.AddTransient<IReadOnlyReceiptJournalRepository, EFCoreReceiptJournalRepository>();
            services.AddTransient<IMiddlewareRepository<ftReceiptJournal>, EFCoreReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, EFCoreActionJournalRepository>();
            services.AddTransient<IActionJournalRepository, EFCoreActionJournalRepository>();
            services.AddTransient<IReadOnlyActionJournalRepository, EFCoreActionJournalRepository>();
            services.AddTransient<IMiddlewareRepository<ftActionJournal>, EFCoreActionJournalRepository>();

            services.AddTransient<IPersistentTransactionRepository<FailedStartTransaction>, EFCoreFailedStartTransactionRepository>();
            services.AddTransient<IPersistentTransactionRepository<FailedFinishTransaction>, EFCoreFailedFinishTransactionRepository>();
            services.AddTransient<IPersistentTransactionRepository<OpenTransaction>, EFCoreOpenTransactionRepository>();

            services.AddTransient<IMasterDataRepository<AccountMasterData>, EFCoreAccountMasterDataRepository>();
            services.AddTransient<IMasterDataRepository<OutletMasterData>, EFCoreOutletMasterDataRepository>();
            services.AddTransient<IMasterDataRepository<AgencyMasterData>, EFCoreAgencyMasterDataRepository>();
            services.AddTransient<IMasterDataRepository<PosSystemMasterData>, EFCorePosSystemMasterDataRepository>();
        }

        public static List<string> Update(DbContextOptions dbContextOptions, Guid queueId, ILogger<IMiddlewareBootstrapper> logger)
        {
            using (var context = new SQLServerMiddlewareDbContext(dbContextOptions, queueId))
            {
                context.Database.SetCommandTimeout(160);
                context.Database.EnsureCreated();
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                context.Database.Migrate();
                return pendingMigrations.ToList();
            }
        }

        private List<BaseStorageBootStrapper.Migrations> ConvertAppliedMigrationsToEnum(List<string> appliedMigrations)
        {
            return appliedMigrations.Select(x =>
            {
                if (x.EndsWith("JournalFRCopyPayload"))
                {
                    return BaseStorageBootStrapper.Migrations.JournalFRCopyPayload;
                }
                return (BaseStorageBootStrapper.Migrations) (-1);
            }).Where(x => x != (BaseStorageBootStrapper.Migrations) (-1)).ToList();
        }
    }
}
