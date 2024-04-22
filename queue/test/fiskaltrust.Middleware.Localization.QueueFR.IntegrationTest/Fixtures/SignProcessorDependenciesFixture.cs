using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures
{
    public sealed class SignProcessorDependenciesFixture
    {
        public Guid CASHBOXIDENTIFICATION => Guid.Parse("ddffc471-b101-4b89-8761-dd3c7f779f7c");
        public Guid CASHBOXID => Guid.Parse("fb1b79e2-f269-4fc0-9065-4821fed073d0");
        public Guid QUEUEID => Guid.Parse("b00f3da1-5a6e-4a2d-8fdf-6c3d8900d2c1");
        public SignatureFactoryFR signatureFactoryFR = new SignatureFactoryFR(new CryptoHelper());
        public ftQueue queue { get; private set; }
        public ftQueueFR queueFR { get; private set; }
        public ftSignaturCreationUnitFR signaturCreationUnitFR { get; private set; }
        public static string terminalID = "369a013a-37e2-4c23-8614-6a8f282e6330";

        private readonly Guid _signaturCreationUnitFRId = Guid.Parse("3e5a8784-c39a-4f96-af35-b4964f9f314f");


        public IConfigurationRepository configurationRepository;

        public SignProcessorDependenciesFixture()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public IConfigurationRepository CreateConfigurationRepository(DateTime? startMoment = null,
            DateTime? stopMoment = null)
        {
            return Task.Run(async () =>
            {
                var repo = new InMemoryConfigurationRepository();
                queue = new ftQueue
                {
                    ftCashBoxId = CASHBOXID,
                    ftQueueId = QUEUEID,
                    ftReceiptNumerator = 10,
                    ftQueuedRow = 1200,
                    StartMoment = startMoment,
                    StopMoment = stopMoment
                };
                queueFR = new ftQueueFR()
                {
                    ftQueueFRId = QUEUEID,
                    ftSignaturCreationUnitFRId = _signaturCreationUnitFRId,
                    CashBoxIdentification = CASHBOXIDENTIFICATION.ToString(),
                };
                signaturCreationUnitFR = new ftSignaturCreationUnitFR
                {
                    ftSignaturCreationUnitFRId = _signaturCreationUnitFRId,
                    Siret = "12345",
                    CertificateSerialNumber = "67890",
                    PrivateKey = "BesdtJZF4EWlK7YdIfIZu3etdTlxfhvrxsv47QzeYMYRFN18B8RZO/M8IbGgFKnAFypCPexupvFix8Xop7QgdQ==",
                    CertificateBase64 = "MIIDqjCCApKgAwIBAgIICNqxEmA4xicwDQYJKoZIhvcNAQELBQAwga4xDzANBgNVBAYTBkZyYW5jZTEoMCYGA1UEAwwfZmlza2FsdHJ1c3QgU0FTIFNhbmRib3ggUm9vdCBDQTEgMB4GA1UECgwXZmlza2FsdHJ1c3QgU0FTIFNhbmRib3gxDjAMBgNVBAcMBVBhcmlzMSUwIwYJKoZIhvcNAQkBFhZjb250YWN0QGZpc2thbHRydXN0LmZyMRgwFgYDVQQLDA8wMDAwMDAwMDAwMDAwMDAwHhcNMjIxMDE4MDAwMDAwWhcNNDIxMDE4MDAwMDAwWjA7MSAwHgYDVQQKDBdfZGV2X3Bvc2NyZWF0b3JfY29tcGFueTEXMBUGA1UEAwwOMTIzNDU2Nzg5MTIzNDUwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAAReOFmn26Cki+Xsfl+AzQ499WGTY/iS1eWan7qEi1/3Om8aBItWoEbC5PGeyiUPjSXHeCGVYOORJKNR4kuwiDo4o4IBBzCCAQMwgeIGA1UdIwSB2jCB14AU80"
                };
                await repo.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
                await repo.InsertOrUpdateQueueFRAsync(queueFR).ConfigureAwait(false);
                await repo.InsertOrUpdateSignaturCreationUnitFRAsync(signaturCreationUnitFR).ConfigureAwait(false);

                return repo;
            }).Result;
        }
        public void CheckResponse(ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse receiptResponse, ftJournalFR journalFR, bool signature = true)
        {
            Assert.NotNull(receiptResponse);
            Assert.Equal(receiptResponse.cbReceiptReference, request.cbReceiptReference);
            Assert.Equal(receiptResponse.ftCashBoxID, request.ftCashBoxID);
            Assert.Equal(receiptResponse.ftQueueID, request.ftQueueID);
            Assert.Equal(receiptResponse.ftCashBoxIdentification, queueFR.CashBoxIdentification);
            if (signature)
            {
                receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "www.fiskaltrust.fr");
                Assert.NotNull(journalFR);
                journalFR.ftQueueId.Should().Be(queue.ftQueueId);
                journalFR.ftQueueItemId.Should().Be(queueItem.ftQueueItemId);
                journalFR.Number.Should().Be(11);
            }
        }

        public (ReceiptRequest, ftQueueItem) CreateReceiptRequest(long ftReceiptCase = 0x4652000000000001, long ftChargeItemCase = 0x4652000000000001, bool noPayItem = false, bool noChargeItem = false)
        {
            var request = new ReceiptRequest
            {
                ftReceiptCase = ftReceiptCase,
                cbChargeItems = noChargeItem ? null : new[]
                {
                    new ChargeItem
                    {
                        Amount = 10,
                        VATRate = 20.0m,
                        ftChargeItemCase = ftChargeItemCase
                    }
                },
                cbPayItems = noPayItem ? null : new[]
                {
                    new PayItem
                    {
                        Amount = 10,
                        ftPayItemCase = 0x4652000000000001
                    }
                },
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = "123456789",
                cbTerminalID = terminalID,
                ftQueueID = QUEUEID.ToString(),
                ftCashBoxID = CASHBOXID.ToString()
            };

            var queueItem = new ftQueueItem
            {
                cbReceiptMoment = request.cbReceiptMoment,
                cbReceiptReference = request.cbReceiptReference,
                cbTerminalID = request.cbTerminalID,
                country = "FR",
                ftQueueId = QUEUEID,
                ftQueueItemId = Guid.NewGuid(),
                ftQueueRow = 1,
                request = JsonConvert.SerializeObject(request),
                requestHash = "test request hash"
            };
            return (request, queueItem);
        }

        public static void CheckTraining(ReceiptResponse receiptResponse)
        {
            "ftA#X1".Should().Be(receiptResponse.ftReceiptIdentification);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "mode école");
            receiptResponse.ftReceiptFooter.Where(x => x == "T R A I N I N G").Should().NotBeEmpty();
        }
    }
}
