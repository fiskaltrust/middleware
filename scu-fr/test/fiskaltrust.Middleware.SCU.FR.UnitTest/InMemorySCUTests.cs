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
    public async Task ProcessReceiptAsync_ChainsOnThePreviousSignature()
    {
        using var sut = new InMemorySCU();

        var (first, firstSignature) = await sut.ProcessReceiptAsync(FRTestData.Request(1), null);
        var (second, secondSignature) = await sut.ProcessReceiptAsync(FRTestData.Request(2), firstSignature);

        firstSignature.Should().Be(SignatureData(first, SignatureTypeFR.ReceiptSignature), "the chained value is the stored signature itself");
        secondSignature.Should().Be(SignatureData(second, SignatureTypeFR.ReceiptSignature));
        FRTestData.IsBase64Url(firstSignature).Should().BeTrue();
        second.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash));
    }

    [Fact]
    public async Task ProcessReceiptAsync_IsDeterministicForTheSameInput()
    {
        using var sut = new InMemorySCU();

        var (first, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);
        var (second, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

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
