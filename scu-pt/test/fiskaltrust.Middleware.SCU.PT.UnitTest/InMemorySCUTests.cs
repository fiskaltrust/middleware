using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PT.InMemory;
using fiskaltrust.Middleware.SCU.PT.Abstraction;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PT.UnitTest;

public class InMemorySCUTests
{
    [Fact]
    public async Task EchoAsync_ShouldReturnMessage()
    {
        var scuPT = new ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("PrivateKey.pem"),
            SoftwareCertificateNumber = "9999"
        };
        var sut = new InMemorySCU(scuPT);
        var echoRequest = new EchoRequest { Message = "Test" };
        
        var result = await sut.EchoAsync(echoRequest);
        
        result.Message.Should().Be("Test");
    }

    [Fact]
    public async Task GetInfoAsync_ShouldReturnInfo()
    {
        var scuPT = new ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("PrivateKey.pem"),
            SoftwareCertificateNumber = "9999"
        };
        var sut = new InMemorySCU(scuPT);
        
        var result = await sut.GetInfoAsync();
        
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ShouldReturnSignedResponse()
    {
        var scuPT = new ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("PrivateKey.pem"),
            SoftwareCertificateNumber = "9999"
        };
        var sut = new InMemorySCU(scuPT);
        
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ftChargeItemCase = ChargeItemCase.NormalVatRate,
                    Amount = 100m,
                    VATAmount = 23m,
                    Description = "Test Item",
                    Quantity = 1,
                    VATRate = 23m
                }
            }
        };
        
        var receiptResponse = new ReceiptResponse
        {
            ftState = (State)0x5054_2000_0000_0000,
            ftReceiptIdentification = "TEST#001"
        };
        
        var request = new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        };
        
        var (response, signature) = await sut.ProcessReceiptAsync(request, null);
        
        response.Should().NotBeNull();
        signature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetHashForItem_ShouldReturnCorrectFormat()
    {
        var scuPT = new ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("PrivateKey.pem"),
            SoftwareCertificateNumber = "9999"
        };
        var sut = new InMemorySCU(scuPT);
        
        var element = new PTInvoiceElement
        {
            InvoiceDate = new DateTime(2024, 1, 1),
            SystemEntryDate = new DateTime(2024, 1, 1, 12, 30, 0),
            InvoiceNo = "INV001",
            GrossTotal = 123.45m,
            Hash = "previousHash"
        };
        
        var result = sut.GetHashForItem(element);
        
        result.Should().Contain("2024-01-01");
        result.Should().Contain("2024-01-01T12:30:00");
        result.Should().Contain("INV001");
        result.Should().Contain("123.45");
        result.Should().Contain("previousHash");
    }
}
