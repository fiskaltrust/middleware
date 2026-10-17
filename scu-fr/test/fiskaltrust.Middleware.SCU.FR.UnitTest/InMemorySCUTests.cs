using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.InMemory;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

public class InMemorySCUTests
{
    [Fact]
    public async Task EchoAsync_ReturnsTheMessage()
    {
        using var sut = new InMemorySCU();

        var response = await sut.EchoAsync(new EchoRequest { Message = "bonjour" });

        response.Message.Should().Be("bonjour");
    }

    [Fact]
    public async Task ProcessReceiptAsync_LinksTheChain()
    {
        using var sut = new InMemorySCU();

        var (_, firstHash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);
        var (second, secondHash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, firstHash);

        firstHash.Should().NotBeNullOrEmpty();
        secondHash.Should().NotBe(firstHash);
        second.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash)).Data.Should().Be(secondHash);
    }

    [Fact]
    public async Task ProcessReceiptAsync_IsDeterministicForTheSameInput()
    {
        using var sut = new InMemorySCU();

        var (first, _) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);
        var (second, _) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);

        // ECDSA signatures are randomized, but the identity signatures must not move.
        SignatureData(first, SignatureTypeFR.Siret).Should().Be(SignatureData(second, SignatureTypeFR.Siret));
        SignatureData(first, SignatureTypeFR.CertificateSerialNumber).Should().Be("INMEMORY-0001");
    }

    [Fact]
    public async Task GetInfoAsync_ReportsSignatureCreationDataAvailable()
    {
        using var sut = new InMemorySCU();

        var info = await sut.GetInfoAsync();

        info.InfoData.Should().Contain("\"SignatureCreationDataAvailable\":true");
    }

    private static string SignatureData(ProcessResponse response, SignatureTypeFR type)
        => response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(type)).Data;
}
