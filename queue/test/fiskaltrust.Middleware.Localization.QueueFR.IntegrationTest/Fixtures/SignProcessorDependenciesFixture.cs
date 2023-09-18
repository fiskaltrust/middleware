using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Queue;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueFR.IntegrationTest.Fixtures
{
    public sealed class SignProcessorDependenciesFixture
    {
        public Guid CASHBOXIDENTIFICATION => Guid.Parse("ddffc471-b101-4b89-8761-dd3c7f779f7c");
        public Guid CASHBOXID => Guid.Parse("fb1b79e2-f269-4fc0-9065-4821fed073d0");
        public Guid QUEUEID => Guid.Parse("b00f3da1-5a6e-4a2d-8fdf-6c3d8900d2c1");

        private readonly Guid _signaturCreationUnitFRId = Guid.Parse("3e5a8784-c39a-4f96-af35-b4964f9f314f");


        public static string terminalID = "369a013a-37e2-4c23-8614-6a8f282e6330";

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
                await repo.InsertOrUpdateQueueAsync(new ftQueue
                {
                    ftCashBoxId = CASHBOXID,
                    ftQueueId = QUEUEID,
                    ftReceiptNumerator = 10,
                    ftQueuedRow = 1200,
                    StartMoment = startMoment,
                    StopMoment = stopMoment
                }).ConfigureAwait(false);
                await repo.InsertOrUpdateQueueFRAsync(new ftQueueFR()
                {
                    ftQueueFRId = QUEUEID,
                    ftSignaturCreationUnitFRId = _signaturCreationUnitFRId,
                    CashBoxIdentification = CASHBOXIDENTIFICATION.ToString(),
                }).ConfigureAwait(false);
                await repo.InsertOrUpdateSignaturCreationUnitFRAsync(new ftSignaturCreationUnitFR
                {
                    ftSignaturCreationUnitFRId = _signaturCreationUnitFRId,
                    Siret = "12345",
                    CertificateSerialNumber = "67890",
                    PrivateKey = "BesdtJZF4EWlK7YdIfIZu3etdTlxfhvrxsv47QzeYMYRFN18B8RZO/M8IbGgFKnAFypCPexupvFix8Xop7QgdQ==",
                    CertificateBase64 = "MIIDqjCCApKgAwIBAgIICNqxEmA4xicwDQYJKoZIhvcNAQELBQAwga4xDzANBgNVBAYTBkZyYW5jZTEoMCYGA1UEAwwfZmlza2FsdHJ1c3QgU0FTIFNhbmRib3ggUm9vdCBDQTEgMB4GA1UECgwXZmlza2FsdHJ1c3QgU0FTIFNhbmRib3gxDjAMBgNVBAcMBVBhcmlzMSUwIwYJKoZIhvcNAQkBFhZjb250YWN0QGZpc2thbHRydXN0LmZyMRgwFgYDVQQLDA8wMDAwMDAwMDAwMDAwMDAwHhcNMjIxMDE4MDAwMDAwWhcNNDIxMDE4MDAwMDAwWjA7MSAwHgYDVQQKDBdfZGV2X3Bvc2NyZWF0b3JfY29tcGFueTEXMBUGA1UEAwwOMTIzNDU2Nzg5MTIzNDUwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAAReOFmn26Cki+Xsfl+AzQ499WGTY/iS1eWan7qEi1/3Om8aBItWoEbC5PGeyiUPjSXHeCGVYOORJKNR4kuwiDo4o4IBBzCCAQMwgeIGA1UdIwSB2jCB14AU80TP+mFGheT0sx7YlBKL1sBl40ehgbSkgbEwga4xDzANBgNVBAYTBkZyYW5jZTEoMCYGA1UEAwwfZmlza2FsdHJ1c3QgU0FTIFNhbmRib3ggUm9vdCBDQTEgMB4GA1UECgwXZmlza2FsdHJ1c3QgU0FTIFNhbmRib3gxDjAMBgNVBAcMBVBhcmlzMSUwIwYJKoZIhvcNAQkBFhZjb250YWN0QGZpc2thbHRydXN0LmZyMRgwFgYDVQQLDA8wMDAwMDAwMDAwMDAwMDCCCAjXMRv3EXLvMAwGA1UdEwEB/wQCMAAwDgYDVR0PAQH/BAQDAgeAMA0GCSqGSIb3DQEBCwUAA4IBAQAEGzaSp2h0ZRUd67orh+jER1+J1RicZqPC0N85FEy7QZJ9xh8Uo4N79s3qzCs/J2hNa/lk3PlHzx3mbiqGoMQL3Zm0Q1LtshmJEL2Q94wEBqoglRfeKsPaaoILi/S6nI5bdwtMJcng7mr++fGqFkMzJrV0RXYMpueodPtTk+Zei/GD/xUiGzshJfNyI+x858rbXkfeXMzH18uuNXaGHpMb0ceX+I8h0gkZZX6rZ9tTbG9TubVaaoUSuQXHx+ZxjJYDY/8qae5vWWSQ4y5JVPyhBDSJ2vgolTLmS63agrpXMbWenu9ng5IQMFxqGfX6EDoMHYdqch1M88A0sJBmjxxm"
                }).ConfigureAwait(false);
                
                return repo;
            }).Result;
        }
    }
}
