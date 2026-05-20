using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class HasAnyAadeProviderDataTests
{
    private const long LocalPayItemFlag = 0x0000_0001_0000_0000;

    private static PayItemCase GR(PayItemCase c) =>
        ((PayItemCase) 0x4752_2000_0000_0000).WithCase(c);

    private static PayItemCase WithLocalFlag(PayItemCase payItemCase) =>
        (PayItemCase) ((long) payItemCase | LocalPayItemFlag);

    private static ReceiptRequest BuildRequest(List<PayItem>? payItems = null) =>
        new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = payItems ?? new List<PayItem>(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.ECommerce0x0004)
        };

    private static PayItem BuildPayItem(object? caseData = null) =>
        new PayItem
        {
            Position = 1,
            Amount = 10.0m,
            Description = "Card",
            ftPayItemCase = WithLocalFlag(GR(PayItemCase.DebitCardPayment)),
            ftPayItemCaseData = caseData
        };

    [Fact]
    public void NoPayItems_ReturnsFalse()
    {
        var request = BuildRequest();

        AADEFactory.HasAnyAadeProviderData(request).Should().BeFalse();
    }

    [Fact]
    public void PayItemWithoutCaseData_ReturnsFalse()
    {
        var request = BuildRequest(new List<PayItem> { BuildPayItem() });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeFalse();
    }

    [Fact]
    public void UnrelatedCaseData_ReturnsFalse()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new { somethingElse = "foo", nested = new { another = "bar" } })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeFalse();
    }

    [Fact]
    public void EmptyAadeFieldValues_ReturnsFalse()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new
            {
                aadeTransactionId = "",
                aadeProviderSignature = "",
                aadeProviderId = "",
                aadeProviderSignatureData = ""
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeFalse();
    }

    [Theory]
    [InlineData("aadeTransactionId")]
    [InlineData("aadeProviderSignature")]
    [InlineData("aadeProviderId")]
    [InlineData("aadeProviderSignatureData")]
    public void AnySingleAadeFieldAtRoot_ReturnsTrue(string fieldName)
    {
        var caseData = new Dictionary<string, object> { [fieldName] = "some-value" };
        var request = BuildRequest(new List<PayItem> { BuildPayItem(caseData) });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeTrue();
    }

    [Fact]
    public void CloudApiNestedProviderData_ReturnsTrue()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new
            {
                Provider = new
                {
                    Protocol = "",
                    ProtocolRequest = new
                    {
                        aadeProviderSignatureData = "",
                        aadeProviderSignature = "cloud-signature-123"
                    },
                    ProtocolResponse = new
                    {
                        aadeTransactionId = "TXN-CLOUD-001"
                    }
                }
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeTrue();
    }

    [Fact]
    public void GenericAadeSignatureData_ReturnsTrue()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new
            {
                aadeSignatureData = new
                {
                    aadeProviderSignature = "generic-signature-456",
                    aadeTransactionId = "TXN-GENERIC-002"
                }
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeTrue();
    }

    [Fact]
    public void App2AppUrlQueryWithAadeFields_ReturnsTrue()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new
            {
                Provider = new
                {
                    Protocol = "vivapayclient",
                    ProtocolRequest = "vivapayclient://pay?merchantKey=abc&aadeProviderSignature=sig-xyz",
                    ProtocolResponse = "myapp://result?status=success&aadeTransactionId=TXN-APP-001"
                }
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeTrue();
    }

    [Fact]
    public void App2AppUrlQueryWithoutAadeFields_ReturnsFalse()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new
            {
                Provider = new
                {
                    Protocol = "vivapayclient",
                    ProtocolRequest = "vivapayclient://pay?merchantKey=abc&amount=10",
                    ProtocolResponse = "myapp://result?status=success"
                }
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeFalse();
    }

    [Fact]
    public void App2AppUrlWithEmptyAadeValue_ReturnsFalse()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new
            {
                Provider = new
                {
                    ProtocolRequest = "vivapayclient://pay?aadeProviderSignature="
                }
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeFalse();
    }

    [Fact]
    public void OnlySecondPayItemCarriesAadeData_ReturnsTrue()
    {
        var request = BuildRequest(new List<PayItem>
        {
            BuildPayItem(new { somethingElse = "no aade here" }),
            BuildPayItem(new
            {
                aadeSignatureData = new { aadeTransactionId = "TXN-2" }
            })
        });

        AADEFactory.HasAnyAadeProviderData(request).Should().BeTrue();
    }
}
