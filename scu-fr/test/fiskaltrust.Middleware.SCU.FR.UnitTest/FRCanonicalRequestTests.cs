using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// The canonical request form of market-fr ADR 0013. The vectors in <c>TestData/fr-canonical-request-v1.json</c>
/// were computed by an independent implementation written from the ADR, and TaxAuditorTool checks the same
/// file, so a change here that breaks them breaks the verifier too.
/// </summary>
public class FRCanonicalRequestTests
{
    public static IEnumerable<object[]> Vectors() => ReadVectors(vector =>
    [
        vector.GetProperty("name").GetString()!,
        vector.GetProperty("request").GetRawText(),
        vector.GetProperty("canonical").GetString()!,
        vector.GetProperty("sha256").GetString()!,
        vector.GetProperty("signedField").GetString()!,
    ]);

    /// <summary>Each vector's request as the v2 queue stores it, which is what the archive and TaxAuditorTool read.</summary>
    public static IEnumerable<object[]> StoredVectors() => ReadVectors(vector =>
    [
        vector.GetProperty("name").GetString()!,
        vector.GetProperty("storedRequest").GetString()!,
        vector.GetProperty("canonical").GetString()!,
        vector.GetProperty("signedField").GetString()!,
    ]);

    private static List<object[]> ReadVectors(Func<JsonElement, object[]> select)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "fr-canonical-request-v1.json")));
        return document.RootElement.GetProperty("vectors").EnumerateArray().Select(select).ToList();
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void Vector_IsReproduced(string name, string requestJson, string canonical, string sha256, string signedField)
    {
        // Deserialized the way the v2 queue deserializes a request.
        var request = JsonSerializer.Deserialize<ReceiptRequest>(requestJson)!;

        Encoding.UTF8.GetString(FRCanonicalRequest.Serialize(request)).Should().Be(canonical, name);
        FRCanonicalRequest.Hash(request).Should().Be(sha256, name);
        FRCanonicalRequest.SignedField(request).Should().Be(signedField, name);
    }

    [Theory]
    [MemberData(nameof(StoredVectors))]
    public void Vector_IsReproducedFromTheStoredRequest(string name, string storedRequest, string canonical, string signedField)
    {
        // The queue stores the request it re-serialized; what it reads back gives the same form.
        var request = JsonSerializer.Deserialize<ReceiptRequest>(storedRequest)!;

        Encoding.UTF8.GetString(FRCanonicalRequest.Serialize(request)).Should().Be(canonical, name);
        FRCanonicalRequest.SignedField(request).Should().Be(signedField, name);
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void Vector_SurvivesAQueueRoundTrip(string name, string requestJson, string canonical, string sha256, string signedField)
    {
        // Serialized again by the contract in use now, not only by the one that wrote the vectors.
        var stored = JsonSerializer.Serialize(JsonSerializer.Deserialize<ReceiptRequest>(requestJson));
        var request = JsonSerializer.Deserialize<ReceiptRequest>(stored)!;

        Encoding.UTF8.GetString(FRCanonicalRequest.Serialize(request)).Should().Be(canonical, name);
        FRCanonicalRequest.SignedField(request).Should().Be(signedField, name).And.EndWith(sha256);
    }

    [Fact]
    public void Serialize_DoesNotDependOnTheMomentKindOfTheSameInstant()
    {
        var instant = new DateTime(2026, 11, 3, 9, 15, 30, DateTimeKind.Utc);
        var utc = new ReceiptRequest { cbReceiptReference = "r", cbReceiptMoment = instant };
        var local = new ReceiptRequest { cbReceiptReference = "r", cbReceiptMoment = instant.ToLocalTime() };
        var unspecified = new ReceiptRequest { cbReceiptReference = "r", cbReceiptMoment = DateTime.SpecifyKind(instant, DateTimeKind.Unspecified) };

        FRCanonicalRequest.Hash(local).Should().Be(FRCanonicalRequest.Hash(utc));
        FRCanonicalRequest.Hash(unspecified).Should().Be(FRCanonicalRequest.Hash(utc), "a moment without offset is taken as UTC");
    }

    [Fact]
    public void Serialize_IgnoresTheDecimalScale()
    {
        var a = new ReceiptRequest { cbReceiptReference = "r", cbChargeItems = [new ChargeItem { Amount = 1.50m, VATRate = 20.0m }] };
        var b = new ReceiptRequest { cbReceiptReference = "r", cbChargeItems = [new ChargeItem { Amount = 1.5m, VATRate = 20m }] };

        FRCanonicalRequest.Hash(a).Should().Be(FRCanonicalRequest.Hash(b));
    }

    [Fact]
    public void Serialize_ChangesWithAnySignedValue()
    {
        var a = JsonSerializer.Deserialize<ReceiptRequest>("""{"cbReceiptReference":"r","ftReceiptCaseData":{"x":1}}""")!;
        var b = JsonSerializer.Deserialize<ReceiptRequest>("""{"cbReceiptReference":"r","ftReceiptCaseData":{"x":2}}""")!;

        FRCanonicalRequest.Hash(a).Should().NotBe(FRCanonicalRequest.Hash(b));
    }

    [Fact]
    public void Serialize_WithALoneSurrogate_IsRejected()
    {
        var request = new ReceiptRequest { cbReceiptReference = "\udc00" };

        var act = () => FRCanonicalRequest.Serialize(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Serialize_WithADuplicateFreeFormMember_IsRejected()
    {
        var request = JsonSerializer.Deserialize<ReceiptRequest>("""{"cbReceiptReference":"r","cbUser":{"a":1,"a":2}}""")!;

        var act = () => FRCanonicalRequest.Serialize(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Serialize_WithANumberTooLongToWrite_IsRejected()
    {
        var request = JsonSerializer.Deserialize<ReceiptRequest>("""{"cbReceiptReference":"r","cbUser":1e1001}""")!;

        var act = () => FRCanonicalRequest.Serialize(request);

        act.Should().Throw<ArgumentException>();
    }
}
