using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Queue;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures
{
    public sealed class SignProcessorDependenciesFixture
    {
        public Guid CASHBOXIDENTIFICATION => Guid.Parse("ddffc471-b101-4b89-8761-dd3c7f779f7c");
        public Guid CASHBOXID => Guid.Parse("fb1b79e2-f269-4fc0-9065-4821fed073d0");
        public Guid QUEUEID => Guid.Parse("b00f3da1-5a6e-4a2d-8fdf-6c3d8900d2c1");

        private readonly Guid _signaturCreationUnitDEId = Guid.NewGuid();
        private readonly Guid _signaturCreationUnitDETargetId = Guid.NewGuid();
        private readonly string _host = "localhost";
        private readonly int _port = 1400;
        public static string terminalID = "369a013a-37e2-4c23-8614-6a8f282e6330";

        public IMiddlewareQueueItemRepository queueItemRepository = new InMemoryQueueItemRepository();
        public IReceiptJournalRepository receiptJournalRepository = new InMemoryReceiptJournalRepository();
        public IActionJournalRepository actionJournalRepository = new InMemoryActionJournalRepository();
        public InMemoryOpenTransactionRepository openTransactionRepository = new InMemoryOpenTransactionRepository();
        public IMasterDataRepository<AccountMasterData> accountMasterDataRepository = new InMemoryAccountMasterDataRepository();
        public IMasterDataRepository<OutletMasterData> outletMasterDataRepository = new InMemoryOutletMasterDataRepository();
        public IMasterDataRepository<PosSystemMasterData> posSystemMasterDataRepository = new InMemoryPosSystemMasterDataRepository();
        public IMasterDataRepository<AgencyMasterData> agencyMasterDataRepository = new InMemoryAgencyMasterDataRepository();
        public IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepository = new InMemoryFailedFinishTransactionRepository();
        public IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepository = new InMemoryFailedStartTransactionRepository();

        public IConfigurationRepository configurationRepository;

        public InMemorySCU InMemorySCU { get; }
        public IDESSCDProvider DeSSCDProvider { get; }

        public SignProcessorDependenciesFixture()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            InMemorySCU = new InMemorySCU();
            var deSSCDProviderMock = new Mock<IDESSCDProvider>();
            deSSCDProviderMock.SetupGet(x => x.Instance).Returns(InMemorySCU);
            DeSSCDProvider = deSSCDProviderMock.Object;
        }

        public IConfigurationRepository CreateConfigurationRepository(bool inFailedMode = false, DateTime? startMoment = null, DateTime? stopMoment = null, bool sourceIsScuSwitch = false, bool targetIsScuSwitch = false, bool queueDECreationUnitIsNull = false)
        {
            return Task.Run(async () =>
            {
                var repo = new InMemoryConfigurationRepository();
                await repo.InsertOrUpdateQueueAsync(new ftQueue
                {
                    ftCashBoxId = CASHBOXID,
                    ftQueueId = QUEUEID,
                    ftReceiptNumerator = 10,
                    ftQueuedRow = 1200,
                    StartMoment = startMoment,
                    StopMoment = stopMoment
                }).ConfigureAwait(false);
                await repo.InsertOrUpdateQueueDEAsync(new ftQueueDE
                {
                    ftQueueDEId = QUEUEID,
                    ftSignaturCreationUnitDEId = queueDECreationUnitIsNull ? null : _signaturCreationUnitDEId,
                    CashBoxIdentification = CASHBOXIDENTIFICATION.ToString(),
                    SSCDFailCount = inFailedMode ? 1 : 0
                }).ConfigureAwait(false);
                await repo.InsertOrUpdateSignaturCreationUnitDEAsync(new ftSignaturCreationUnitDE
                {
                    Url = $"grpc://{_host}:{_port}",
                    ftSignaturCreationUnitDEId = _signaturCreationUnitDEId,
                    Mode = sourceIsScuSwitch ? 0x10000 : 0x00000,
                    ModeConfigurationJson = $"{{\"TargetScuId\": \"{_signaturCreationUnitDETargetId}\"}}"
                }).ConfigureAwait(false);

                await repo.InsertOrUpdateSignaturCreationUnitDEAsync(new ftSignaturCreationUnitDE
                {
                    Url = $"grpc://{_host}:{_port}",
                    ftSignaturCreationUnitDEId = _signaturCreationUnitDETargetId,
                    Mode = targetIsScuSwitch ? 0x20000 : 0x00000,
                    ModeConfigurationJson = $"{{\"SourceScuId\": \"{_signaturCreationUnitDEId}\"}}"

                }).ConfigureAwait(false);
                

                return repo;
            }).Result;
        }
        public SignProcessor CreateSignProcessorForSignProcessorDE(bool queueInFailedMode, DateTime? startMoment = null, DateTime? stopMoment = null, Dictionary<string, object> configs = null, 
            bool masterdataUpdate = false, bool openTrans = false, bool sourceIsScuSwitch = false, bool targetIsScuSwitch = false, bool queueDECreationUnitIsNull = false)
        {
            InMemorySCU.OpenTans = openTrans;
            var journalRepositoryMock = new Mock<IMiddlewareJournalDERepository>(MockBehavior.Strict);
            var configuration = configs ?? new Dictionary<string, object>();
            var config = new MiddlewareConfiguration { Configuration = configuration };
            config.CashBoxId = CASHBOXID;
            config.QueueId = QUEUEID;

            IMasterDataService masterDataService;
            if (masterdataUpdate)
            {
                masterDataService = new MasterDataService(config, accountMasterDataRepository, outletMasterDataRepository, posSystemMasterDataRepository, agencyMasterDataRepository);
            }
            else
            {
                masterDataService = new Mock<IMasterDataService>().Object;
            }
            configurationRepository = CreateConfigurationRepository(queueInFailedMode, startMoment, stopMoment, sourceIsScuSwitch, targetIsScuSwitch, queueDECreationUnitIsNull);

            var signProcessorDE = RequestCommandFactoryHelper.ConstructSignProcessor(Mock.Of<ILogger<SignProcessorDE>>(), configurationRepository, journalRepositoryMock.Object,
                actionJournalRepository, DeSSCDProvider, new DSFinVKTransactionPayloadFactory(), failedFinishTransactionRepository,
                failedStartTransactionRepository, openTransactionRepository, masterDataService, config,
                queueItemRepository, new SignatureFactoryDE(QueueDEConfiguration.FromMiddlewareConfiguration(config)));
            var signProcessor = new SignProcessor(Mock.Of<ILogger<SignProcessor>>(), configurationRepository, queueItemRepository, receiptJournalRepository,
                actionJournalRepository, new CryptoHelper(), signProcessorDE, config);
            return signProcessor;
        }
        public async Task AddOpenOrders(string receiptReference, int transnr) => await openTransactionRepository.InsertAsync(new OpenTransaction { cbReceiptReference = receiptReference, StartMoment = DateTime.UtcNow.AddHours(-12), StartTransactionSignatureBase64 = "somebase64==", TransactionNumber = transnr });

        public async Task AddFailedStartTransaction(string receiptReference) => await failedStartTransactionRepository.InsertOrUpdateTransactionAsync(new FailedStartTransaction { cbReceiptReference = receiptReference, StartMoment = DateTime.UtcNow.AddHours(-12), CashBoxIdentification = CASHBOXIDENTIFICATION.ToString(), ftQueueItemId = Guid.NewGuid() });

        public InitiateSCUSwitch GetInitiateSCUSwitch() => new InitiateSCUSwitch() { SourceSCUId = _signaturCreationUnitDEId, TargetSCUId = _signaturCreationUnitDETargetId };

        public bool AreTargeAndSourceScusAsGiven(Guid signaturCreationUnitDEId, Guid signaturCreationUnitDETargetId) => _signaturCreationUnitDEId.Equals(signaturCreationUnitDEId) && _signaturCreationUnitDETargetId.Equals(signaturCreationUnitDETargetId);
    }
}
