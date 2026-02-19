using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Asn1.Ocsp;
using Xunit;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.IntegrationTest
{
    [Collection("SwissbitCloudV2Tests")]
    public class SwissbitCloudV2Tests : IClassFixture<SwissbitCloudV2Fixture>
    {
        private readonly SwissbitCloudV2Fixture _testFixture;

        public SwissbitCloudV2Tests(SwissbitCloudV2Fixture testFixture)
        {
            _testFixture = testFixture;
        }
                
        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = await _testFixture.GetSut();

            var request = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureBase64.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(_testFixture.TestClientId.ToString());

            await sut.FinishTransactionAsync(CreateFinishTransactionRequest(result.TransactionNumber, request.ClientId));
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Fail_Because_ClientNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var ClientId = Guid.NewGuid().ToString();
            var request = CreateStartTransactionRequest(ClientId);
            var result = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await result.Should().ThrowAsync<Exception>().WithMessage($"The client {ClientId} is not registered.");
        }


        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task RegisterAndUnregisterClientIdAsync_Should_Return_ValidData()
        {
            var sut = await _testFixture.GetSut();

            var ClientId = Guid.NewGuid().ToString().Replace("-", "").Remove(30);
            
            var registerdClients = await sut.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = ClientId });
            registerdClients.ClientIds.Should().Contain(ClientId);

            var unregisterdClients = await sut.UnregisterClientIdAsync(new UnregisterClientIdRequest { ClientId = ClientId });

            unregisterdClients.ClientIds.Should().NotContain(ClientId);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Not_Increment_TransactionNumber_And_Increment_SignatureCounter()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var updateResult = await sut.UpdateTransactionAsync(updateRequest);

            updateResult.Should().NotBeNull();
            updateResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            updateResult.SignatureData.Should().NotBeNull();
            updateResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            updateResult.SignatureData.SignatureBase64.Should().NotBeNull();
            updateResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            updateResult.ProcessDataBase64.Should().BeEquivalentTo(updateRequest.ProcessDataBase64);
            updateResult.ProcessType.Should().Be(updateRequest.ProcessType);

            await sut.FinishTransactionAsync(CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId));
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var clientId = Guid.NewGuid().ToString();
            var request = CreateUpdateTransactionRequest(0, clientId);
            var action = new Func<Task>(async () => await sut.UpdateTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {clientId} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.SignatureData.SignatureBase64.Should().NotBeNull();
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);

            var tseInfo = await sut.GetTseInfoAsync();
            startResult.TseSerialNumberOctet.Should().Be(tseInfo.SerialNumberOctet);
            finishResult.TseSerialNumberOctet.Should().Be(tseInfo.SerialNumberOctet);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartAndFinishTransactionAsync_Should_WorkFor_OrderWithoutContent()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateOrderStartTransactionRequest(_testFixture.TestClientId.ToString(), "");
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, "");
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.SignatureData.SignatureBase64.Should().NotBeNull();
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateFinishTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.FinishTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = await _testFixture.GetSut();
            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            // We do simulate a restart of the SCU. If the SCU is restarted we loose all state and so this is similar 
            // as if we recreate the sut.
            var sut2 = _testFixture.GetNewSut();
            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut2.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task GetTseInfoAsync_Should_Return_Valid_TseInfo()
        {
            var sut = await _testFixture.GetSut();

            var result = await sut.GetTseInfoAsync().ConfigureAwait(false);

            result.Should().NotBeNull();
            result.CurrentNumberOfClients.Should().BeGreaterThan(0);
            result.SerialNumberOctet.Should().NotBeNullOrEmpty();
            result.PublicKeyBase64.Should().NotBeNullOrEmpty();
            result.MaxNumberOfClients.Should().BeGreaterOrEqualTo(result.CurrentNumberOfClients);
            result.MaxNumberOfStartedTransactions.Should().BeGreaterOrEqualTo(result.CurrentNumberOfStartedTransactions);
            result.CertificatesBase64.Should().HaveCount(1);
            result.CurrentClientIds.Should().Contain(_testFixture.TestClientId);
            result.CurrentState.Should().Be(TseStates.Initialized);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task ExportDataAsync_Should_Return_MultipleTransactionLogs()
        {
            var sut = await _testFixture.GetSut();

            var exportSession = await sut.StartExportSessionAsync(new StartExportSessionRequest
            {
                ClientId = _testFixture.TestClientId
            });
            exportSession.Should().NotBeNull();
            using (var fileStream = File.OpenWrite($"export_{exportSession.TokenId}.tar"))
            {
                ExportDataResponse export;
                do
                {
                    export = await sut.ExportDataAsync(new ExportDataRequest
                    {
                        TokenId = exportSession.TokenId,
                        MaxChunkSize = 1024 * 1024
                    });
                    if (!export.TotalTarFileSizeAvailable)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        var allBytes = Convert.FromBase64String(export.TarFileByteChunkBase64);
                        await fileStream.WriteAsync(allBytes, 0, allBytes.Length);
                    }
                } while (!export.TarFileEndOfFile);
            }

            var endSessionRequest = new EndExportSessionRequest
            {
                TokenId = exportSession.TokenId,
                Erase = true
            };
            using (var fileStream = File.OpenRead($"export_{exportSession.TokenId}.tar"))
            {
                endSessionRequest.Sha256ChecksumBase64 = Convert.ToBase64String(SHA256.Create().ComputeHash(fileStream));
            }

            var endExportSessionResult = await sut.EndExportSessionAsync(endSessionRequest);
            endExportSessionResult.IsValid.Should().BeTrue();

            using (var fileStream = File.OpenRead($"export_{exportSession.TokenId}.tar"))
            {
                var logs = LogParser.GetLogsFromTarStream(fileStream).ToList();
                logs.Should().HaveCountGreaterThan(0);
            }
        }

        private StartTransactionRequest CreateStartTransactionRequest(string clientId)
        {
            var fixture = new Fixture();
            return new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private UpdateTransactionRequest CreateUpdateTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            return new UpdateTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private FinishTransactionRequest CreateFinishTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            return new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private StartTransactionRequest CreateOrderStartTransactionRequest(string clientId, string processDataBase64)
        {
            return new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessDataBase64 = processDataBase64,
                ProcessType = "",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private FinishTransactionRequest CreateOrderFinishTransactionRequest(ulong transactionNumber, string clientId, string processDataBase64)
        {
            return new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = processDataBase64,
                ProcessType = "Bestellung-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public void ExtractCertificatesFromChain_Should_Parse_Certificate_Chain_Correctly()
        {
            // This is the actual certificate chain format returned by SwissbitCloudV2
            var certificateChain = "-----BEGIN CERTIFICATE-----\nMIIDWDCCAt6gAwIBAgIUewYrgYKabzZzOvi+ugMIgQjAjLgwCgYIKoZIzj0EAwMw\nVjEUMBIGA1UEChMLU3dpc3NiaXQgQUcxHzAdBgNVBAsTFkVtYmVkZGVkIElvVCBT\nb2x1dGlvbnMxHTAbBgNVBAMTFFRTRS1UZXN0LUNBIFN3aXNzYml0MB4XDTI1MDQw\nNzA5MDIwN1oXDTMzMDQwNjIzNTk1OVowgYAxCzAJBgNVBAYTAkRFMUkwRwYDVQQD\nE0A2NTMyYmU4Yzk1M2M2ZGQ1YTUwOTA1ODI3MzRlZDgzNDE0M2JmZDIwNGRjZGYz\nYjVlMGNjZTg2ZWY5YzZjNmFjMSYwJAYJBAB/AAcDCgECMBcCAQETEkJTSS1LLVRS\nLTA2MTItMjAyNDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABEQ57F29an1iZs9p\n3Z7fekAUCEyCyh81ISsDHz1HRN9NcR9sup518X6wF+tsQplKG3xuz9voVaTyROqo\n4PZH5vOjggFdMIIBWTAfBgNVHSMEGDAWgBSshAcY9usz8wtKiKOdFr1JicvL8TAd\nBgNVHQ4EFgQUDwBJHam5vLrJPdvNfihZ9Q2aA2owDAYDVR0TAQH/BAIwADAOBgNV\nHQ8BAf8EBAMCB4AwKwYDVR0QBCQwIoAPMjAyNTA0MDcwOTAyMDdagQ8yMDMzMDQw\nNjIzNTk1OVowegYDVR0gBHMwcTBvBgorBgEEAYOTbwIEMGEwXwYIKwYBBQUHAgEW\nU2h0dHBzOi8vZGEtcnouZGUvZGUvdWViZXItZGFyei91bnRlcm5laG1lbi9wa2kv\ndHNlLXBraS01L3plcnRpZml6aWVydW5nc3JpY2h0bGluaWUvMFAGA1UdHwRJMEcw\nRaBDoEGGP2h0dHA6Ly90c2Utc3dpc3NiaXQtdGVzdC5kYS1yei5uZXQvY3JsL3Rz\nZS10ZXN0LWNhLXN3aXNzYml0LmNybDAKBggqhkjOPQQDAwNoADBlAjAgusgY5FII\nH1i5MBgvV6rRTotnsfMsQ2c4osNiYpxbBQXgdJq42RRmK5T7MofkkLACMQCTq8Wp\noisB0LuZoOLZoyqVmfgzFH/TKbY8RDh6EAREEdaDmVdSdJSIqIJx3Be10n4=\n-----END CERTIFICATE-----\n-----BEGIN CERTIFICATE-----\nMIIEbjCCA/OgAwIBAgIUF2j87druAH8XzBvKgXDyzIzckwEwCgYIKoZIzj0EAwMw\nWzEUMBIGA1UEChMLU3dpc3NiaXQgQUcxHzAdBgNVBAsTFkVtYmVkZGVkIElvVCBT\nb2x1dGlvbnMxIjAgBgNVBAMTGVRTRS1UZXN0LVJvb3QtQ0EgU3dpc3NiaXQwHhcN\nMjQxMjA0MTE0NTU3WhcNMzQxMjAzMjM1OTU5WjBWMRQwEgYDVQQKEwtTd2lzc2Jp\ndCBBRzEfMB0GA1UECxMWRW1iZWRkZWQgSW9UIFNvbHV0aW9uczEdMBsGA1UEAxMU\nVFNFLVRlc3QtQ0EgU3dpc3NiaXQwdjAQBgcqhkjOPQIBBgUrgQQAIgNiAASdhALr\nd124QzMprsrLI9FDy1jb/ApTvIVkd90Fwb5J85a2Q5KhaCXLh8/3mPNtYixojX+9\nBGUSuTk1nrbw4fNZKFaLIlaw9MVENEOfI8Eu/AQXPuWaLvtOu4Vyt0mtHV6jggJ7\nMIICdzAfBgNVHSMEGDAWgBSvjKbwMHNHl1+nI30dI49ahdLXpjAdBgNVHQ4EFgQU\nrIQHGPbrM/MLSoijnRa9SYnLy/EwEgYDVR0TAQH/BAgwBgEB/wIBADAOBgNVHQ8B\nAf8EBAMCAQYwKwYDVR0QBCQwIoAPMjAyNDEyMDQxMTQ1NTdagQ8yMDI2MTIwMzIz\nNTk1OVowegYDVR0gBHMwcTBvBgorBgEEAYOTbwIEMGEwXwYIKwYBBQUHAgEWU2h0\ndHBzOi8vZGEtcnouZGUvZGUvdWViZXItZGFyei91bnRlcm5laG1lbi9wa2kvdHNl\nLXBraS01L3plcnRpZml6aWVydW5nc3JpY2h0bGluaWUvMFUGA1UdHwROMEwwSqBI\noEaGRGh0dHA6Ly90c2Utc3dpc3NiaXQtdGVzdC5kYS1yei5uZXQvY3JsL3RzZS10\nZXN0LXJvb3QtY2Etc3dpc3NiaXQuY3JsMFYGA1UdEQRPME2BEFRTRS1QS0lAZGEt\ncnouZGWGOWh0dHBzOi8vZGEtcnouZGUvZGUvdWViZXItZGFyei91bnRlcm5laG1l\nbi9wa2kvdHNlLXBraS01LzBWBgNVHRIETzBNgRBUU0UtUEtJQGRhLXJ6LmRlhjlo\ndHRwczovL2RhLXJ6LmRlL2RlL3VlYmVyLWRhcnovdW50ZXJuZWhtZW4vcGtpL3Rz\nZS1wa2ktNS8wYQYIKwYBBQUHAQEEVTBTMFEGCCsGAQUFBzAChkVodHRwOi8vdHNl\nLXN3aXNzYml0LXRlc3QuZGEtcnoubmV0L2NhL3RzZS10ZXN0LXJvb3QtY2Ffc3dp\nc3NiaXRfNC5jcnQwCgYIKoZIzj0EAwMDaQAwZgIxAOqDyi16NhxWFrjclIa6Tv1j\nRr4bA0yZLQbHmRphStU6apYGc1iVz5VEP3jzh2JGEQIxAKvAKO7SPXvJRbbV01kG\n0ZPo8V8vxKCNcUZf/C+KqS7M5ognq92WXvTlWD2UmncwCA==\n-----END CERTIFICATE-----\n-----BEGIN CERTIFICATE-----\nMIIEUDCCA9egAwIBAgIUHKqnliMla+NrmWOcHVQ0PqHNDcswCgYIKoZIzj0EAwMw\nWzEUMBIGA1UEChMLU3dpc3NiaXQgQUcxHzAdBgNVBAsTFkVtYmVkZGVkIElvVCBT\nb2x1dGlvbnMxIjAgBgNVBAMTGVRTRS1UZXN0LVJvb3QtQ0EgU3dpc3NiaXQwHhcN\nMjQxMjA0MTEzNTExWhcNMzgxMjAzMjM1OTU5WjBbMRQwEgYDVQQKEwtTd2lzc2Jp\ndCBBRzEfMB0GA1UECxMWRW1iZWRkZWQgSW9UIFNvbHV0aW9uczEiMCAGA1UEAxMZ\nVFNFLVRlc3QtUm9vdC1DQSBTd2lzc2JpdDB2MBAGByqGSM49AgEGBSuBBAAiA2IA\nBFkA1MgFCSdsnPrr2XqLrcoXmcbIF5Sjvk3TqHwzI+8QtG7lJLtEW7p/0jao6t/l\nnndBvUVt8BsZ5lAgbVJ60abUmD3VQWtywTGl7F/2I0pxxXJuNlzivL5e5fUZQcm/\njaOCAlowggJWMB0GA1UdDgQWBBSvjKbwMHNHl1+nI30dI49ahdLXpjASBgNVHRMB\nAf8ECDAGAQH/AgEBMA4GA1UdDwEB/wQEAwIBBjArBgNVHRAEJDAigA8yMDI0MTIw\nNDExMzUxMVqBDzIwMjgxMjAzMjM1OTU5WjB6BgNVHSAEczBxMG8GCisGAQQBg5Nv\nAgQwYTBfBggrBgEFBQcCARZTaHR0cHM6Ly9kYS1yei5kZS9kZS91ZWJlci1kYXJ6\nL3VudGVybmVobWVuL3BraS90c2UtcGtpLTUvemVydGlmaXppZXJ1bmdzcmljaHRs\naW5pZS8wVQYDVR0fBE4wTDBKoEigRoZEaHR0cDovL3RzZS1zd2lzc2JpdC10ZXN0\nLmRhLXJ6Lm5ldC9jcmwvdHNlLXRlc3Qtcm9vdC1jYS1zd2lzc2JpdC5jcmwwVgYD\nVR0RBE8wTYEQVFNFLVBLSUBkYS1yei5kZYY5aHR0cHM6Ly9kYS1yei5kZS9kZS91\nZWJlci1kYXJ6L3VudGVybmVobWVuL3BraS90c2UtcGtpLTUvMFYGA1UdEgRPME2B\nEFRTRS1QS0lAZGEtcnouZGWGOWh0dHBzOi8vZGEtcnouZGUvZGUvdWViZXItZGFy\nei91bnRlcm5laG1lbi9wa2kvdHNlLXBraS01LzBhBggrBgEFBQcBAQRVMFMwUQYI\nKwYBBQUHMAKGRWh0dHA6Ly90c2Utc3dpc3NiaXQtdGVzdC5kYS1yei5uZXQvY2Ev\ndHNlLXRlc3Qtcm9vdC1jYV9zd2lzc2JpdF80LmNydDAKBggqhkjOPQQDAwNnADBk\nAjA1Hm3LWdzvjpzZ46CWbkDRR9QRfnvWRdYBy0NlBuY57r+HVwltjamSHYRdtqRg\nghMCMFhROCJO6U7yp1zXNwk0dfoYKv/6XvJ6mLUZSHDaW9XHnu0lw04aTzmC7Xqk\nLZZ8rw==\n-----END CERTIFICATE-----\n";

            var sut = _testFixture.GetNewSut();
            
            // Use reflection to call the private method
            var method = sut.GetType().GetMethod("ExtractCertificatesFromChain", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = method.Invoke(sut, new object[] { certificateChain }) as List<string>;

            // Should extract 3 certificates from the chain
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            
            // Each certificate should be valid base64
            foreach (var cert in result)
            {
                cert.Should().NotBeNullOrEmpty();
                var bytes = Convert.FromBase64String(cert);
                bytes.Should().NotBeEmpty();
                
                // Verify it's a valid X509 certificate
                var x509Cert = new X509Certificate2(bytes);
                x509Cert.Should().NotBeNull();
                x509Cert.Subject.Should().Contain("Swissbit");
            }
        }
    }
}
