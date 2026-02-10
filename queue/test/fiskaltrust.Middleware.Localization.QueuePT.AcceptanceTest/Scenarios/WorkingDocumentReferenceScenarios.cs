using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

/// <summary>
/// Tests for working document (ProForma, Budget, Table Check) references when converting to invoices or POS receipts.
/// Validates that signatures contain proper references to the original working documents.
/// </summary>
public class WorkingDocumentReferenceScenarios : AbstractScenarioTests
{
    #region ProForma (PF) Scenarios

    [Fact]
    public async Task ProForma_ToInvoice_ShouldContainReferenceSignature()
    {
        // Step 1: Create a ProForma receipt
        var proFormaReceipt = """
            {
                "cbReceiptReference": "proforma-ref-001",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 50,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 50,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User"
            }
            """;

        var (proFormaRequest, proFormaResponse) = await ProcessReceiptAsync(proFormaReceipt, (long) ((ReceiptCase) 0x0007).WithCountry("PT"));
        proFormaResponse.ftState.State().Should().Be(State.Success, because: "ProForma receipt should succeed. Errors: " + string.Join(", ", proFormaResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 2: Create an Invoice referencing the ProForma
        var invoiceReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 50,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 50,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User",
                "cbPreviousReceiptReference": "proforma-ref-001"
            }
            """;

        var (invoiceRequest, invoiceResponse) = await ProcessReceiptAsync(invoiceReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        invoiceResponse.ftState.State().Should().Be(State.Success, because: "Invoice should succeed. Errors: " + string.Join(", ", invoiceResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Verify the signature contains reference to ProForma
        var referenceSignature = invoiceResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia") && s.Data.Contains("Proforma"));
        referenceSignature.Should().NotBeNull("Invoice should contain a reference signature to the ProForma");
        referenceSignature!.Data.Should().Contain("PF", "Reference should mention the ProForma document type");
    }

    [Fact]
    public async Task ProForma_ToPosReceipt_ShouldContainReferenceSignature()
    {
        // Step 1: Create a ProForma receipt
        var proFormaReceipt = """
            {
                "cbReceiptReference": "proforma-ref-002",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 30,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 30,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User"
            }
            """;

        var (proFormaRequest, proFormaResponse) = await ProcessReceiptAsync(proFormaReceipt, (long) ((ReceiptCase) 0x0007).WithCountry("PT"));
        proFormaResponse.ftState.State().Should().Be(State.Success, because: "ProForma receipt should succeed. Errors: " + string.Join(", ", proFormaResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 2: Create a POS Receipt referencing the ProForma
        var posReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 30,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 30,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User",
                "cbPreviousReceiptReference": "proforma-ref-002"
            }
            """;

        var (posRequest, posResponse) = await ProcessReceiptAsync(posReceipt, (long) ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"));
        posResponse.ftState.State().Should().Be(State.Success, because: "POS Receipt should succeed. Errors: " + string.Join(", ", posResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Verify the signature contains reference to ProForma
        var referenceSignature = posResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia") && s.Data.Contains("Proforma"));
        referenceSignature.Should().NotBeNull("POS Receipt should contain a reference signature to the ProForma");
        referenceSignature!.Data.Should().Contain("PF", "Reference should mention the ProForma document type");
    }

    #endregion

    #region Table Check (CM) Scenarios

    [Fact]
    public async Task TableCheck_ToInvoice_ShouldContainReferenceSignature()
    {
        // Step 1: Create a Table Check receipt
        var tableCheckReceipt = """
            {
                "cbReceiptReference": "tablecheck-ref-001",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 45,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 45,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User"
            }
            """;

        var (tableCheckRequest, tableCheckResponse) = await ProcessReceiptAsync(tableCheckReceipt, (long) ((ReceiptCase) 0x0006).WithCountry("PT"));
        tableCheckResponse.ftState.State().Should().Be(State.Success, because: "Table Check receipt should succeed. Errors: " + string.Join(", ", tableCheckResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 2: Create an Invoice referencing the Table Check
        var invoiceReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 45,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 45,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User",
                "cbPreviousReceiptReference": "tablecheck-ref-001"
            }
            """;

        var (invoiceRequest, invoiceResponse) = await ProcessReceiptAsync(invoiceReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        invoiceResponse.ftState.State().Should().Be(State.Success, because: "Invoice should succeed. Errors: " + string.Join(", ", invoiceResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Verify the signature contains reference to Table Check
        var referenceSignature = invoiceResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia") && s.Data.Contains("Consulta de mesa"));
        referenceSignature.Should().NotBeNull("Invoice should contain a reference signature to the Table Check (Consulta de mesa)");
        referenceSignature!.Data.Should().Contain("CM", "Reference should mention the Table Check document type");
    }

    [Fact]
    public async Task TableCheck_ToPosReceipt_ShouldContainReferenceSignature()
    {
        // Step 1: Create a Table Check receipt
        var tableCheckReceipt = """
            {
                "cbReceiptReference": "tablecheck-ref-002",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 25,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 25,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User"
            }
            """;

        var (tableCheckRequest, tableCheckResponse) = await ProcessReceiptAsync(tableCheckReceipt, (long) ((ReceiptCase) 0x0006).WithCountry("PT"));
        tableCheckResponse.ftState.State().Should().Be(State.Success, because: "Table Check receipt should succeed. Errors: " + string.Join(", ", tableCheckResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 2: Create a POS Receipt referencing the Table Check
        var posReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 25,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 25,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User",
                "cbPreviousReceiptReference": "tablecheck-ref-002"
            }
            """;

        var (posRequest, posResponse) = await ProcessReceiptAsync(posReceipt, (long) ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"));
        posResponse.ftState.State().Should().Be(State.Success, because: "POS Receipt should succeed. Errors: " + string.Join(", ", posResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Verify the signature contains reference to Table Check
        var referenceSignature = posResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia") && s.Data.Contains("Consulta de mesa"));
        referenceSignature.Should().NotBeNull("POS Receipt should contain a reference signature to the Table Check (Consulta de mesa)");
        referenceSignature!.Data.Should().Contain("CM", "Reference should mention the Table Check document type");
    }

    #endregion
}
