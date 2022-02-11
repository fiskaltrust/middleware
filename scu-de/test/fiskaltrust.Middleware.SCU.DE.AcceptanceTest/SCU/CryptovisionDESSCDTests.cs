using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Cms;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.AcceptanceTest.SCU
{
    [Collection("CryptovisionDESSCDTests")]
    public class CryptovisionDESSCDTests : IDESSCDTests, IDisposable
    {
        private IDESSCD _instance = null;

        private const string _DEVICE_PATH = "e:";

        protected override string TseClientId => "POS001";

        protected override bool CompleteResetIsPossible => true;

        protected override TseInfo ExpectedInitializedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0000-2020",
            PublicKeyBase64 = "BIgvEK+aphlQlnRGm/cZBZHH1VqWColNMiXO33vvak801pdC6AFbrpqULCdllcYypxxrWziIt7isn78ffardC2s=",
            SerialNumberOctet = "4A3F03A2DEC81878B432548668F603D14F7B7F90D230E30C87C1A705DCE1C890",
            SignatureAlgorithm = "ecdsa-plain-SHA256",
            MaxNumberOfStartedTransactions = 512,
            MaxNumberOfSignatures = 0,
            MaxNumberOfClients = 128,
            MaxLogMemorySize = 2751462912,
            LogTimeFormat = "unixTime",
            FirmwareIdentification = "240346 Mar 30 2020 21:47:01",
            CurrentLogMemorySize = 2923,
            CurrentNumberOfStartedTransactions = 0,
            CurrentNumberOfSignatures = 17,
            CertificatesBase64 = new List<string>
            {
                "MIICpTCCAiugAwIBAgIQPKsK8TdDwQqVsinbgDwc0DAKBggqhkjOPQQDAzBUMQswCQYDVQQGEwJERTEVMBMGA1UEChMMRC1UcnVzdCBHbWJIMS4wLAYDVQQDEyVELVRSVVNUIFRJTSBIVyBTaWduaW5nIFRlc3QgQ0EgMSAyMDE5MB4XDTE5MTAyNTEzNDkyOFoXDTI1MTAzMDE0NDkyOFowbzELMAkGA1UEBhMCREUxSTBHBgNVBAMTQEI3QTZBQ0I1Q0IxOEUzODFBNUZBQzFDMkI2RUY4MDI3NzE3MjU3QUFCQTBCODdGMzQ3RjMxRTUxMTczNjNCMDcxFTATBgNVBAUTDENTTTAwOTM2MzYxMDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABIgvEK+aphlQlnRGm/cZBZHH1VqWColNMiXO33vvak801pdC6AFbrpqULCdllcYypxxrWziIt7isn78ffardC2ujgcMwgcAwFwYDVR0gBBAwDjAMBgorBgEEAaU0AgICMFUGA1UdHwROMEwwSqBIoEaGRGh0dHA6Ly9jcmwuZC10cnVzdC5uZXQvY3JsL2QtdHJ1c3RfdGltX2h3X3NpZ25pbmdfdGVzdF9jYV8xXzIwMTkuY3JsMB8GA1UdIwQYMBaAFLBLx32PU21BrS4pYaflzQ+bFxMoMA4GA1UdDwEB/wQEAwIHgDAdBgNVHQ4EFgQUCck9n7qassVVHVhcnFQ7fFXxZZYwCgYIKoZIzj0EAwMDaAAwZQIwEFQdoWpiQf2WR7avECC3IiRfAf4OH/lfVfCZlCYFhrzpy4U8ApUdwfGAD8YxEqwlAjEAvLHpZmts1Gd6gquPk5iAXwUIVTKUj6MKp6+Gxi1pnHbP2KtQKDg/ZBRHZcgg007v",
                "MIICrjCCAjWgAwIBAgIQByEL64YmQN4Y5bu5DTi9UDAKBggqhkjOPQQDAzBOMQswCQYDVQQGEwJERTEVMBMGA1UEChMMRC1UcnVzdCBHbWJIMSgwJgYDVQQDEx9ELVRSVVNUIFRJTSBSb290IFRlc3QgQ0EgMSAyMDE5MB4XDTE5MDkxNjEyNDAwMFoXDTMxMDkxNjEyMzkwMFowVDELMAkGA1UEBhMCREUxFTATBgNVBAoTDEQtVHJ1c3QgR21iSDEuMCwGA1UEAxMlRC1UUlVTVCBUSU0gSFcgU2lnbmluZyBUZXN0IENBIDEgMjAxOTB2MBAGByqGSM49AgEGBSuBBAAiA2IABGxzZ9jaeiUmLIi1VwrcvxAyjHO1sNC5DoGLKXidjMjWOOi/1WkCkapMWl0yPY3uHRpmRytzgKPj+l+c/eV2kOCYqXJXXugrvyWTrU6kX6R8uXFWYwHr/YiEMCqHH8B4lKOB0TCBzjASBgNVHRMBAf8ECDAGAQH/AgEAMB0GA1UdDgQWBBSwS8d9j1NtQa0uKWGn5c0PmxcTKDAXBgNVHSAEEDAOMAwGCisGAQQBpTQCAgIwHwYDVR0jBBgwFoAUyY0ZU8HqJEOmCTqipVRtsarxO/UwDgYDVR0PAQH/BAQDAgEGME8GA1UdHwRIMEYwRKBCoECGPmh0dHA6Ly9jcmwuZC10cnVzdC5uZXQvY3JsL2QtdHJ1c3RfdGltX3Jvb3RfdGVzdF9jYV8xXzIwMTkuY3JsMAoGCCqGSM49BAMDA2cAMGQCMGmKjmnPueqovRgwNfsiDyZ5LjtusbNhKIKVJqlzdjHKuGSPVW+wgQzhUMKcS2OCnAIwdWB9EVSfnaw5uKElQCnvoazn3/oMwH1oOm114KOG2dJE+v7RAxv9xgUe8szE4qFg",
                "MIIChDCCAgugAwIBAgIQFSnzUAbmn1IWsGN8//5MjTAKBggqhkjOPQQDAzBOMQswCQYDVQQGEwJERTEVMBMGA1UEChMMRC1UcnVzdCBHbWJIMSgwJgYDVQQDEx9ELVRSVVNUIFRJTSBSb290IFRlc3QgQ0EgMSAyMDE5MB4XDTE5MDkxNjA4MTAwMFoXDTM0MDkxNjA4MDkwMFowTjELMAkGA1UEBhMCREUxFTATBgNVBAoTDEQtVHJ1c3QgR21iSDEoMCYGA1UEAxMfRC1UUlVTVCBUSU0gUm9vdCBUZXN0IENBIDEgMjAxOTB2MBAGByqGSM49AgEGBSuBBAAiA2IABLW4XSV9Y9IYo4dQ6FCBp3vKytYK/1NiFjcoh5YSGYxGuTBtGlYRratQ7og7al2WWOOVcCbABDkHiboTuU0nQc+bF4U/wdcbO6YEU/EtKs7F9ASyh45CaMHi8dVc+tO1d6OBrTCBqjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTJjRlTweokQ6YJOqKlVG2xqvE79TAXBgNVHSAEEDAOMAwGCisGAQQBpTQCAgIwDgYDVR0PAQH/BAQDAgEGME8GA1UdHwRIMEYwRKBCoECGPmh0dHA6Ly9jcmwuZC10cnVzdC5uZXQvY3JsL2QtdHJ1c3RfdGltX3Jvb3RfdGVzdF9jYV8xXzIwMTkuY3JsMAoGCCqGSM49BAMDA2cAMGQCMFbv76axPZybv7efvTwwKdYqV6NCEM8Sjy8VeINQYyMI7V1mo/KQT2ZDR5uAxCzTigIwAuHRNzE76V6FnKS3F5MMg1SZXBGmk7RYpVU5nhP29xb+yJ0VeAIkzL25Cv8QtJSk",
            },
            CurrentClientIds = new List<string>
            {
                TseClientId
            },
            CurrentNumberOfClients = 0,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Initialized
        };

        protected override TseInfo ExpectedUninitializedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0000-2020",
            PublicKeyBase64 = null,
            SerialNumberOctet = null,
            SignatureAlgorithm = null,
            MaxNumberOfStartedTransactions = 0,
            MaxNumberOfSignatures = 0,
            MaxNumberOfClients = 0,
            MaxLogMemorySize = 2751462912,
            LogTimeFormat = null,
            FirmwareIdentification = "240346 Mar 30 2020 21:47:01",
            CurrentLogMemorySize = 0,
            CurrentNumberOfStartedTransactions = 0,
            CurrentNumberOfSignatures = 0,
            CertificatesBase64 = new List<string>(),
            CurrentClientIds = new List<string>(),
            CurrentNumberOfClients = 0,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Uninitialized
        };

        protected override TseInfo ExpectedTermiantedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0000-2020",
            PublicKeyBase64 = null,
            SerialNumberOctet = "4A3F03A2DEC81878B432548668F603D14F7B7F90D230E30C87C1A705DCE1C890",
            SignatureAlgorithm = null,
            MaxNumberOfStartedTransactions = 0,
            MaxNumberOfSignatures = 0,
            MaxNumberOfClients = 0,
            MaxLogMemorySize = 2751462912,
            LogTimeFormat = null,
            FirmwareIdentification = "240346 Mar 30 2020 21:47:01",
            CurrentLogMemorySize = 3952,
            CurrentNumberOfStartedTransactions = 0,
            CurrentNumberOfSignatures = 0,
            CertificatesBase64 = new List<string>
            {
                "MIICpTCCAiugAwIBAgIQPKsK8TdDwQqVsinbgDwc0DAKBggqhkjOPQQDAzBUMQswCQYDVQQGEwJERTEVMBMGA1UEChMMRC1UcnVzdCBHbWJIMS4wLAYDVQQDEyVELVRSVVNUIFRJTSBIVyBTaWduaW5nIFRlc3QgQ0EgMSAyMDE5MB4XDTE5MTAyNTEzNDkyOFoXDTI1MTAzMDE0NDkyOFowbzELMAkGA1UEBhMCREUxSTBHBgNVBAMTQEI3QTZBQ0I1Q0IxOEUzODFBNUZBQzFDMkI2RUY4MDI3NzE3MjU3QUFCQTBCODdGMzQ3RjMxRTUxMTczNjNCMDcxFTATBgNVBAUTDENTTTAwOTM2MzYxMDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABIgvEK+aphlQlnRGm/cZBZHH1VqWColNMiXO33vvak801pdC6AFbrpqULCdllcYypxxrWziIt7isn78ffardC2ujgcMwgcAwFwYDVR0gBBAwDjAMBgorBgEEAaU0AgICMFUGA1UdHwROMEwwSqBIoEaGRGh0dHA6Ly9jcmwuZC10cnVzdC5uZXQvY3JsL2QtdHJ1c3RfdGltX2h3X3NpZ25pbmdfdGVzdF9jYV8xXzIwMTkuY3JsMB8GA1UdIwQYMBaAFLBLx32PU21BrS4pYaflzQ+bFxMoMA4GA1UdDwEB/wQEAwIHgDAdBgNVHQ4EFgQUCck9n7qassVVHVhcnFQ7fFXxZZYwCgYIKoZIzj0EAwMDaAAwZQIwEFQdoWpiQf2WR7avECC3IiRfAf4OH/lfVfCZlCYFhrzpy4U8ApUdwfGAD8YxEqwlAjEAvLHpZmts1Gd6gquPk5iAXwUIVTKUj6MKp6+Gxi1pnHbP2KtQKDg/ZBRHZcgg007v",
                "MIICrjCCAjWgAwIBAgIQByEL64YmQN4Y5bu5DTi9UDAKBggqhkjOPQQDAzBOMQswCQYDVQQGEwJERTEVMBMGA1UEChMMRC1UcnVzdCBHbWJIMSgwJgYDVQQDEx9ELVRSVVNUIFRJTSBSb290IFRlc3QgQ0EgMSAyMDE5MB4XDTE5MDkxNjEyNDAwMFoXDTMxMDkxNjEyMzkwMFowVDELMAkGA1UEBhMCREUxFTATBgNVBAoTDEQtVHJ1c3QgR21iSDEuMCwGA1UEAxMlRC1UUlVTVCBUSU0gSFcgU2lnbmluZyBUZXN0IENBIDEgMjAxOTB2MBAGByqGSM49AgEGBSuBBAAiA2IABGxzZ9jaeiUmLIi1VwrcvxAyjHO1sNC5DoGLKXidjMjWOOi/1WkCkapMWl0yPY3uHRpmRytzgKPj+l+c/eV2kOCYqXJXXugrvyWTrU6kX6R8uXFWYwHr/YiEMCqHH8B4lKOB0TCBzjASBgNVHRMBAf8ECDAGAQH/AgEAMB0GA1UdDgQWBBSwS8d9j1NtQa0uKWGn5c0PmxcTKDAXBgNVHSAEEDAOMAwGCisGAQQBpTQCAgIwHwYDVR0jBBgwFoAUyY0ZU8HqJEOmCTqipVRtsarxO/UwDgYDVR0PAQH/BAQDAgEGME8GA1UdHwRIMEYwRKBCoECGPmh0dHA6Ly9jcmwuZC10cnVzdC5uZXQvY3JsL2QtdHJ1c3RfdGltX3Jvb3RfdGVzdF9jYV8xXzIwMTkuY3JsMAoGCCqGSM49BAMDA2cAMGQCMGmKjmnPueqovRgwNfsiDyZ5LjtusbNhKIKVJqlzdjHKuGSPVW+wgQzhUMKcS2OCnAIwdWB9EVSfnaw5uKElQCnvoazn3/oMwH1oOm114KOG2dJE+v7RAxv9xgUe8szE4qFg",
                "MIIChDCCAgugAwIBAgIQFSnzUAbmn1IWsGN8//5MjTAKBggqhkjOPQQDAzBOMQswCQYDVQQGEwJERTEVMBMGA1UEChMMRC1UcnVzdCBHbWJIMSgwJgYDVQQDEx9ELVRSVVNUIFRJTSBSb290IFRlc3QgQ0EgMSAyMDE5MB4XDTE5MDkxNjA4MTAwMFoXDTM0MDkxNjA4MDkwMFowTjELMAkGA1UEBhMCREUxFTATBgNVBAoTDEQtVHJ1c3QgR21iSDEoMCYGA1UEAxMfRC1UUlVTVCBUSU0gUm9vdCBUZXN0IENBIDEgMjAxOTB2MBAGByqGSM49AgEGBSuBBAAiA2IABLW4XSV9Y9IYo4dQ6FCBp3vKytYK/1NiFjcoh5YSGYxGuTBtGlYRratQ7og7al2WWOOVcCbABDkHiboTuU0nQc+bF4U/wdcbO6YEU/EtKs7F9ASyh45CaMHi8dVc+tO1d6OBrTCBqjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTJjRlTweokQ6YJOqKlVG2xqvE79TAXBgNVHSAEEDAOMAwGCisGAQQBpTQCAgIwDgYDVR0PAQH/BAQDAgEGME8GA1UdHwRIMEYwRKBCoECGPmh0dHA6Ly9jcmwuZC10cnVzdC5uZXQvY3JsL2QtdHJ1c3RfdGltX3Jvb3RfdGVzdF9jYV8xXzIwMTkuY3JsMAoGCCqGSM49BAMDA2cAMGQCMFbv76axPZybv7efvTwwKdYqV6NCEM8Sjy8VeINQYyMI7V1mo/KQT2ZDR5uAxCzTigIwAuHRNzE76V6FnKS3F5MMg1SZXBGmk7RYpVU5nhP29xb+yJ0VeAIkzL25Cv8QtJSk",
            },
            CurrentClientIds = new List<string>
            {
                TseClientId
            },
            CurrentNumberOfClients = 0,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Terminated
        };

        protected override IDESSCD GetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            if (_instance != null && _instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (configuration == null)
            {
                configuration = new Dictionary<string, object>() { { "devicePath", _DEVICE_PATH } };
            }
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var scuBootStrapper = new CryptoVision.ScuBootstrapper
            {
                Configuration = configuration
            };
            scuBootStrapper.ConfigureServices(serviceCollection);
            _instance = serviceCollection.BuildServiceProvider().GetService<IDESSCD>();
            return _instance;
        }

        protected override IDESSCD GetResetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            var infoLog = "";
            var errorLog = "";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-jar factoryReset.jar",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = "tools/CryptoVision"
                }
            };
            process.Start();
            process.OutputDataReceived += (_, e) => infoLog += e.Data;
            process.ErrorDataReceived += (_, e) => errorLog += e.Data;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception(errorLog);
            }
            return GetSystemUnderTest(configuration);
        }

        public void Dispose()
        {
            if (_instance is IDisposable disposable)
            {
                disposable.Dispose();
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            }
        }

        [Fact]
        public async Task RegisterClientIdAsync_ShouldReturnArrayIncludingClient_IfRegistered_SpecialClient()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });

            var result = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = "ftrbRhgLpPiEuxrbVr8aD54A"
            });
            result.ClientIds.Should().BeEquivalentTo(new List<string>
            {
                "ftrbRhgLpPiEuxrbVr8aD54A"
            });
        }
    }
}