using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;
using fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT;

public class SAFTTests
{
    [Fact]
    public void AuditFile_Encoding_ShouldBe_Windows1252()
    {
        var data = SAFTMapping.SerializeAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [], 0);
        data.Should().StartWith("<?xml version=\"1.0\" encoding=\"windows-1252\"?>");
    }

    [Fact]
    public void AuditFile_ProductId_ShouldBe_Correct()
    {
        var data = SAFTMapping.CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [], 0);
        data.Header.ProductID.Should().Be("fiskaltrust.CloudCashBox/FISKALTRUST CONSULTING GMBH - Sucursal em Portugal");
    }

    [Fact]
    public void AuditFile_TaxTable_ShouldBeEmptys()
    {
        var data = SAFTMapping.CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [], 0);
        data.MasterFiles.TaxTable!.TaxTableEntry.Should().BeEmpty();
    }

    [Fact]
    public void AuditFile_With_ChargeItems_WithDifferentVATRates_Should_Include_TaxTableEntries()
    {
        var chargeItems = new List<ChargeItem>
        {
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = PTVATRates.Normal,
                    VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Normal),
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.NormalCase,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 1,
                    Amount = 50,
                    VATRate = PTVATRates.Discounted1,
                    VATAmount = VATHelpers.CalculateVAT(50, PTVATRates.Discounted1),
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.Discounted1Case,
                    Quantity = 1,
                    Description = "Line item 1"
                }
        };
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbUser = 1
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = Guid.Parse("1573f671-047a-4327-8e8f-f832254a1a88"),
            ftQueueItemID = Guid.Parse("82c0f7c5-24e3-49fd-b40e-f253a926126e"),
            ftQueueRow = 44,
            ftCashBoxIdentification = "fiskaltrust65957",
            ftCashBoxID = Guid.Parse("5c4a97c9-954a-46f2-bc3c-583244f5fc61"),
            cbTerminalID = "1",
            cbReceiptReference = "8e3a715d-818d-4116-a143-ebacad2fbb15",
            ftReceiptIdentification = "FS ft2024/0012",
            ftReceiptMoment = DateTime.Parse("2025-03-07T15:05:39.1147334Z"),
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem
                {
                    ftSignatureFormat = (SignatureFormat) 0x10001,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0012,
                    Caption = "Hash",
                    Data = "AiNOquIhokFqvkFu8fwPUH/IXyYkIV6tpDgVLBBKiKxtJJPYAZu3w91Jc+P+G0lbeKl5x4YsfZm2YNS7xgoOAlTGc/xZZ8xfqZxVZ2f3qORSxk29gaIg1saIX7zsECBfxcGr4n4SWy/b0ejt9RdtY0iXbZA0w6RlJRZyyBqcLgs="
                },
                new SignatureItem
                {
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0014,
                    Caption = "AFU6 - Processado por programa certificado",
                    Data = "No 9999/AT"
                },
                new SignatureItem
                {
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0010,
                    Caption = "",
                    Data = "ATCUD: XBPRP1M-12"
                },
                new SignatureItem
                {
                    ftSignatureFormat = SignatureFormat.QRCode,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0001,
                    Caption = "[www.fiskaltrust.pt]",
                    Data = "A:980833310*B:199998132*C:PT*D:FS*E:N*F:20250307*G:FS ft2024/0012*H:XBPRP1M-12*I1:PT*I2:0.00*I3:0.00*I4:0.00*I5:0.00*I6:0.00*I7:81.30*I8:18.70*N:18.70*O:100.00*Q:AFU6*R:9999*S:ftQueueId=1573f671-047a-4327-8e8f-f832254a1a88;ftQueueItemId=82c0f7c5-24e3-49fd-b40e-f253a926126e"
                }
            },
            ftState = (State) 5788286605450018816
        };
        var data = SAFTMapping.CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [new ftQueueItem {
            request = JsonSerializer.Serialize(receiptRequest),
            response = JsonSerializer.Serialize(receiptResponse)
        }], 0);
        data.MasterFiles.TaxTable!.TaxTableEntry.Should().HaveCount(2);
        data.MasterFiles.TaxTable!.TaxTableEntry.Should().ContainEquivalentOf(new TaxTableEntry
        {
            TaxType = "IVA",
            TaxCountryRegion = "PT",
            TaxCode = "RED",
            Description = "Taxa Reduzida",
            TaxPercentage = 6.000000m,
        });
        data.MasterFiles.TaxTable!.TaxTableEntry.Should().ContainEquivalentOf(new TaxTableEntry
        {
            TaxType = "IVA",
            TaxCountryRegion = "PT",
            TaxCode = "NOR",
            Description = "Taxa Normal",
            TaxPercentage = 23.000000m,
        });
    }

    [Fact]
    public void AuditFile_With_ChargeItems_WithSameName_ButDifferentPrices_ShouldCreate_TwoProducts()
    {
        var chargeItems = new List<ChargeItem>
        {
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = PTVATRates.Normal,
                    VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Normal),
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.NormalCase,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 1,
                    Amount = 50,
                    VATRate = PTVATRates.Discounted1,
                    VATAmount = VATHelpers.CalculateVAT(50, PTVATRates.Discounted1),
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.Discounted1Case,
                    Quantity = 1,
                    Description = "Line item 1"
                }
        };
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbUser = 1
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = Guid.Parse("1573f671-047a-4327-8e8f-f832254a1a88"),
            ftQueueItemID = Guid.Parse("82c0f7c5-24e3-49fd-b40e-f253a926126e"),
            ftQueueRow = 44,
            ftCashBoxIdentification = "fiskaltrust65957",
            ftCashBoxID = Guid.Parse("5c4a97c9-954a-46f2-bc3c-583244f5fc61"),
            cbTerminalID = "1",
            cbReceiptReference = "8e3a715d-818d-4116-a143-ebacad2fbb15",
            ftReceiptIdentification = "FS ft2024/0012",
            ftReceiptMoment = DateTime.Parse("2025-03-07T15:05:39.1147334Z"),
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem
                {
                    ftSignatureFormat = (SignatureFormat) 0x10001,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0012,
                    Caption = "Hash",
                    Data = "AiNOquIhokFqvkFu8fwPUH/IXyYkIV6tpDgVLBBKiKxtJJPYAZu3w91Jc+P+G0lbeKl5x4YsfZm2YNS7xgoOAlTGc/xZZ8xfqZxVZ2f3qORSxk29gaIg1saIX7zsECBfxcGr4n4SWy/b0ejt9RdtY0iXbZA0w6RlJRZyyBqcLgs="
                },
                new SignatureItem
                {
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0014,
                    Caption = "AFU6 - Processado por programa certificado",
                    Data = "No 9999/AT"
                },
                new SignatureItem
                {
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0010,
                    Caption = "",
                    Data = "ATCUD: XBPRP1M-12"
                },
                new SignatureItem
                {
                    ftSignatureFormat = SignatureFormat.QRCode,
                    ftSignatureType = (SignatureType) 0x5054_2000_0000_0001,
                    Caption = "[www.fiskaltrust.pt]",
                    Data = "A:980833310*B:199998132*C:PT*D:FS*E:N*F:20250307*G:FS ft2024/0012*H:XBPRP1M-12*I1:PT*I2:0.00*I3:0.00*I4:0.00*I5:0.00*I6:0.00*I7:81.30*I8:18.70*N:18.70*O:100.00*Q:AFU6*R:9999*S:ftQueueId=1573f671-047a-4327-8e8f-f832254a1a88;ftQueueItemId=82c0f7c5-24e3-49fd-b40e-f253a926126e"
                }
            },
            ftState = (State) 5788286605450018816
        };
        var data = SAFTMapping.CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [new ftQueueItem {
            request = JsonSerializer.Serialize(receiptRequest),
            response = JsonSerializer.Serialize(receiptResponse)
        }], 0);
        data.MasterFiles.Product!.Should().HaveCount(2);
    }
}
