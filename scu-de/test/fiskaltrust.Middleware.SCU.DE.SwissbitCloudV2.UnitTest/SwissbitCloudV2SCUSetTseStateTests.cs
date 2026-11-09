using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Exceptions;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.UnitTest
{
    // Disabling a Swissbit Cloud TSE is final. PreventTseDisable protects a TSE that several cashboxes
    // share (the sandbox developer sample) from a single cashbox's out-of-operation receipt.
    public class SwissbitCloudV2SCUSetTseStateTests
    {
        private readonly Mock<ISwissbitCloudV2ApiProvider> _apiProviderMock = new Mock<ISwissbitCloudV2ApiProvider>(MockBehavior.Strict);

        private SwissbitCloudV2SCU CreateSut(bool preventTseDisable)
        {
            var configuration = new SwissbitCloudV2SCUConfiguration { PreventTseDisable = preventTseDisable };
            return new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), _apiProviderMock.Object, new ClientCache(_apiProviderMock.Object), configuration);
        }

        private void TseIs(SwissbitCloudV2TseState state)
        {
            _apiProviderMock.Setup(x => x.GetTseStatusAsync()).ReturnsAsync(new TseDto { InitializationState = state.ToString() });
        }

        [Fact]
        public async Task SetTseStateAsync_Terminated_WithPreventTseDisable_Should_ThrowAndNeverDisable()
        {
            TseIs(SwissbitCloudV2TseState.initialized);
            var sut = CreateSut(preventTseDisable: true);

            var ex = await Assert.ThrowsAsync<SwissbitCloudV2Exception>(() => sut.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated }));

            Assert.Contains("PreventTseDisable", ex.Message);
            _apiProviderMock.Verify(x => x.DisableTseAsync(), Times.Never);
        }

        [Fact]
        public async Task SetTseStateAsync_Terminated_WithoutPreventTseDisable_Should_DisableOnce()
        {
            TseIs(SwissbitCloudV2TseState.initialized);
            _apiProviderMock.Setup(x => x.DisableTseAsync()).ReturnsAsync(new TseDto { InitializationState = SwissbitCloudV2TseState.disabled.ToString() });
            var sut = CreateSut(preventTseDisable: false);

            var result = await sut.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });

            Assert.Equal(TseStates.Terminated, result.CurrentState);
            _apiProviderMock.Verify(x => x.DisableTseAsync(), Times.Once);
        }

        [Fact]
        public async Task SetTseStateAsync_Initialized_WithPreventTseDisable_Should_StillAnswerTheInitializedTse()
        {
            TseIs(SwissbitCloudV2TseState.initialized);
            var sut = CreateSut(preventTseDisable: true);

            var result = await sut.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized });

            Assert.Equal(TseStates.Initialized, result.CurrentState);
            _apiProviderMock.Verify(x => x.DisableTseAsync(), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetTseStateAsync_OnDisabledTse_Should_StillRefuseInitialization_WhateverTheFlag(bool preventTseDisable)
        {
            TseIs(SwissbitCloudV2TseState.disabled);
            var sut = CreateSut(preventTseDisable);

            var ex = await Assert.ThrowsAsync<SwissbitCloudV2Exception>(() => sut.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized }));

            Assert.Contains("disabled", ex.Message);
            _apiProviderMock.Verify(x => x.DisableTseAsync(), Times.Never);
        }

        [Fact]
        public async Task SetTseStateAsync_Terminated_WithPreventTseDisable_OnAnAlreadyDisabledTse_Should_ReportTerminatedWithoutCalling()
        {
            TseIs(SwissbitCloudV2TseState.disabled);
            var sut = CreateSut(preventTseDisable: true);

            var result = await sut.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated });

            Assert.Equal(TseStates.Terminated, result.CurrentState);
            _apiProviderMock.Verify(x => x.DisableTseAsync(), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData("true")]
        public void Configuration_Should_ReadPreventTseDisable_TheWayTheBootstrapperDoes(object flagValue)
        {
            // Cashbox configurations carry booleans as JSON true or as the string "true".
            var withFlag = new Dictionary<string, object> { { "TseSerialNumber", "abc" }, { "TseAccessToken", "def" }, { "Sandbox", true }, { "PreventTseDisable", flagValue } };
            var withoutFlag = new Dictionary<string, object> { { "TseSerialNumber", "abc" }, { "TseAccessToken", "def" }, { "Sandbox", true } };

            Assert.True(Deserialize(withFlag).PreventTseDisable);
            Assert.False(Deserialize(withoutFlag).PreventTseDisable);
        }

        private static SwissbitCloudV2SCUConfiguration Deserialize(Dictionary<string, object> configuration)
        {
            // Same round trip as ScuBootstrapper.ConfigureServices.
            return JsonConvert.DeserializeObject<SwissbitCloudV2SCUConfiguration>(JsonConvert.SerializeObject(configuration));
        }
    }
}
