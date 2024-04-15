using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Queue;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using fiskaltrust.ifPOS.v1.at;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Linq;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class SignProcessorATFixture
    {
        private const string CERT_PASSWORD = "password";
        public Guid cashBoxId { get; private set; }
        public Guid queueId { get; private set; }
        public ftQueue queue { get; private set; }
        public ftQueueAT queueAT { get; private set; }
        public MiddlewareConfiguration middlewareConfiguration;
        public QueueATConfiguration queueATConfiguration;
        public ILogger<RequestCommand> logger;
        public IConfigurationRepository configurationRepository;
        private ftSignaturCreationUnitAT _signaturCreationUnitAT;
        private readonly AsymmetricKeyParameter _key;
        private readonly byte[] _certificate;

        public SignProcessorATFixture()
        {
            cashBoxId = Guid.Parse("6caa852c-4230-4496-83c0-1597eee7084e");
            queueId = Guid.Parse("ef9764af-1102-41e8-b901-eb89d45cde1c");
            middlewareConfiguration = new MiddlewareConfiguration()
            {
                CashBoxId = cashBoxId,
                QueueId = queueId,
                IsSandbox = true,
                Configuration = new Dictionary<string, object>()
                {
                    { "EnableMonthlyExport", false }
                }
            };
            queueATConfiguration = QueueATConfiguration.FromMiddlewareConfiguration(middlewareConfiguration);
            logger = new Mock<ILogger<RequestCommand>>().Object;
            var randomCertificate = CreateRandomSignatureCertificate();

            using var ms = new MemoryStream(randomCertificate);
            var pkcs = new Pkcs12Store(ms, CERT_PASSWORD.ToCharArray());

            foreach (var alias in pkcs.Aliases)
            {
                if (pkcs.IsKeyEntry(alias as string))
                {
                    var key = pkcs.GetKey(alias as string).Key;
                    var certChain = pkcs.GetCertificateChain(alias as string);
#pragma warning disable IDE0056 // Use index operator
                    var cert = certChain[certChain.Length - 1].Certificate;
#pragma warning restore IDE0056 // Use index operator

                    if (cert.SigAlgOid.Equals("1.2.840.10045.4.3.2"))
                    {
                        _key = key;
                        _certificate = cert.GetEncoded();
                        return;
                    }
                }
            }
        }

        public IATSSCDProvider GetIATSSCDProvider(string zda)
        {
            var sscd = new Mock<IATSSCD>();
            sscd.Setup(x => x.ZdaAsync()).ReturnsAsync(new ZdaResponse() { ZDA = zda });
            sscd.Setup(x => x.SignAsync(It.IsAny<SignRequest>())).ReturnsAsync(new SignResponse() { SignedData = new byte[] { 0x01, 0x02, 0x03 } });
            sscd.Setup(x => x.CertificateAsync()).ReturnsAsync(new CertificateResponse { Certificate = _certificate });
            var sscdProvider = new Mock<IATSSCDProvider>();
            sscdProvider.Setup(x => x.GetCurrentlyActiveInstanceAsync()).ReturnsAsync((_signaturCreationUnitAT, sscd.Object, 0));
            var scus = new List<ftSignaturCreationUnitAT>
            {
                _signaturCreationUnitAT
            };
            sscdProvider.Setup(x => x.GetAllInstances()).ReturnsAsync(scus);
            return sscdProvider.Object;
        }

        public ftQueueItem CreateQueueItem(ReceiptRequest request)
        {
            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueMoment = DateTime.UtcNow,
                ftQueueTimeout = queue.Timeout,
                cbReceiptMoment = request.cbReceiptMoment,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftQueueRow = ++queue.ftQueuedRow
            };
            if (queueItem.ftQueueTimeout == 0)
            {
                queueItem.ftQueueTimeout = 15000;
            }

            queueItem.country = ReceiptRequestHelper.GetCountry(request);
            queueItem.version = ReceiptRequestHelper.GetRequestVersion(request);
            queueItem.request = JsonConvert.SerializeObject(request);
            return queueItem;
        }

        public async Task CreateConfigurationRepository(bool inFailedMode = false, DateTime? startMoment = null, DateTime? stopMoment = null)
        {
            configurationRepository = new InMemoryConfigurationRepository();
            _signaturCreationUnitAT = new ftSignaturCreationUnitAT()
            {
                ftSignaturCreationUnitATId = Guid.NewGuid()
            };
            await configurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(_signaturCreationUnitAT);

            queue = new ftQueue
            {
                ftCashBoxId = cashBoxId,
                ftQueueId = queueId,
                ftReceiptNumerator = 10,
                ftQueuedRow = 1200,
                StartMoment = startMoment,
                StopMoment = stopMoment
            };
            await configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
            queueAT = new ftQueueAT
            {
                ftQueueATId = queueId,
                SSCDFailCount = inFailedMode ? 1 : 0,
                CashBoxIdentification = cashBoxId.ToString(),
                SignAll = true
            };
            using (var sha256 = SHA256.Create())
            {
                var rawKey = Encoding.UTF8.GetBytes($"{queueAT.CashBoxIdentification} {DateTime.UtcNow:G} {Guid.NewGuid()}");
                queueAT.EncryptionKeyBase64 = Convert.ToBase64String(sha256.ComputeHash(rawKey));
            }
            await configurationRepository.InsertOrUpdateQueueATAsync(queueAT).ConfigureAwait(false);
        }

        public ReceiptRequest CreateReceiptRequest(string filename)
        {
            var fullpath = Path.Combine("RequestCommands", "Requests", filename);
            var content = File.ReadAllText(fullpath);
            content = content.Replace("{{cashbox_id}}", cashBoxId.ToString());
            content = content.Replace("{{possystem_id}}", Guid.NewGuid().ToString());
            content = content.Replace("{{current_moment}}", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            return JsonConvert.DeserializeObject<ReceiptRequest>(content);
        }

        private byte[] CreateRandomSignatureCertificate()
        {
            var random = new SecureRandom();
            var kpg = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
            kpg.Init(new KeyGenerationParameters(random, 256));

            var signingKeyPair = kpg.GenerateKeyPair();

            var signingBuilder = new X509V3CertificateGenerator();
            signingBuilder.SetSerialNumber(BigInteger.One);
            signingBuilder.SetNotBefore(DateTime.Now.AddDays(-1));
            signingBuilder.SetNotAfter(DateTime.Now.AddYears(20));
            signingBuilder.SetPublicKey(signingKeyPair.Public);
            signingBuilder.SetIssuerDN(new X509Name("CN=fiskaltrust DEMO"));
            signingBuilder.SetSubjectDN(new X509Name("CN=fiskaltrust DEMO Signing Cert"));
            signingBuilder.SetSignatureAlgorithm("SHA256withECDSA");
            signingBuilder.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));
            signingBuilder.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(X509KeyUsage.DigitalSignature));
            var signingCertificate = signingBuilder.Generate(signingKeyPair.Private);

            var store = new Pkcs12Store();
            var signingCertificateEntry = new X509CertificateEntry(signingCertificate);
            store.SetKeyEntry("sign-pk", new AsymmetricKeyEntry(signingKeyPair.Private), new[] { signingCertificateEntry });

            var ms = new MemoryStream();
            store.Save(ms, CERT_PASSWORD.ToCharArray(), random);

            return ms.ToArray();
        }

        public static void TestAllSignSignatures(Models.RequestCommandResponse result, bool iszero = true, string counterAdd = "", bool check = true)
        {
            var signAllSig = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Sign-All-Mode").FirstOrDefault();
            signAllSig.Should().NotBeNull();
            signAllSig.Data.Should().Be("Sign: True Counter:True");
            signAllSig.ftSignatureType.Should().Be(4707387510509010944);
            if (iszero)
            {
                var signZero = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Zero-Receipt").FirstOrDefault();
                signZero.Should().NotBeNull();
                signZero.Data.Should().Be("Sign: True Counter:True");
                signZero.ftSignatureType.Should().Be(4707387510509010944);
            }
            var signCounter = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "Counter Add").FirstOrDefault();
            signCounter.Should().NotBeNull();
            if (counterAdd != string.Empty)
            {
                signCounter.Data.Should().Be(counterAdd);
            }
            else
            {
                signCounter.Data.Should().Be("0");
            }
            signCounter.ftSignatureType.Should().Be(4707387510509010944);
            var signFT = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption == "www.fiskaltrust.at").FirstOrDefault();
            signFT.Should().NotBeNull();
            signFT.ftSignatureType.Should().Be(4707387510509010945);
            if (check)
            {
                var signPr = result.ReceiptResponse.ftSignatures.ToList().Where(x => x.Caption.Contains("Prüfen")).FirstOrDefault();
                signPr.Should().NotBeNull();
                signPr.Data.Should().Contain("ActionJournalId");
            }
        }
    }
}
