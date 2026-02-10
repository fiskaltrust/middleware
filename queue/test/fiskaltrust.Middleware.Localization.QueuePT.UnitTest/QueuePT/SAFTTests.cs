using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT;

public class SAFTTests
{
    [Fact]
    public void AuditFile_Encoding_ShouldBe_Windows1252()
    {
        var data = new SaftExporter().SerializeAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [], 0);
        Encoding.UTF8.GetString(data).Should().StartWith("<?xml version=\"1.0\" encoding=\"windows-1252\"?>");
    }

    [Fact]
    public void AuditFile_ProductId_ShouldBe_Correct()
    {
        var data = new SaftExporter().CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [], 0);
        data.Header.ProductID.Should().Be("fiskaltrust.CloudCashBox/FISKALTRUST CONSULTING GMBH - Sucursal em Portugal");
    }

    [Fact]
    public void AuditFile_TaxTable_ShouldBeEmptys()
    {
        var data = new SaftExporter().CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "999"
        }, [], 0);
        data.MasterFiles.TaxTable!.TaxTableEntry.Should().BeEmpty();
    }
}
