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
    private const string DocumentoNaoServeFatura = "Este documento não serve de fatura";

    #region Working Document Signature Tests - "Este documento não serve de fatura"

    [Fact]
    public async Task ProForma_ShouldContain_DocumentoNaoServeFatura_Signature()
    {
        // Arrange - Working documents must not have payment items
        var proFormaReceipt = """
            {
                "cbReceiptReference": "proforma-doc-signature-001",
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
                "cbPayItems": [],
                "cbUser": "Test User"
            }
            """;

        // Act
        var (request, response) = await ProcessReceiptAsync(proFormaReceipt, (long) ((ReceiptCase) 0x0007).WithCountry("PT"));

        // Assert
        response.ftState.State().Should().Be(State.Success, because: "ProForma receipt should succeed. Errors: " + string.Join(", ", response.ftSignatures?.Select(s => s.Data) ?? []));
        
        var documentoNaoSignature = response.ftSignatures.FirstOrDefault(s => s.Data.Contains(DocumentoNaoServeFatura));
        documentoNaoSignature.Should().NotBeNull($"ProForma (PF) working document should contain the signature '{DocumentoNaoServeFatura}'");
    }


    [Fact]
    public async Task TableCheck_ShouldContain_DocumentoNaoServeFatura_Signature()
    {
        // Arrange - Working documents must not have payment items
        var tableCheckReceipt = """
            {
                "cbReceiptReference": "tablecheck-doc-signature-001",
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
                "cbPayItems": [],
                "cbUser": "Test User"
            }
            """;

        // Act
        var (request, response) = await ProcessReceiptAsync(tableCheckReceipt, (long) ((ReceiptCase) 0x0006).WithCountry("PT"));

        // Assert
        response.ftState.State().Should().Be(State.Success, because: "Table Check receipt should succeed. Errors: " + string.Join(", ", response.ftSignatures?.Select(s => s.Data) ?? []));
        
        var documentoNaoSignature = response.ftSignatures.FirstOrDefault(s => s.Data.Contains(DocumentoNaoServeFatura));
        documentoNaoSignature.Should().NotBeNull($"Table Check (CM) working document should contain the signature '{DocumentoNaoServeFatura}'");
    }

    [Fact]
    public async Task Budget_ShouldContain_DocumentoNaoServeFatura_Signature()
    {
        // Budget is a sub-case of ProForma (0x0007) with flag 0x0000_0002_0000_0000
        // Arrange - Working documents must not have payment items
        var budgetReceipt = """
            {
                "cbReceiptReference": "budget-doc-signature-001",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 75,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [],
                "cbUser": "Test User"
            }
            """;

        // Act - Budget uses ProForma (0x0007) with Budget flag (0x0000_0002_0000_0000)
        var budgetReceiptCase = (long) (((ReceiptCase) 0x0007).WithCountry("PT") | (ReceiptCase) 0x0000_0002_0000_0000);
        var (request, response) = await ProcessReceiptAsync(budgetReceipt, budgetReceiptCase);

        // Assert
        response.ftState.State().Should().Be(State.Success, because: "Budget (OR) receipt should succeed. Errors: " + string.Join(", ", response.ftSignatures?.Select(s => s.Data) ?? []));
        
        var documentoNaoSignature = response.ftSignatures.FirstOrDefault(s => s.Data.Contains(DocumentoNaoServeFatura));
        documentoNaoSignature.Should().NotBeNull($"Budget (OR) working document should contain the signature '{DocumentoNaoServeFatura}'");
    }

    [Fact]
    public async Task ProForma_WithPayItems_ShouldFail()
    {
        // Arrange - Working documents must not have payment items
        var proFormaReceipt = """
            {
                "cbReceiptReference": "proforma-with-pay-001",
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

        // Act
        var (request, response) = await ProcessReceiptAsync(proFormaReceipt, (long) ((ReceiptCase) 0x0007).WithCountry("PT"));

        // Assert
        response.ftState.State().Should().Be(State.Error, because: "ProForma with payment items should fail");
        response.ftSignatures.Should().Contain(s => s.Data.Contains("EEEE_WorkingDocumentPayItemsNotAllowed"));
    }

    [Fact]
    public async Task TableCheck_WithPayItems_ShouldFail()
    {
        // Arrange - Working documents must not have payment items
        var tableCheckReceipt = """
            {
                "cbReceiptReference": "tablecheck-with-pay-001",
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

        // Act
        var (request, response) = await ProcessReceiptAsync(tableCheckReceipt, (long) ((ReceiptCase) 0x0006).WithCountry("PT"));

        // Assert
        response.ftState.State().Should().Be(State.Error, because: "Table Check with payment items should fail");
        response.ftSignatures.Should().Contain(s => s.Data.Contains("EEEE_WorkingDocumentPayItemsNotAllowed"));
    }

    [Fact]
    public async Task Budget_WithPayItems_ShouldFail()
    {
        // Budget is a sub-case of ProForma (0x0007) with flag 0x0000_0002_0000_0000
        // Arrange - Working documents must not have payment items
        var budgetReceipt = """
            {
                "cbReceiptReference": "budget-with-pay-001",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 75,
                        "Description": "Test Product",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 75,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User"
            }
            """;

        // Act - Budget uses ProForma (0x0007) with Budget flag (0x0000_0002_0000_0000)
        var budgetReceiptCase = (long) (((ReceiptCase) 0x0007).WithCountry("PT") | (ReceiptCase) 0x0000_0002_0000_0000);
        var (request, response) = await ProcessReceiptAsync(budgetReceipt, budgetReceiptCase);

        // Assert
        response.ftState.State().Should().Be(State.Error, because: "Budget with payment items should fail");
        response.ftSignatures.Should().Contain(s => s.Data.Contains("EEEE_WorkingDocumentPayItemsNotAllowed"));
    }

    #endregion

    #region ProForma (PF) Scenarios

    [Fact]
    public async Task ProForma_ToInvoice_ShouldContainReferenceSignature()
    {
        // Step 1: Create a ProForma receipt - Working documents must not have payment items
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
                "cbPayItems": [],
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
        var referenceSignature = invoiceResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia"));
        referenceSignature.Should().NotBeNull("Invoice should contain a reference signature to the ProForma");
        referenceSignature!.Data.Should().Contain("PF", "Reference should mention the ProForma document type");
    }


    [Fact]
    public async Task ProForma_ToPosReceipt_ShouldContainReferenceSignature()
    {
        // Step 1: Create a ProForma receipt - Working documents must not have payment items
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
                "cbPayItems": [],
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
        var referenceSignature = posResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia"));
        referenceSignature.Should().NotBeNull("POS Receipt should contain a reference signature to the ProForma");
        referenceSignature!.Data.Should().Contain("PF", "Reference should mention the ProForma document type");
    }


    #endregion

    #region Table Check (CM) Scenarios

    [Fact]
    public async Task TableCheck_ToInvoice_ShouldContainReferenceSignature()
    {
        // Step 1: Create a Table Check receipt - Working documents must not have payment items
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
                "cbPayItems": [],
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
        var referenceSignature = invoiceResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia"));
        referenceSignature.Should().NotBeNull("Invoice should contain a reference signature to the Table Check (Consulta de mesa)");
        referenceSignature!.Data.Should().Contain("CM", "Reference should mention the Table Check document type");
    }

    [Fact]
    public async Task TableCheck_ToPosReceipt_ShouldContainReferenceSignature()
    {
        // Step 1: Create a Table Check receipt - Working documents must not have payment items
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
                "cbPayItems": [],
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
        var referenceSignature = posResponse.ftSignatures.FirstOrDefault(s => s.Data.Contains("Referencia"));
        referenceSignature.Should().NotBeNull("POS Receipt should contain a reference signature to the Table Check (Consulta de mesa)");
        referenceSignature!.Data.Should().Contain("CM", "Reference should mention the Table Check document type");
    }

    #endregion
}
