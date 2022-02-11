using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using FluentAssertions.Execution;
using fiskaltrust.ifPOS.v1;
using AutoFixture;
using System.Linq;
using FluentAssertions.Extensions;
using System.IO;
using System.Security.Cryptography;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using Org.BouncyCastle.Crypto.Agreement;

namespace fiskaltrust.Middleware.SCU.DE.AcceptanceTest
{
    public abstract class IDESSCDTests
    {
        protected abstract IDESSCD GetSystemUnderTest(Dictionary<string, object> configuration = null);

        protected abstract IDESSCD GetResetSystemUnderTest(Dictionary<string, object> configuration = null);

        protected virtual ulong SignaturCounterOffset { get; } = 0;

        protected virtual string TseClientId { get; }

        protected virtual TseInfo ExpectedInitializedTseInfo { get; }

        protected virtual TseInfo ExpectedUninitializedTseInfo { get; }

        protected virtual TseInfo ExpectedTermiantedTseInfo { get; }

        protected virtual bool CompleteResetIsPossible => false;

        [Fact]
        public void CreateInstance()
        {
            var desscd = GetSystemUnderTest();
            desscd.Should().NotBeNull();
        }

        [Fact]
        public async Task Echo()
        {
            var scu = GetSystemUnderTest();

            var Message = "try to use \"echo\"";

            var response = await scu.EchoAsync(new ScuDeEchoRequest { Message = Message });
            response.Message.Should().Be(Message);

        }

        [Fact]
        public async Task ExecuteSelfTest()
        {
            var scu = GetSystemUnderTest();
            await scu.ExecuteSelfTestAsync();
        }

        [Fact]
        public async Task ExecuteSetTseTime()
        {
            var scu = GetSystemUnderTest();
            await scu.ExecuteSetTseTimeAsync();
        }

