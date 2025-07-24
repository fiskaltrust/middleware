using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using FluentAssertions;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest;

public class AADEFactoryTests
{
    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_IfHandwritten_AndFieldsAreNotDefined()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {

                }
            }
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error!.Exception.Message.Should().Be("When using Handwritten receipts the Series must be provided in the ftReceiptCaseData payload.");
    }

    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_IfHandwritten_FieldsAreDefined_HashPayloadShouldMatch()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = "Test",
                    Series = "test", // This should be defined
                    AA = 123456789,
                    HashAlg = "SHA256",
                    HashPayload = "asdf"
                }
            },
            cbReceiptAmount = 100,
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error!.Exception.Message.Should().StartWith("The HashPayload does not match the expected value.");
    }

    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_IfHandwritten_FieldsAreDefined_HashPayloadMatches_EverythingGreen()
    {
        var merchantId = "Test";
        var series = "test";
        var aa = 1;
        var cbReceiptReference = Guid.NewGuid().ToString();
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptMoment = DateTime.UtcNow;
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem 
                {
                    Amount = 100
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = merchantId,
                    Series = series, // This should be defined
                    AA = aa,
                    HashAlg = "SHA256",
                    HashPayload = merchantId + "-" + series + "-" + aa + "-" + cbReceiptReference + "-" + receiptMoment + "-"  + chargeItems.Sum(x => x.Amount)
                }
            },
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().BeNull();

        action.Should().NotBeNull();
        action!.invoice[0].invoiceHeader.series.Should().Be(series);
        action!.invoice[0].invoiceHeader.aa.Should().Be(aa.ToString());
    }

    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_FieldsAreDefined_ButShouldBeIgnored()
    {
        var merchantId = "Test";
        var series = "test";
        var aa = 1;
        var cbReceiptReference = Guid.NewGuid().ToString();
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = merchantId,
                    Series = series, // This should be defined
                    AA = aa,
                    HashAlg = "SHA256",
                    HashPayload = merchantId + "-" + series + "-" + aa + "-" + cbReceiptReference + "-" + chargeItems.Sum(x => x.Amount)
                }
            },
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().BeNull();

        action.Should().NotBeNull();
        action!.invoice[0].invoiceHeader.series.Should().Be(receiptResponse.ftCashBoxIdentification);
        action!.invoice[0].invoiceHeader.aa.Should().Be(291.ToString());
    }
}
