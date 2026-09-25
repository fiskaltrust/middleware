using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using V1 = fiskaltrust.ifPOS.v1;
using IITSSCD = fiskaltrust.ifPOS.v1.it.IITSSCD;
using RTInfo = fiskaltrust.ifPOS.v1.it.RTInfo;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.SCU;

public class ITSSCDProviderTests
{
    private readonly ftQueue _queue = TestHelpers.CreateQueue();
    private readonly ftQueueIT _queueIT;
    private readonly ftSignaturCreationUnitIT _scu;
    private readonly Mock<IConfigurationRepository> _configurationRepository;
    private readonly Mock<IITSSCD> _client = new(MockBehavior.Strict);
    private readonly Mock<IClientFactory<IITSSCD>> _clientFactory = new(MockBehavior.Strict);
    private readonly List<ClientConfiguration> _clientConfigurations = [];

    public ITSSCDProviderTests()
    {
        _queueIT = TestHelpers.CreateQueueIT(_queue);
        _scu = TestHelpers.CreateSignaturCreationUnitIT(_queueIT, url: "[\"rest://localhost:1401\",\"grpc://localhost:1400\"]");
        _configurationRepository = TestHelpers.CreateConfigurationRepository(_queueIT, _scu);
        _client.Setup(x => x.GetRTInfoAsync()).ReturnsAsync(new RTInfo { SerialNumber = TestHelpers.RTSerialNumber, InfoData = "{}" });
        _clientFactory.Setup(x => x.CreateClient(It.IsAny<ClientConfiguration>())).Returns((ClientConfiguration c) =>
        {
            _clientConfigurations.Add(c);
            return _client.Object;
        });
    }

    private ITSSCDProvider CreateSut(QueueITConfiguration? configuration = null)
        => new(Mock.Of<ILogger<ITSSCDProvider>>(), _clientFactory.Object, TestHelpers.Lazy(_configurationRepository.Object), _queue.ftQueueId, configuration ?? new QueueITConfiguration());

    [Fact]
    public async Task CreatesTheClientFromTheAssignedScu_PrefersGrpc_AndStoresTheRTInfo()
    {
        var sut = CreateSut(new QueueITConfiguration { ScuTimeoutMs = 5000, ScuMaxRetries = 2 });

        var rtInfo = await sut.GetRTInfoAsync();
        await sut.GetRTInfoAsync();

        using var scope = new AssertionScope();
        rtInfo.SerialNumber.Should().Be(TestHelpers.RTSerialNumber);
        _clientConfigurations.Should().HaveCount(1, "the client is created once and reused");
        _clientConfigurations[0].Url.Should().Be("grpc://localhost:1400/");
        _clientConfigurations[0].UrlType.Should().Be("grpc");
        _clientConfigurations[0].Timeout.Should().Be(TimeSpan.FromSeconds(5));
        _clientConfigurations[0].RetryCount.Should().Be(2);
        _configurationRepository.Verify(x => x.InsertOrUpdateSignaturCreationUnitITAsync(It.Is<ftSignaturCreationUnitIT>(s => s.ftSignaturCreationUnitITId == _scu.ftSignaturCreationUnitITId && s.InfoJson.Contains(TestHelpers.RTSerialNumber))), Times.Once);
    }

    [Fact]
    public async Task StillAnswers_WhenTheRTInfoCannotBeStoredAtCreation()
    {
        _client.SetupSequence(x => x.GetRTInfoAsync())
            .ThrowsAsync(new TimeoutException("not reachable"))
            .ReturnsAsync(new RTInfo { SerialNumber = TestHelpers.RTSerialNumber });
        var sut = CreateSut();

        var rtInfo = await sut.GetRTInfoAsync();

        rtInfo.SerialNumber.Should().Be(TestHelpers.RTSerialNumber);
        _configurationRepository.Verify(x => x.InsertOrUpdateSignaturCreationUnitITAsync(It.IsAny<ftSignaturCreationUnitIT>()), Times.Never);
    }

    [Fact]
    public async Task RetriesCreatingTheClient_AfterAFailedAttempt()
    {
        var scuId = _queueIT.ftSignaturCreationUnitITId;
        _queueIT.ftSignaturCreationUnitITId = null;
        var sut = CreateSut();

        var act = () => sut.GetRTInfoAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no ftSignaturCreationUnitIT assigned*");

        _queueIT.ftSignaturCreationUnitITId = scuId;
        (await sut.GetRTInfoAsync()).SerialNumber.Should().Be(TestHelpers.RTSerialNumber);
        _clientConfigurations.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessReceipt_TranslatesToV1AndBack()
    {
        V1.it.ProcessRequest? v1Request = null;
        _client.Setup(x => x.ProcessReceiptAsync(It.IsAny<V1.it.ProcessRequest>())).ReturnsAsync((V1.it.ProcessRequest request) =>
        {
            v1Request = request;
            var response = request.ReceiptResponse;
            response.ftReceiptIdentification += "0001-0002";
            response.ftSignatures = [.. response.ftSignatures, new V1.SignaturItem { Caption = "<rt-doc-number>", Data = "0002", ftSignatureFormat = 1, ftSignatureType = 0x4954_2000_0000_0012 }];
            return new V1.it.ProcessResponse { ReceiptResponse = response };
        });
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, "0001-0001");
        request.cbCustomer = "{\"CustomerName\":\"Mario Rossi\"}";
        var response = TestHelpers.CreateResponse(_queue, TestHelpers.CreateQueueItem(_queue), request);
        response.ftStateData = new MiddlewareStateData { PreviousReceiptReference = [] };
        var sut = CreateSut();

        var result = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response });

        using var scope = new AssertionScope();
        v1Request.Should().NotBeNull();
        v1Request!.ReceiptRequest.ftReceiptCase.Should().Be(0x4954_2000_0100_0001);
        v1Request.ReceiptRequest.cbPreviousReceiptReference.Should().Be("0001-0001");
        v1Request.ReceiptRequest.cbCustomer.Should().Be("{\"CustomerName\":\"Mario Rossi\"}");
        v1Request.ReceiptResponse.ftStateData.Should().BeNull();
        result.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#0001-0002");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(x => x.Data == "0002");
        result.ReceiptResponse.ftStateData.Should().BeSameAs(response.ftStateData);
        result.ReceiptResponse.ftQueueItemID.Should().Be(response.ftQueueItemID);
    }

    [Fact]
    public void GetUriForSignaturCreationUnit_AcceptsAPlainUrl()
    {
        var uri = ITSSCDProvider.GetUriForSignaturCreationUnit(new ftSignaturCreationUnitIT { Url = "rest://localhost:1401" });

        uri.Scheme.Should().Be("rest");
        uri.Port.Should().Be(1401);
    }

    [Fact]
    public void GetUriForSignaturCreationUnit_RefusesAnScuWithoutUrl()
    {
        var act = () => ITSSCDProvider.GetUriForSignaturCreationUnit(new ftSignaturCreationUnitIT { ftSignaturCreationUnitITId = Guid.NewGuid(), Url = "" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*has no Url configured*");
    }
}
