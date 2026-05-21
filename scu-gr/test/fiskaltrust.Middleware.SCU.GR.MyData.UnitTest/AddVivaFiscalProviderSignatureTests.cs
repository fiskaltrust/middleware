using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class AddVivaFiscalProviderSignatureTests
{
    private const string VivaSignatureCaption = "www.viva.com";

    private static ReceiptCase Case(ReceiptCase c) =>
        ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(c);

    private static ProcessRequest BuildRequest(ReceiptCase receiptCase) =>
        new ProcessRequest
        {
            ReceiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = receiptCase,
                cbChargeItems = new List<ChargeItem>(),
                cbPayItems = new List<PayItem>()
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftSignatures = new List<SignatureItem>()
            }
        };

    private static InvoicesDoc DocWithSignature(string? signature) =>
        new InvoicesDoc
        {
            invoice = new[]
            {
                new AadeBookInvoiceType
                {
                    paymentMethods = new[]
                    {
                        new PaymentMethodDetailType
                        {
                            ProvidersSignature = signature == null
                                ? null
                                : new ProviderSignatureType { Signature = signature, SigningAuthor = "126" }
                        }
                    }
                }
            }
        };

    private static InvoicesDoc EmptyDoc() =>
        new InvoicesDoc { invoice = new[] { new AadeBookInvoiceType() } };

    [Fact]
    public void NonEcommerce_WithoutProviderSignatureInPayload_StillAddsSignature()
    {
        var request = BuildRequest(Case(ReceiptCase.PointOfSaleReceipt0x0001));

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request, EmptyDoc());

        request.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == VivaSignatureCaption);
    }

    [Fact]
    public void Ecommerce_WithProviderSignatureInPayload_AddsSignature()
    {
        var request = BuildRequest(Case(ReceiptCase.ECommerce0x0004));

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request, DocWithSignature("sig-123"));

        request.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == VivaSignatureCaption);
    }

    [Fact]
    public void Ecommerce_WithoutProviderSignatureInPayload_ResetsSignature()
    {
        var request = BuildRequest(Case(ReceiptCase.ECommerce0x0004));

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request, EmptyDoc());

        request.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == VivaSignatureCaption);
    }

    [Fact]
    public void Ecommerce_WithEmptyProviderSignatureInPayload_ResetsSignature()
    {
        var request = BuildRequest(Case(ReceiptCase.ECommerce0x0004));

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request, DocWithSignature(""));

        request.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == VivaSignatureCaption);
    }

    [Fact]
    public void Ecommerce_WithNullProvidersSignatureObject_ResetsSignature()
    {
        var request = BuildRequest(Case(ReceiptCase.ECommerce0x0004));

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request, DocWithSignature(null));

        request.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == VivaSignatureCaption);
    }

    [Fact]
    public void Ecommerce_PreservesPreviouslyAddedSignatures()
    {
        var request = BuildRequest(Case(ReceiptCase.ECommerce0x0004));
        var preexisting = new SignatureItem { Caption = "mydata-xml", Data = "<xml/>", ftSignatureFormat = SignatureFormat.Text };
        request.ReceiptResponse.ftSignatures.Add(preexisting);

        SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request, EmptyDoc());

        request.ReceiptResponse.ftSignatures.Should().ContainSingle().Which.Should().BeSameAs(preexisting);
    }
}
