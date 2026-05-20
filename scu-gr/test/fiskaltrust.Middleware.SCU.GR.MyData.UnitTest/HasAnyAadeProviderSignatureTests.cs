using System;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class HasAnyAadeProviderSignatureTests
{
    private static InvoicesDoc Doc(params PaymentMethodDetailType[][] invoicesPayments) =>
        new InvoicesDoc
        {
            invoice = Array.ConvertAll(invoicesPayments, p => new AadeBookInvoiceType { paymentMethods = p })
        };

    private static PaymentMethodDetailType Payment(string? transactionId = null, string? providerSignature = null) =>
        new PaymentMethodDetailType
        {
            transactionId = transactionId,
            ProvidersSignature = providerSignature == null ? null : new ProviderSignatureType { Signature = providerSignature, SigningAuthor = "126" }
        };

    [Fact]
    public void NoInvoices_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderSignature(new InvoicesDoc()).Should().BeFalse();

    [Fact]
    public void NoPayments_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(Array.Empty<PaymentMethodDetailType>())).Should().BeFalse();

    [Fact]
    public void PaymentWithoutSignature_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(new[] { Payment() })).Should().BeFalse();

    [Fact]
    public void PaymentWithEmptySignature_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(new[] { Payment(providerSignature: "") })).Should().BeFalse();

    [Fact]
    public void TransactionIdWithoutSignature_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(new[] { Payment(transactionId: "TXN-001") })).Should().BeFalse();

    [Fact]
    public void PaymentWithSignature_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(new[] { Payment(providerSignature: "sig-1") })).Should().BeTrue();

    [Fact]
    public void OnlySecondPaymentHasSignature_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(new[] { Payment(), Payment(providerSignature: "sig-2") })).Should().BeTrue();

    [Fact]
    public void OnlySecondInvoiceHasSignature_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderSignature(Doc(
            new[] { Payment() },
            new[] { Payment(providerSignature: "sig-3") })).Should().BeTrue();
}
