using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.InMemory;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest;

public class InMemorySCUTests
{
    private static ProcessRequest CreateRequest(ulong receiptCase) => new()
    {
        ReceiptRequest = new ReceiptRequest
        {
            cbReceiptReference = "pl-test",
            ftReceiptCase = (ReceiptCase)receiptCase,
        },
        ReceiptResponse = new ReceiptResponse
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptIdentification = "ft1#",
        },
    };

    private const ulong CashSale = 0x504C_2000_0000_0001;
    private const ulong DailyClosing = 0x504C_2000_0000_2011;
    private const ulong ZeroReceipt = 0x504C_2000_0000_2000;
    private const ulong Invoice = 0x504C_2000_0000_1001;

    [Fact]
    public async Task EchoAsync_ShouldMirrorMessage()
    {
        var scu = new InMemorySCU();
        (await scu.EchoAsync(new EchoRequest { Message = "test" })).Message.Should().Be("test");
    }

    [Fact]
    public async Task GetInfoAsync_ShouldExposeTypedDeviceInfo_ThroughInfoData()
    {
        var scu = new InMemorySCU();

        var info = await scu.GetInfoAsync();
        var deviceInfo = PLDeviceInfo.FromPLSSCDInfo(info);

        deviceInfo.Should().NotBeNull();
        deviceInfo!.FiscalizationState.Should().Be(PLFiscalizationState.Fiscalized);
        deviceInfo.UniqueDeviceNumber.Should().NotBeNullOrEmpty();
        deviceInfo.VatRateTable.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldNumberReceiptsDeterministically()
    {
        var scu = new InMemorySCU();

        var first = await scu.ProcessReceiptAsync(CreateRequest(CashSale));
        var second = await scu.ProcessReceiptAsync(CreateRequest(CashSale));

        first.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#1");
        second.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#2");
        second.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.FiscalDocumentNumber) && x.Data == "2");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldSetCashBoxIdentification_ToUniqueDeviceNumber()
    {
        var deviceInfo = InMemorySCU.CreateDefaultDeviceInfo();
        var scu = new InMemorySCU(deviceInfo);

        var result = await scu.ProcessReceiptAsync(CreateRequest(CashSale));

        result.ReceiptResponse.ftCashBoxIdentification.Should().Be(deviceInfo.UniqueDeviceNumber);
        result.ReceiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.UniqueDeviceNumber));
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldIncrementZCounter_OnDailyClosing()
    {
        var scu = new InMemorySCU();

        await scu.ProcessReceiptAsync(CreateRequest(DailyClosing));
        var info = PLDeviceInfo.FromPLSSCDInfo(await scu.GetInfoAsync());

        info!.CurrentZReportNumber.Should().Be(1);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldNotNumberZeroReceipts()
    {
        var scu = new InMemorySCU();

        var result = await scu.ProcessReceiptAsync(CreateRequest(ZeroReceipt));

        result.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        var info = PLDeviceInfo.FromPLSSCDInfo(await scu.GetInfoAsync());
        info!.CurrentZReportNumber.Should().Be(0);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldRejectInvoiceCases()
    {
        var scu = new InMemorySCU();

        var act = () => scu.ProcessReceiptAsync(CreateRequest(Invoice));

        await act.Should().ThrowAsync<PLValidationException>();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldRunValidationHooks()
    {
        var seen = new List<string?>();
        var scu = new InMemorySCU(validators: new Action<ProcessRequest>[]
        {
            request => seen.Add(request.ReceiptRequest.cbReceiptReference),
            request => throw new PLValidationException("blocked by hook"),
        });

        var act = () => scu.ProcessReceiptAsync(CreateRequest(CashSale));

        await act.Should().ThrowAsync<PLValidationException>().WithMessage("blocked by hook");
        seen.Should().ContainSingle().Which.Should().Be("pl-test");
    }
}
