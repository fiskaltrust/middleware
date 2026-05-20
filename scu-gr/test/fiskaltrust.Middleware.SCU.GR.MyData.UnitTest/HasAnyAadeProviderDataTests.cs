using System;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class HasAnyAadeProviderDataTests
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
        AADEFactory.HasAnyAadeProviderData(new InvoicesDoc()).Should().BeFalse();

    [Fact]
    public void NoPayments_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderData(Doc(Array.Empty<PaymentMethodDetailType>())).Should().BeFalse();

    [Fact]
    public void PaymentWithoutAadeFields_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderData(Doc(new[] { Payment() })).Should().BeFalse();

    [Fact]
    public void PaymentWithEmptyAadeFields_ReturnsFalse() =>
        AADEFactory.HasAnyAadeProviderData(Doc(new[] { Payment(transactionId: "", providerSignature: "") })).Should().BeFalse();

    [Fact]
    public void PaymentWithTransactionId_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderData(Doc(new[] { Payment(transactionId: "TXN-001") })).Should().BeTrue();

    [Fact]
    public void PaymentWithProviderSignature_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderData(Doc(new[] { Payment(providerSignature: "sig-1") })).Should().BeTrue();

    [Fact]
    public void OnlySecondPaymentHasAadeData_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderData(Doc(new[] { Payment(), Payment(transactionId: "TXN-2") })).Should().BeTrue();

    [Fact]
    public void OnlySecondInvoiceHasAadeData_ReturnsTrue() =>
        AADEFactory.HasAnyAadeProviderData(Doc(
            new[] { Payment() },
            new[] { Payment(providerSignature: "sig-2") })).Should().BeTrue();
}
