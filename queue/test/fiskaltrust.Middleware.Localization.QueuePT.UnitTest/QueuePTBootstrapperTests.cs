using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest;

public class QueuePTBootstrapperTests
{
    [Fact]
    public void BootstrapSetupTests()
    {
        var cashBoxId = Guid.NewGuid();
        var queueId = Guid.NewGuid();

        var bootstrapper = new QueuePTBootstrapper(queueId, new LoggerFactory(), new Dictionary<string, object>
            {
                { "storageaccountname", "test" },
                { "init_ftQueue", Newtonsoft.Json.JsonConvert.SerializeObject(new List<ftQueue> { new ftQueue { ftQueueId = queueId, ftCashBoxId = cashBoxId } }) }
            });
        _ = bootstrapper.RegisterForSign();

        //var signResult = await signMethod(JsonSerializer.Serialize(new ReceiptRequest
        //{
        //    ftCashBoxID = cashBoxId,
        //}));
        //var response = JsonSerializer.Deserialize<ReceiptResponse>(signResult);
    }

    [Fact]
    public async Task BootstrapSetup_WithSignMethodTests()
    {
        var cashBoxId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var bootstrapper = new QueuePTBootstrapper(queueId, new LoggerFactory(), new Dictionary<string, object>
            {
                { "storageaccountname", "test" },
                { "init_ftQueue", Newtonsoft.Json.JsonConvert.SerializeObject(new List<ftQueue> { new ftQueue { ftQueueId = queueId, ftCashBoxId = cashBoxId } }) }
            });

        var signMethod = bootstrapper.RegisterForSign();

        var signResult = await signMethod(JsonSerializer.Serialize(new ReceiptRequest
        {
            ftCashBoxID = cashBoxId,
        }));
        var response = JsonSerializer.Deserialize<ReceiptResponse>(signResult);
        response.Should().NotBeNull();
        //response!.ftState.Should().Be(0x5054_2000_FFFF_FFFFF);
    }
}
