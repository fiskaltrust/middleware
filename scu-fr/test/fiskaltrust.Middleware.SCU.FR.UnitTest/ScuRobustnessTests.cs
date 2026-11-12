using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// SCU input variations never change what is signed or crash a receipt, and a configuration that would sign
/// something unverifiable is refused at start-up (market-fr#221).
/// </summary>
public class ScuRobustnessTests
{
    private static (string PrivateKey, string Certificate, string Serial) KeyAndCertificate()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var certificate = new CertificateRequest("CN=SCU FR", key, HashAlgorithmName.SHA256).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return (privateKey, Convert.ToBase64String(certificate.RawData), certificate.SerialNumber);
    }

    private static Dictionary<string, object> Lne(Action<Dictionary<string, object>>? change = null)
    {
        var configuration = new Dictionary<string, object>
        {
            ["Siret"] = FRTestData.Siret,
            ["PrivateKey"] = "aa",
            ["CertificateSerialNumber"] = "CERT-4711",
            ["LneCertificateNumber"] = "LNE-2026-042",
        };
        change?.Invoke(configuration);
        return configuration;
    }

    private static Func<Dictionary<string, object>, object> Reader(string scu) => scu switch
    {
        "LNE" => c => LneConfiguration.FromConfiguration(c),
        "SelfCertified" => c => SelfCertifiedConfiguration.FromConfiguration(c),
        _ => c => InfoCertConfiguration.FromConfiguration(c),
    };

    // LNE certificate data, validated at start-up (Ref LNE 1.8 Ex. 9: certificate serial, certification mention).

    [Theory]
    [InlineData("CertificateSerialNumber")]
    [InlineData("LneCertificateNumber")]
    public void TheLneScu_RequiresItsCertificateData(string key)
        => ((Action) (() => LneConfiguration.FromConfiguration(Lne(c => c.Remove(key))))).Should().Throw<FRValidationException>().WithMessage($"*{key}*");

    [Theory]
    [InlineData("LNE")]
    [InlineData("SelfCertified")]
    [InlineData("InfoCert")]
    public void TheConfiguredSerial_MustBeTheCertificatesSerial(string scu)
    {
        var (privateKey, certificate, serial) = KeyAndCertificate();
        var configuration = Lne(c => { c["PrivateKey"] = privateKey; c["CertificateBase64"] = certificate; c["CertificateSerialNumber"] = "CERT-4711"; });

        ((Action) (() => Reader(scu)(configuration))).Should().Throw<FRValidationException>().WithMessage($"*{serial}*");
    }

    [Theory]
    [InlineData("LNE")]
    [InlineData("SelfCertified")]
    [InlineData("InfoCert")]
    public void TheCertificatesSerial_IsTakenInAnyCaseAndWithSeparators(string scu)
    {
        var (privateKey, certificate, serial) = KeyAndCertificate();
        var written = string.Join(":", Enumerable.Range(0, serial.Length / 2).Select(i => serial.Substring(i * 2, 2))).ToLowerInvariant();
        var configuration = Lne(c => { c["PrivateKey"] = privateKey; c["CertificateBase64"] = certificate; c["CertificateSerialNumber"] = written; });

        ((Action) (() => Reader(scu)(configuration))).Should().NotThrow();
    }

    // A "|" in a field of the LNE and self-certified data set shifts every later field.

    [Theory]
    [InlineData("LNE", "Siret")]
    [InlineData("LNE", "CertificateSerialNumber")]
    [InlineData("SelfCertified", "Siret")]
    [InlineData("SelfCertified", "CertificateSerialNumber")]
    public void ADataSetSeparator_InASignedConfigurationValue_IsRefused(string scu, string key)
        => ((Action) (() => Reader(scu)(Lne(c => c[key] = "1234|5678")))).Should().Throw<FRValidationException>().WithMessage($"*{key}*");

    [Theory]
    [InlineData("LNE")]
    [InlineData("SelfCertified")]
    public async Task ADataSetSeparator_InTheCashBoxIdentification_RefusesTheReceipt(string scu)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        IFRSSCD sut = scu == "LNE"
            ? new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711", LneCertificateNumber = "LNE-1" })
            : new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711" });
        var request = FRTestData.Request();
        request.ReceiptResponse.ftCashBoxIdentification = "till|1";

        var (response, signature) = await sut.ProcessReceiptAsync(request, null);

        signature.Should().BeEmpty();
        ((long) response.ReceiptResponse.ftState).Should().Be((long) fiskaltrust.Middleware.SCU.FR.Abstraction.Cases.StateFR.Error, "a value the data set cannot hold is a refusal, not an unreachable SCU");
        JsonSerializer.Serialize(response.ReceiptResponse.ftSignatures).Should().Contain("ftCashBoxIdentification");
    }

    // The launcher hands Newtonsoft values and numbers in the configuration.

    [Theory]
    [InlineData("LNE")]
    [InlineData("SelfCertified")]
    [InlineData("InfoCert")]
    public void AConfigurationOfNewtonsoftValuesAndNumbers_IsRead(string scu)
    {
        var configuration = Lne(c =>
        {
            c["Siret"] = long.Parse(FRTestData.Siret);
            c["PrivateKey"] = new JValue("aa");
            c["SoftwareVersion"] = 2;
            c["Unrelated"] = JObject.Parse("{\"a\":{\"b\":1}}");
        });

        var result = Reader(scu)(configuration);

        JsonSerializer.Serialize(result, result.GetType()).Should().Contain($"\"Siret\":\"{FRTestData.Siret}\"").And.Contain("\"SoftwareVersion\":\"2\"").And.Contain("\"PrivateKey\":\"aa\"");
    }

    // The Launcher's sandbox key, read like every other configuration value (FRScuConfiguration).

    public static TheoryData<object, bool> SandboxValues => new()
    {
        { true, true },
        { "True", true },
        { new JValue(true), true },
        { new JValue("true"), true },
        { JsonDocument.Parse("true").RootElement, true },
        { false, false },
        { new JValue(false), false },
        { "no", false },
    };

    [Theory]
    [MemberData(nameof(SandboxValues))]
    public void TheSandboxKey_IsReadWhateverItsForm(object value, bool expected)
    {
        LneConfiguration.FromConfiguration(Lne(c => c["sandbox"] = value)).Sandbox.Should().Be(expected);
        InfoCertConfiguration.FromConfiguration(Lne(c => c["sandbox"] = value)).Sandbox.Should().Be(expected);
    }

    // The state data the SCU reads may come camelCased or as a Newtonsoft object.

    [Fact]
    public void TheStateData_IsReadWhateverItsCasing()
    {
        var response = new ReceiptResponse { ftStateData = JsonDocument.Parse("{\"fr\":{\"copyNumber\":2}}").RootElement };

        FRStateData.CopyNumber(response).Should().Be(2);
    }

    [Fact]
    public void TheStateData_IsReadFromANewtonsoftObject()
    {
        var response = new ReceiptResponse { ftStateData = JObject.Parse("{\"FR\":{\"CopyNumber\":3}}") };

        FRStateData.CopyNumber(response).Should().Be(3);
    }

    // Two rates the summary key cannot tell apart.

    [Fact]
    public async Task TwoRatesThatShareASummaryKey_SignOneSummaryLine()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(new InfoCertConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711" });
        var request = FRTestData.Request();
        var item = request.ReceiptRequest.cbChargeItems[0];
        request.ReceiptRequest.cbChargeItems =
        [
            new ChargeItem { Amount = item.Amount, VATRate = 5.5m, Quantity = 1, Description = "a", ftChargeItemCase = item.ftChargeItemCase },
            new ChargeItem { Amount = item.Amount, VATRate = 5.50000000000001m, Quantity = 1, Description = "b", ftChargeItemCase = item.ftChargeItemCase },
        ];

        var (response, signature) = await sut.ProcessReceiptAsync(request, null);

        signature.Should().NotBeEmpty();
    }
}