        [Fact]
        public async Task GetTseInfoAsync_ForInitializedDevice()
        {
            var scu = GetResetSystemUnderTest();
            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });
           await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });
            var tseInfo = await scu.GetTseInfoAsync();
            using (new AssertionScope())
            {
                tseInfo.MaxNumberOfClients.Should().Be(ExpectedInitializedTseInfo.MaxNumberOfClients);
                tseInfo.MaxNumberOfStartedTransactions.Should().Be(ExpectedInitializedTseInfo.MaxNumberOfStartedTransactions);
                tseInfo.MaxNumberOfSignatures.Should().Be(ExpectedInitializedTseInfo.MaxNumberOfSignatures);
                tseInfo.MaxLogMemorySize.Should().Be(ExpectedInitializedTseInfo.MaxLogMemorySize);
                tseInfo.FirmwareIdentification.Should().Be(ExpectedInitializedTseInfo.FirmwareIdentification);
                tseInfo.CertificationIdentification.Should().Be(ExpectedInitializedTseInfo.CertificationIdentification);
                tseInfo.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                tseInfo.LogTimeFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                tseInfo.SerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                tseInfo.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                tseInfo.CertificatesBase64.Should().BeEquivalentTo(ExpectedInitializedTseInfo.CertificatesBase64);
                if (CompleteResetIsPossible)
                {
                    tseInfo.CurrentLogMemorySize.Should().Be(ExpectedInitializedTseInfo.CurrentLogMemorySize);
                    tseInfo.CurrentNumberOfSignatures.Should().Be(ExpectedInitializedTseInfo.CurrentNumberOfSignatures);
                }
                else
                {
                    tseInfo.CurrentLogMemorySize.Should().BeGreaterThan(0);
                    tseInfo.CurrentNumberOfSignatures.Should().BeGreaterThan(0);
                }
                tseInfo.CurrentNumberOfStartedTransactions.Should().Be(ExpectedInitializedTseInfo.CurrentNumberOfStartedTransactions);
                tseInfo.CurrentState.Should().Be(ExpectedInitializedTseInfo.CurrentState);
                tseInfo.CurrentNumberOfClients.Should().Be(ExpectedInitializedTseInfo.CurrentNumberOfClients);
                tseInfo.CurrentClientIds.Should().BeEquivalentTo(ExpectedInitializedTseInfo.CurrentClientIds);
                tseInfo.Info.Should().BeNull();
            }
        }

        [Fact]
        public async Task GetTseInfoAsync_ForUninitializedDevice()
        {
            var scu = GetResetSystemUnderTest();
            var tseInfo = await scu.GetTseInfoAsync();
            using (new AssertionScope())
            {
                tseInfo.MaxNumberOfClients.Should().Be(ExpectedUninitializedTseInfo.MaxNumberOfClients);
                tseInfo.MaxNumberOfStartedTransactions.Should().Be(ExpectedUninitializedTseInfo.MaxNumberOfStartedTransactions);
                tseInfo.MaxNumberOfSignatures.Should().Be(ExpectedUninitializedTseInfo.MaxNumberOfSignatures);
                tseInfo.MaxLogMemorySize.Should().Be(ExpectedUninitializedTseInfo.MaxLogMemorySize);
                tseInfo.FirmwareIdentification.Should().Be(ExpectedUninitializedTseInfo.FirmwareIdentification);
                tseInfo.CertificationIdentification.Should().Be(ExpectedUninitializedTseInfo.CertificationIdentification);
                tseInfo.SignatureAlgorithm.Should().Be(ExpectedUninitializedTseInfo.SignatureAlgorithm);
                tseInfo.LogTimeFormat.Should().Be(ExpectedUninitializedTseInfo.LogTimeFormat);
                tseInfo.SerialNumberOctet.Should().Be(ExpectedUninitializedTseInfo.SerialNumberOctet);
                tseInfo.PublicKeyBase64.Should().Be(ExpectedUninitializedTseInfo.PublicKeyBase64);
                tseInfo.CertificatesBase64.Should().BeEquivalentTo(ExpectedUninitializedTseInfo.CertificatesBase64);
                tseInfo.CurrentLogMemorySize.Should().Be(ExpectedUninitializedTseInfo.CurrentLogMemorySize);
                tseInfo.CurrentNumberOfStartedTransactions.Should().Be(ExpectedUninitializedTseInfo.CurrentNumberOfStartedTransactions);
                tseInfo.CurrentNumberOfSignatures.Should().Be(ExpectedUninitializedTseInfo.CurrentNumberOfSignatures);
                tseInfo.CurrentState.Should().Be(ExpectedUninitializedTseInfo.CurrentState);
                tseInfo.CurrentNumberOfClients.Should().Be(ExpectedUninitializedTseInfo.CurrentNumberOfClients);
                tseInfo.CurrentClientIds.Should().BeEquivalentTo(ExpectedUninitializedTseInfo.CurrentClientIds);
            }
        }

        [Fact]
        public async Task GetTseInfoAsync_ForTerminatedDevice()
        {
            var scu = GetResetSystemUnderTest();
            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Initialized
            });
            await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });
            await scu.SetTseStateAsync(new TseState
            {
                CurrentState = TseStates.Terminated
            });
            var tseInfo = await scu.GetTseInfoAsync();
            using (new AssertionScope())
            {
                tseInfo.MaxNumberOfClients.Should().Be(ExpectedTermiantedTseInfo.MaxNumberOfClients);
                tseInfo.MaxNumberOfStartedTransactions.Should().Be(ExpectedTermiantedTseInfo.MaxNumberOfStartedTransactions);
                tseInfo.MaxNumberOfSignatures.Should().Be(ExpectedTermiantedTseInfo.MaxNumberOfSignatures);
                tseInfo.MaxLogMemorySize.Should().Be(ExpectedTermiantedTseInfo.MaxLogMemorySize);
                tseInfo.FirmwareIdentification.Should().Be(ExpectedTermiantedTseInfo.FirmwareIdentification);
                tseInfo.CertificationIdentification.Should().Be(ExpectedTermiantedTseInfo.CertificationIdentification);
                tseInfo.SignatureAlgorithm.Should().Be(ExpectedTermiantedTseInfo.SignatureAlgorithm);
                tseInfo.LogTimeFormat.Should().Be(ExpectedTermiantedTseInfo.LogTimeFormat);
                tseInfo.SerialNumberOctet.Should().Be(ExpectedTermiantedTseInfo.SerialNumberOctet);
                tseInfo.PublicKeyBase64.Should().Be(ExpectedTermiantedTseInfo.PublicKeyBase64);
                tseInfo.CertificatesBase64.Should().BeEquivalentTo(ExpectedTermiantedTseInfo.CertificatesBase64);
                tseInfo.CurrentLogMemorySize.Should().Be(ExpectedTermiantedTseInfo.CurrentLogMemorySize);
                tseInfo.CurrentNumberOfStartedTransactions.Should().Be(ExpectedTermiantedTseInfo.CurrentNumberOfStartedTransactions);
                tseInfo.CurrentNumberOfSignatures.Should().Be(ExpectedTermiantedTseInfo.CurrentNumberOfSignatures);
                tseInfo.CurrentState.Should().Be(ExpectedTermiantedTseInfo.CurrentState);
                tseInfo.CurrentNumberOfClients.Should().Be(ExpectedTermiantedTseInfo.CurrentNumberOfClients);
                tseInfo.CurrentClientIds.Should().BeEquivalentTo(ExpectedTermiantedTseInfo.CurrentClientIds);
                tseInfo.Info.Should().BeNull();
            }
        }

        [Fact]
        public async Task SetTseStateAsync_ToInitialized_ShouldReturnInitialized()
        {
            var scu = GetResetSystemUnderTest();
            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            tseState.CurrentState.Should().Be(TseStates.Initialized);
        }

        [Fact]
        public async Task SetTseStateAsync_ToInitialized_Twice_ShouldReturnInitialized()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            tseState.CurrentState.Should().Be(TseStates.Initialized);
        }

        [Fact]
        public async Task SetTseStateAsync_ToInitialized_AfterTerminated_ShouldFail()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });

            Func<Task> action = async () => await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            await action.Should().ThrowAsync<ScuException>();
        }

        [Fact]
        public async Task SetTseStateAsync_ToUninitialized_ShouldReturnUninitialized()
        {
            var scu = GetResetSystemUnderTest();
            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Uninitialized });
            tseState.CurrentState.Should().Be(TseStates.Uninitialized);
        }

        [Fact]
        public async Task SetTseStateAsync_ToUninitialized_AfterInitialized_ShouldIgnoreUnintialized()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Uninitialized });
            tseState.CurrentState.Should().Be(TseStates.Initialized);
        }

        [Fact]
        public async Task SetTseStateAsync_ToUninitialized_AfterTerminated_ShouldIgnoreUnintialized()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });
            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Uninitialized });
            tseState.CurrentState.Should().Be(TseStates.Terminated);
        }

        [Fact]
        public async Task SetTseStateAsync_Terminated()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });

            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });
            tseState.CurrentState.Should().Be(TseStates.Terminated);
        }

        [Fact]
        public async Task SetTseStateAsync_ToTerminated_Twice_ShouldReturnInitialized()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });
            var tseState = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });
            tseState.CurrentState.Should().Be(TseStates.Terminated);
        }

        [Fact]
        public async Task RegisterClientIdAsync_ShouldReturnArrayIncludingClient_IfRegistered()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });

            var result = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });
            result.ClientIds.Should().BeEquivalentTo(new List<string>
            {
                TseClientId
            });
        }

        [Fact]
        public async Task RegisterClientIdAsync_AndAnotherClient_ShouldReturnArrayIncludingBothClient_IfRegistered()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });

            var secondId = "POS002";
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });
            var result = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = secondId
            });
            result.ClientIds.Should().BeEquivalentTo(new List<string>
            {
                TseClientId,
                secondId
            });
        }

        [Fact]
        public async Task RegisterClientIdAsync_ShouldFail_IfTseUninitialized()
        {
            var scu = GetResetSystemUnderTest();
            Func<Task> action = async () => await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });
            await action.Should().ThrowAsync<ScuException>();
        }

        [Fact]
        public async Task RegisterClientIdAsync_ShouldFail_IfTseTerminated()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });
            Func<Task> action = async () => await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });
            await action.Should().ThrowAsync<ScuException>();
        }

        [Fact]
        public async Task TransactionOperations_ShouldFail_IfTseUninitialized()
        {
            var scu = GetResetSystemUnderTest();

            using (new AssertionScope())
            {
                Func<Task> startAction = async () => await scu.StartTransactionAsync(new StartTransactionRequest());
                await startAction.Should().ThrowAsync<ScuException>().WithMessage("Expected state to be Initialized but instead the TSE state was Uninitialized.");

                Func<Task> updateAction = async () => await scu.UpdateTransactionAsync(new UpdateTransactionRequest());
                await updateAction.Should().ThrowAsync<ScuException>().WithMessage("Expected state to be Initialized but instead the TSE state was Uninitialized.");

                Func<Task> finishAction = async () => await scu.FinishTransactionAsync(new FinishTransactionRequest());
                await finishAction.Should().ThrowAsync<ScuException>().WithMessage("Expected state to be Initialized but instead the TSE state was Uninitialized.");
            }
        }

        [Fact]
        public async Task TransactionOperations_ShouldFail_IfTseTerminated()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });

            using (new AssertionScope())
            {
                Func<Task> startAction = async () => await scu.StartTransactionAsync(new StartTransactionRequest());
                await startAction.Should().ThrowAsync<ScuException>().WithMessage("Expected state to be Initialized but instead the TSE state was Terminated.");

                Func<Task> updateAction = async () => await scu.UpdateTransactionAsync(new UpdateTransactionRequest());
                await updateAction.Should().ThrowAsync<ScuException>().WithMessage("Expected state to be Initialized but instead the TSE state was Terminated.");

                Func<Task> finishAction = async () => await scu.FinishTransactionAsync(new FinishTransactionRequest());
                await finishAction.Should().ThrowAsync<ScuException>().WithMessage("Expected state to be Initialized but instead the TSE state was Terminated.");
            }
        }

        [Fact]
        public async Task TransactionOperations_ShouldFail_IfClientIsNotRegistered()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });

            var notExistingClientId = Guid.NewGuid().ToString();

            using (new AssertionScope())
            {
                Func<Task> startAction = async () => await scu.StartTransactionAsync(new StartTransactionRequest
                {
                    ClientId = notExistingClientId
                });
                await startAction.Should().ThrowAsync<ScuException>().WithMessage($"The client with the id {notExistingClientId} is not registered.");

                Func<Task> updateAction = async () => await scu.UpdateTransactionAsync(new UpdateTransactionRequest
                {
                    ClientId = notExistingClientId
                });
                await updateAction.Should().ThrowAsync<ScuException>().WithMessage($"The client with the id {notExistingClientId} is not registered.");

                Func<Task> finishAction = async () => await scu.FinishTransactionAsync(new FinishTransactionRequest
                {
                    ClientId = notExistingClientId
                });
                await finishAction.Should().ThrowAsync<ScuException>().WithMessage($"The client with the id {notExistingClientId} is not registered.");
            }
        }

        [Fact]
        public async Task AllOperations_ShouldFail_IfTseIsNotAvailable()
        {
            var scu = GetSystemUnderTest(new Dictionary<string, object> {
                {"devicePath", "f:"}
            });

            using (new AssertionScope())
            {
                Func<Task> startAction = async () => await scu.StartTransactionAsync(new StartTransactionRequest());
                await startAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> updateAction = async () => await scu.UpdateTransactionAsync(new UpdateTransactionRequest());
                await updateAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> finishAction = async () => await scu.FinishTransactionAsync(new FinishTransactionRequest());
                await finishAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> getTseInfoAction = async () => await scu.GetTseInfoAsync();
                await getTseInfoAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> setTseStateAction = async () => await scu.SetTseStateAsync(new TseState());
                await setTseStateAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> registerClientIdoAction = async () => await scu.RegisterClientIdAsync(new RegisterClientIdRequest());
                await registerClientIdoAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> unregisterClientIdoAction = async () => await scu.UnregisterClientIdAsync(new UnregisterClientIdRequest());
                await unregisterClientIdoAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                Func<Task> startExportSessionAction = async () => await scu.StartExportSessionAsync(new StartExportSessionRequest());
                await startExportSessionAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                //Func<Task> startExportSessionByTimeStampAction = async () => await scu.StartExportSessionByTimeStampAsync(new StartExportSessionByTimeStampRequest());
                //await startExportSessionByTimeStampAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");

                //Func<Task> startExportSessionByTransactionAction = async () => await scu.StartExportSessionByTransactionAsync(new StartExportSessionByTransactionRequest());
                //await startExportSessionByTransactionAction.Should().ThrowAsync<ScuException>().WithMessage("The TSE is not available.*");
            }
        }

        [Fact]
        public async Task UpdateAndFinishTransactionOperations_ShouldFail_IfTransactionNotStarted()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            ulong nonExistingTransactionNumber = 400;

            using (new AssertionScope())
            {
                Func<Task> updateAction = async () => await scu.UpdateTransactionAsync(CreateUpdateTransactionRequest(nonExistingTransactionNumber, TseClientId));
                await updateAction.Should().ThrowAsync<ScuException>().WithMessage($"The transaction with the number {nonExistingTransactionNumber} is either not started or has been finished already.");

                Func<Task> finishAction = async () => await scu.FinishTransactionAsync(CreateFinishTransactionRequest(nonExistingTransactionNumber, TseClientId));
                await finishAction.Should().ThrowAsync<ScuException>().WithMessage($"The transaction with the number {nonExistingTransactionNumber} is either not started or has been finished already.");
            }
        }

        [Fact]
        public async Task StartTransactionAsync_ShouldReturnResultWithIncreasedCounters()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var result = await scu.StartTransactionAsync(CreateStartTransactionRequest(TseClientId));
            var expectedDateTime = DateTime.UtcNow;

            var info = await scu.GetTseInfoAsync();

            using (new AssertionScope())
            {
                result.TransactionNumber.Should().Be(1);
                result.TimeStamp.Should().BeCloseTo(expectedDateTime, 2.Seconds());
                result.TseSerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                result.ClientId.Should().Be(TseClientId);
                result.SignatureData.SignatureCounter.Should().Be(18);
                result.SignatureData.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                result.SignatureData.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                result.SignatureData.SignatureBase64.Should().NotBeNullOrWhiteSpace();

                info.CurrentNumberOfStartedTransactions.Should().Be(1);
                info.CurrentStartedTransactionNumbers.Should().Contain(1);
            }
        }

        [Fact]
        public async Task UpdateTransactionAsync_ShouldReturnResultWithIncreasedCounters()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var startTransactionResponse = await scu.StartTransactionAsync(CreateStartTransactionRequest(TseClientId));
            var updateRequest = CreateUpdateTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId);
            var result = await scu.UpdateTransactionAsync(updateRequest);
            var expectedDateTime = DateTime.UtcNow;

            var info = await scu.GetTseInfoAsync();

            using (new AssertionScope())
            {
                result.TransactionNumber.Should().Be(startTransactionResponse.TransactionNumber);
                result.TimeStamp.Should().BeCloseTo(expectedDateTime, 3.Seconds());
                result.TseSerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                result.ClientId.Should().Be(TseClientId);
                result.ProcessType.Should().Be(updateRequest.ProcessType);
                result.ProcessDataBase64.Should().Be(updateRequest.ProcessDataBase64);
                result.SignatureData.SignatureCounter.Should().Be(SignaturCounterOffset +19);
                result.SignatureData.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                result.SignatureData.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                result.SignatureData.SignatureBase64.Should().NotBeNullOrWhiteSpace();

                info.CurrentNumberOfStartedTransactions.Should().Be(1);
                info.CurrentStartedTransactionNumbers.Should().Contain(startTransactionResponse.TransactionNumber);
            }
        }

        [Fact]
        public async Task FinishTransactionAsync_ShouldReturnResultWithIncreasedCounters()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var startTransactionResponse = await scu.StartTransactionAsync(CreateStartTransactionRequest(TseClientId));
            var finishRequest = CreateFinishTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId);
            var result = await scu.FinishTransactionAsync(finishRequest);
            var expectedDateTime = DateTime.UtcNow;

            using (new AssertionScope())
            {
                result.TransactionNumber.Should().Be(startTransactionResponse.TransactionNumber);
                result.StartTransactionTimeStamp.Should().Be(startTransactionResponse.TimeStamp);
                result.TimeStamp.Should().BeCloseTo(expectedDateTime, 2.Seconds());
                result.TseTimeStampFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                result.TseSerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                result.ClientId.Should().Be(TseClientId);
                result.ProcessType.Should().Be(finishRequest.ProcessType);
                result.ProcessDataBase64.Should().Be(finishRequest.ProcessDataBase64);
                result.SignatureData.SignatureCounter.Should().Be(19);
                result.SignatureData.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                result.SignatureData.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                result.SignatureData.SignatureBase64.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public async Task StartUpdateFinishTransactionAsync_ShouldReturnResultWithIncreasedCounters()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var startTransactionResponse = await scu.StartTransactionAsync(CreateStartTransactionRequest(TseClientId));
            _ = await scu.UpdateTransactionAsync(CreateUpdateTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId));
            var finishRequest = CreateFinishTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId);
            var result = await scu.FinishTransactionAsync(finishRequest);
            var expectedDateTime = DateTime.UtcNow;

            using (new AssertionScope())
            {
                result.TransactionNumber.Should().Be(1);
                result.StartTransactionTimeStamp.Should().Be(startTransactionResponse.TimeStamp);
                result.TimeStamp.Should().BeCloseTo(expectedDateTime, 2.Seconds());
                result.TseTimeStampFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                result.TseSerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                result.ClientId.Should().Be(TseClientId);
                result.ProcessType.Should().Be(finishRequest.ProcessType);
                result.ProcessDataBase64.Should().Be(finishRequest.ProcessDataBase64);
                result.SignatureData.SignatureCounter.Should().Be(20);
                result.SignatureData.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                result.SignatureData.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                result.SignatureData.SignatureBase64.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public async Task StartUpdateFinishTransactionAsync_ShouldReturnResultWithIncreasedCounters_IfSCUIsReinitialized()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var startTransactionResponse = await scu.StartTransactionAsync(CreateStartTransactionRequest(TseClientId));
            _ = await scu.UpdateTransactionAsync(CreateUpdateTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId));

            scu = GetSystemUnderTest();
            var finishRequest = CreateFinishTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId);
            var result = await scu.FinishTransactionAsync(finishRequest);
            var expectedDateTime = DateTime.UtcNow;

            using (new AssertionScope())
            {
                result.TransactionNumber.Should().Be(startTransactionResponse.TransactionNumber);
                result.StartTransactionTimeStamp.Should().Be(startTransactionResponse.TimeStamp);
                result.TimeStamp.Should().BeCloseTo(expectedDateTime, 3.Seconds());
                result.TseTimeStampFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                result.TseSerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                result.ClientId.Should().Be(TseClientId);
                result.ProcessType.Should().Be(finishRequest.ProcessType);
                result.ProcessDataBase64.Should().Be(finishRequest.ProcessDataBase64);
                result.SignatureData.SignatureCounter.Should().Be(24 + SignaturCounterOffset);
                result.SignatureData.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                result.SignatureData.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                result.SignatureData.SignatureBase64.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public async Task StartUpdateFinishTransactionAsync_x100_ShouldFinishInTime()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            StartTransactionResponse startTransactionResponse = null;
            FinishTransactionRequest finishRequest = null;
            FinishTransactionResponse result = null;
            for (var i = 0; i < 100; i++)
            {
                startTransactionResponse = await scu.StartTransactionAsync(CreateStartTransactionRequest(TseClientId));
                _ = await scu.UpdateTransactionAsync(CreateUpdateTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId));
                finishRequest = CreateFinishTransactionRequest(startTransactionResponse.TransactionNumber, TseClientId);
                result = await scu.FinishTransactionAsync(finishRequest);
            }

            var expectedDateTime = DateTime.UtcNow;
            using (new AssertionScope())
            {
                result.TransactionNumber.Should().Be(100);
                result.StartTransactionTimeStamp.Should().Be(startTransactionResponse.TimeStamp);
                result.TimeStamp.Should().BeCloseTo(expectedDateTime, 3.Seconds());
                result.TseTimeStampFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                result.TseSerialNumberOctet.Should().Be(ExpectedInitializedTseInfo.SerialNumberOctet);
                result.ClientId.Should().Be(TseClientId);
                result.ProcessType.Should().Be(finishRequest.ProcessType);
                result.ProcessDataBase64.Should().Be(finishRequest.ProcessDataBase64);
                result.SignatureData.SignatureCounter.Should().Be(317 + SignaturCounterOffset);
                result.SignatureData.PublicKeyBase64.Should().Be(ExpectedInitializedTseInfo.PublicKeyBase64);
                result.SignatureData.SignatureAlgorithm.Should().Be(ExpectedInitializedTseInfo.SignatureAlgorithm);
                result.SignatureData.SignatureBase64.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public async Task StartExportSessionAsyncWithErase_ExportShouldContainLogs()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var startTransactionResponses = new List<(StartTransactionRequest, StartTransactionResponse)>();
            var updateTransactionResponses = new List<(UpdateTransactionRequest, UpdateTransactionResponse)>();
            var finishTransactionResponses = new List<(FinishTransactionRequest, FinishTransactionResponse)>();

            for (var i = 0; i < 10; i++)
            {
                var startRequest = CreateStartTransactionRequest(TseClientId);
                var startResponse = await scu.StartTransactionAsync(startRequest);
                startTransactionResponses.Add((startRequest, startResponse));
                var updateRequest = CreateUpdateTransactionRequest(startResponse.TransactionNumber, TseClientId);
                updateTransactionResponses.Add((updateRequest, await scu.UpdateTransactionAsync(updateRequest)));
                var finishRequest = CreateFinishTransactionRequest(startResponse.TransactionNumber, TseClientId);
                finishTransactionResponses.Add((finishRequest, await scu.FinishTransactionAsync(finishRequest)));
            }

            var exportSession = await scu.StartExportSessionAsync(new StartExportSessionRequest());
            await PerformExport(scu, exportSession.TokenId, erase: false);
            CompareLogs(startTransactionResponses, updateTransactionResponses, finishTransactionResponses, exportSession);
            
            var secondExportSession = await scu.StartExportSessionAsync(new StartExportSessionRequest());
            await PerformExport(scu, secondExportSession.TokenId, erase: false);
            CompareLogs(startTransactionResponses, updateTransactionResponses, finishTransactionResponses, secondExportSession);
        }

        [Fact]
        public async Task StartExportSessionAsyncWithErase_ShouldDeleteLogs()
        {
            var scu = GetResetSystemUnderTest();
            _ = await scu.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });
            _ = await scu.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = TseClientId
            });

            var startTransactionResponses = new List<(StartTransactionRequest, StartTransactionResponse)>();
            var updateTransactionResponses = new List<(UpdateTransactionRequest, UpdateTransactionResponse)>();
            var finishTransactionResponses = new List<(FinishTransactionRequest, FinishTransactionResponse)>();

            for (var i = 0; i < 10; i++)
            {
                var startRequest = CreateStartTransactionRequest(TseClientId);
                var startResponse = await scu.StartTransactionAsync(startRequest);
                startTransactionResponses.Add((startRequest, startResponse));
                var updateRequest = CreateUpdateTransactionRequest(startResponse.TransactionNumber, TseClientId);
                updateTransactionResponses.Add((updateRequest, await scu.UpdateTransactionAsync(updateRequest)));
                var finishRequest = CreateFinishTransactionRequest(startResponse.TransactionNumber, TseClientId);
                finishTransactionResponses.Add((finishRequest, await scu.FinishTransactionAsync(finishRequest)));
            }

            var exportSession = await scu.StartExportSessionAsync(new StartExportSessionRequest
            {
                Erase = true
            });
            await PerformExport(scu, exportSession.TokenId, erase: true);

            var secondExportSession = await scu.StartExportSessionAsync(new StartExportSessionRequest
            {
                Erase = false
            });
            await PerformExport(scu, secondExportSession.TokenId, erase: false);

            using (var fileStream = File.OpenRead($"export_{secondExportSession.TokenId}.tar"))
            {
                var logs = LogParser.GetLogsFromTarStream(fileStream).ToList();
                logs.Should().ContainSingle();

                var transactionLogs = logs.OfType<TransactionLogMessage>();
                transactionLogs.Should().BeEmpty();
                var auditLogs = logs.OfType<AuditLogMessage>();
                auditLogs.Should().Contain(x => x.FileName.ToLower().Contains("logout"));
                var systemLogs = logs.OfType<SystemLogMessage>();
                systemLogs.Should().BeEmpty();
            }
        }

        private static async Task PerformExport(IDESSCD scu, string tokenId, bool erase = false)
        {
            using (var fileStream = File.OpenWrite($"export_{tokenId}.tar"))
            {
                ExportDataResponse export;
                do
                {
                    export = await scu.ExportDataAsync(new ExportDataRequest
                    {
                        TokenId = tokenId,
                        MaxChunkSize = 4096
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
                TokenId = tokenId,
                Erase = erase
            };
            using (var fileStream = File.OpenRead($"export_{tokenId}.tar"))
            {
                endSessionRequest.Sha256ChecksumBase64 = Convert.ToBase64String(SHA256.Create().ComputeHash(fileStream));
            }
            var endExportSessionResult = await scu.EndExportSessionAsync(endSessionRequest);
            endExportSessionResult.IsValid.Should().BeTrue();
        }

        private void CompareLogs(List<(StartTransactionRequest, StartTransactionResponse)> startTransactionResponses, List<(UpdateTransactionRequest, UpdateTransactionResponse)> updateTransactionResponses, List<(FinishTransactionRequest, FinishTransactionResponse)> finishTransactionResponses, StartExportSessionResponse exportSession)
        {
            using (var fileStream = File.OpenRead($"export_{exportSession.TokenId}.tar"))
            {
                var logs = LogParser.GetLogsFromTarStream(fileStream).ToList();
                using (new AssertionScope())
                {
                    var startLogs = logs.OfType<TransactionLogMessage>().Where(x => x.OperationType == "StartTransaction");
                    foreach ((var startTransactionRequest, var startTransactionResponse) in startTransactionResponses)
                    {
                        var startLog = startLogs.FirstOrDefault(x => x.TransactionNumber == startTransactionResponse.TransactionNumber);
                        startLog.ClientId.Should().Be(startTransactionResponse.ClientId);
                        startLog.ProcessDataBase64.Should().Be(startTransactionRequest.ProcessDataBase64);
                        startLog.ProcessType.Should().Be(startTransactionRequest.ProcessType);
                        startLog.AdditionalInternalData.Should().BeNullOrEmpty();
                        startLog.SerialNumber.Should().Be(startTransactionResponse.TseSerialNumberOctet);
                        startLog.SignatureAlgorithm.Algorithm.Should().Be(startTransactionResponse.SignatureData.SignatureAlgorithm);
                        startLog.SignaturCounter.Should().Be(startTransactionResponse.SignatureData.SignatureCounter);
                        startLog.SignaturValueBase64.Should().Be(startTransactionResponse.SignatureData.SignatureBase64);
                        startLog.LogTime.Should().Be(startTransactionResponse.TimeStamp);
                        startLog.LogTimeFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                    }
                    var updateLogs = logs.OfType<TransactionLogMessage>().Where(x => x.OperationType == "UpdateTransaction");
                    foreach ((var updateTransactionRequest, var updateTransactionResponse) in updateTransactionResponses)
                    {
                        var updateLog = updateLogs.FirstOrDefault(x => x.TransactionNumber == updateTransactionResponse.TransactionNumber);
                        updateLog.ClientId.Should().Be(updateTransactionResponse.ClientId);
                        updateLog.ProcessDataBase64.Should().Be(updateTransactionResponse.ProcessDataBase64);
                        updateLog.ProcessType.Should().Be(updateTransactionResponse.ProcessType);
                        updateLog.AdditionalInternalData.Should().BeNullOrEmpty();
                        updateLog.SerialNumber.Should().Be(updateTransactionResponse.TseSerialNumberOctet);
                        updateLog.SignatureAlgorithm.Algorithm.Should().Be(updateTransactionResponse.SignatureData.SignatureAlgorithm);
                        updateLog.SignaturCounter.Should().Be(updateTransactionResponse.SignatureData.SignatureCounter);
                        updateLog.SignaturValueBase64.Should().Be(updateTransactionResponse.SignatureData.SignatureBase64);
                        updateLog.LogTime.Should().Be(updateTransactionResponse.TimeStamp);
                        updateLog.LogTimeFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                    }
                    var finishLogs = logs.OfType<TransactionLogMessage>().Where(x => x.OperationType == "FinishTransaction");
                    foreach ((var finishiTransactionRequest, var finishTransactionResponse) in finishTransactionResponses)
                    {
                        var finishLog = finishLogs.FirstOrDefault(x => x.TransactionNumber == finishTransactionResponse.TransactionNumber);
                        finishLog.ClientId.Should().Be(finishTransactionResponse.ClientId);
                        finishLog.ProcessDataBase64.Should().Be(finishTransactionResponse.ProcessDataBase64);
                        finishLog.ProcessType.Should().Be(finishTransactionResponse.ProcessType);
                        finishLog.AdditionalInternalData.Should().BeNullOrEmpty();
                        finishLog.SerialNumber.Should().Be(finishTransactionResponse.TseSerialNumberOctet);
                        finishLog.SignatureAlgorithm.Algorithm.Should().Be(finishTransactionResponse.SignatureData.SignatureAlgorithm);
                        finishLog.SignaturCounter.Should().Be(finishTransactionResponse.SignatureData.SignatureCounter);
                        finishLog.SignaturValueBase64.Should().Be(finishTransactionResponse.SignatureData.SignatureBase64);
                        finishLog.LogTime.Should().Be(finishTransactionResponse.TimeStamp);
                        finishLog.LogTimeFormat.Should().Be(ExpectedInitializedTseInfo.LogTimeFormat);
                    }

                    var auditLogs = logs.OfType<AuditLogMessage>();
                    var systemLogs = logs.OfType<SystemLogMessage>();
                }
            }
        }

        private static StartTransactionRequest CreateStartTransactionRequest(string clientId)
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

        private static UpdateTransactionRequest CreateUpdateTransactionRequest(ulong transactionNumber, string clientId)
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

        private static FinishTransactionRequest CreateFinishTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            var finishRequest = new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
            return finishRequest;
        }
    }
}

