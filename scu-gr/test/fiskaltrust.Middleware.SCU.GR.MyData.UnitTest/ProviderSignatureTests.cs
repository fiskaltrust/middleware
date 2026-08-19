using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// fiskaltrust issues Greek receipts under Viva's AADE provider licence (provider id 126), so the
/// provider block sits at the bottom of every successfully signed GR receipt.
///
/// https://github.com/fiskaltrust/market-gr/issues/276 — the provider's legal name has to be printed
/// above the licence, so the footer reads:
///
///     Viva Bank
///     www.viva.com
///     2024_12_126VIVA_001_ Viva Fiscal_V1_23122024
///
/// Every renderer (receipt-api HTML/PDF, the ESC/POS printer, POS-side printing) prints a signature
/// item as caption line followed by data line, so each footer line needs its own caption/data slot —
/// a "\n" inside one string collapses to a space in the HTML renderer.
/// </summary>
public class ProviderSignatureTests
{
    private static ProcessRequest GreekSaleRequest() => new()
    {
        ReceiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001,
            cbChargeItems = [],
            cbPayItems = []
        },
        ReceiptResponse = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftState = (State) 0x4752_2000_0000_0000,
            ftSignatures = new List<SignatureItem>()
        }
    };

    /// <summary>
    /// The three footer lines, in the order a receipt prints them.
    /// </summary>
    private static List<string> FooterLines(ProcessRequest request) =>
        request.ReceiptResponse.ftSignatures
            .SelectMany(x => new[] { x.Caption, x.Data })
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

    [Fact]
    public void AddVivaFiscalProviderSignature_ShouldPrintLegalNameWebsiteAndLicence_InThatOrder()
    {
        var request = GreekSaleRequest();

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request);

        FooterLines(request).Should().Equal(
            "VIVABANK ΑΝΩΝΥΜΗ ΤΡΑΠΕΖΙΚΗ ΕΤΑΙΡΕΙΑ",
            "www.viva.com",
            "2024_12_126VIVA_001_ Viva Fiscal_V1_23122024");
    }

    /// <summary>
    /// No line may be smuggled into a single string: the HTML renderer encodes caption and data into
    /// one paragraph each, so an embedded newline would print "Viva Bank www.viva.com" on one line.
    /// </summary>
    [Fact]
    public void AddVivaFiscalProviderSignature_ShouldNotPutSeveralLinesIntoOneCaptionOrData()
    {
        var request = GreekSaleRequest();

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request);

        foreach (var signature in request.ReceiptResponse.ftSignatures)
        {
            signature.Caption.Should().NotContain("\n");
            signature.Data.Should().NotContain("\n");
        }
    }

    /// <summary>
    /// Three lines occupy four caption/data slots, so one is empty. The ESC/POS printer prints an
    /// empty slot as a blank line, so the empty one must be last — never between two footer lines.
    /// </summary>
    [Fact]
    public void AddVivaFiscalProviderSignature_ShouldKeepTheEmptySlotLast_SoNoBlankLineSplitsTheFooter()
    {
        var request = GreekSaleRequest();

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request);

        var slots = request.ReceiptResponse.ftSignatures
            .SelectMany(x => new[] { x.Caption, x.Data })
            .ToList();

        slots.FindIndex(string.IsNullOrEmpty).Should().Be(slots.Count - 1);
    }

    /// <summary>
    /// The block stays a provider signature end to end, so consumers that filter on the type keep working.
    /// </summary>
    [Fact]
    public void AddVivaFiscalProviderSignature_ShouldMarkEveryLineAsAVisibleProviderSignature()
    {
        var request = GreekSaleRequest();

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request);

        request.ReceiptResponse.ftSignatures.Should().OnlyContain(x =>
            x.ftSignatureFormat == SignatureFormat.Text &&
            x.ftSignatureType == SignatureTypeGR.ProviderSignature.As<SignatureType>());
    }
}
