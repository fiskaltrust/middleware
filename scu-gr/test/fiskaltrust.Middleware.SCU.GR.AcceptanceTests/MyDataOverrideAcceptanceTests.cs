using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.GR.AcceptanceTests
{
    /// <summary>
    /// Acceptance tests for the MyData Override feature.
    /// These tests simulate real-world scenarios where JSON with override data is sent to ProcessReceipt.
    /// </summary>
    [Trait("Category", "Acceptance")]
    public class MyDataOverrideAcceptanceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly AADEFactory _aadeFactory;

        public MyDataOverrideAcceptanceTests(ITestOutputHelper output)
        {
            _output = output;
            _aadeFactory = new AADEFactory(new fiskaltrust.storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new fiskaltrust.storage.V0.MasterData.AccountMasterData
                {
                    VatId = "112545020"
                },
                Outlet = new fiskaltrust.storage.V0.MasterData.OutletMasterData
                {
                    LocationId = "0"
                }
            }, "https://test.receipts.example.com");
        }

        private ReceiptResponse CreateExampleResponse()
        {
            return new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftCashBoxIdentification = "TEST-CASHBOX-001",
                ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X"),
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State)0x4752_2000_0000_0000
            };
        }

        /// <summary>
        /// Test Case 1: Basic delivery note with dispatch information from JSON
        /// </summary>
        [Fact]
        public async Task DeliveryNote_WithDispatchInfo_ShouldProcessSuccessfully()
        {
            // Arrange - Simulate receiving JSON from POS system
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-01"",
                ""cbReceiptReference"": ""DELIVERY-2025-001"",
                ""cbReceiptMoment"": ""2025-06-18T10:30:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 10,
                        ""Description"": ""Product A"",
                        ""Amount"": 100.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T10:30:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""Cash"",
                        ""Amount"": 100.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T10:30:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {
                            ""invoice"": {
                                ""invoiceHeader"": {
                                    ""dispatchDate"": ""2025-06-18T12:00:00Z"",
                                    ""dispatchTime"": ""2025-06-18T14:30:00Z"",
                                    ""movePurpose"": 1
                                }
                            }
                        }
                    }
                }
            }";

            // Act - Deserialize JSON and process
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                invoiceDoc.Should().NotBeNull();
                invoiceDoc!.invoice[0].invoiceHeader.dispatchDateSpecified.Should().BeTrue();
                invoiceDoc.invoice[0].invoiceHeader.dispatchDate.Date.Should().Be(new DateTime(2025, 6, 18).Date);
                invoiceDoc.invoice[0].invoiceHeader.dispatchTimeSpecified.Should().BeTrue();
                invoiceDoc.invoice[0].invoiceHeader.dispatchTime.Hour.Should().Be(14);
                invoiceDoc.invoice[0].invoiceHeader.dispatchTime.Minute.Should().Be(30);
                invoiceDoc.invoice[0].invoiceHeader.movePurposeSpecified.Should().BeTrue();
                invoiceDoc.invoice[0].invoiceHeader.movePurpose.Should().Be(1);

                _output.WriteLine("? Dispatch date, time, and move purpose successfully applied from JSON");
            }
        }

        /// <summary>
        /// Test Case 2: Complete delivery note with Greek addresses from JSON
        /// </summary>
        [Fact]
        public async Task DeliveryNote_WithGreekAddresses_ShouldProcessSuccessfully()
        {
            // Arrange - Simulate receiving JSON with Greek characters
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-01"",
                ""cbReceiptReference"": ""DELIVERY-2025-042"",
                ""cbReceiptMoment"": ""2025-06-18T08:00:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 50,
                        ""Description"": ""?a?ad?t?? ?????? 1"",
                        ""Amount"": 500.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T08:00:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""?????µ?"",
                        ""Amount"": 500.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T08:00:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {
                            ""invoice"": {
                                ""invoiceHeader"": {
                                    ""dispatchDate"": ""2025-06-18T12:00:00Z"",
                                    ""dispatchTime"": ""2025-06-18T14:30:00Z"",
                                    ""movePurpose"": 1,
                                    ""otherDeliveryNoteHeader"": {
                                        ""loadingAddress"": {
                                            ""street"": ""?apad?aµ??t? 24"",
                                            ""number"": ""0"",
                                            ""postalCode"": ""56429"",
                                            ""city"": ""??a ???a?p?a - Tessa??????""
                                        },
                                        ""deliveryAddress"": {
                                            ""street"": ""??????? 22"",
                                            ""number"": ""0"",
                                            ""postalCode"": ""54622"",
                                            ""city"": ""T?SS???????""
                                        },
                                        ""startShippingBranch"": 0,
                                        ""completeShippingBranch"": 0
                                    }
                                }
                            }
                        }
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                invoiceDoc.Should().NotBeNull();

                var invoice = invoiceDoc!.invoice[0];
                invoice.invoiceHeader.otherDeliveryNoteHeader.Should().NotBeNull();

                // Verify loading address
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.Should().NotBeNull();
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("?apad?aµ??t? 24");
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.postalCode.Should().Be("56429");
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.city.Should().Be("??a ???a?p?a - Tessa??????");

                // Verify delivery address
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.Should().NotBeNull();
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.street.Should().Be("??????? 22");
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.postalCode.Should().Be("54622");
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.city.Should().Be("T?SS???????");

                // Verify shipping branches
                invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranchSpecified.Should().BeTrue();
                invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranchSpecified.Should().BeTrue();

                _output.WriteLine("? Greek addresses successfully processed from JSON");
                _output.WriteLine($"  Loading: {invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street}");
                _output.WriteLine($"  Delivery: {invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.street}");
            }
        }

        /// <summary>
        /// Test Case 3: Multi-branch shipping scenario from JSON
        /// </summary>
        [Fact]
        public async Task MultiBranchShipping_WithFullDetails_ShouldProcessSuccessfully()
        {
            // Arrange
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-WAREHOUSE"",
                ""cbReceiptReference"": ""TRANSFER-2025-055"",
                ""cbReceiptMoment"": ""2025-06-18T09:15:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 100,
                        ""Description"": ""?µp??e?µa µetaf????"",
                        ""Amount"": 1000.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T09:15:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""?s?te???? µetaf???"",
                        ""Amount"": 1000.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T09:15:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {
                            ""invoice"": {
                                ""invoiceHeader"": {
                                    ""dispatchDate"": ""2025-06-18T12:00:00Z"",
                                    ""dispatchTime"": ""2025-06-18T14:30:00Z"",
                                    ""movePurpose"": 2,
                                    ""otherDeliveryNoteHeader"": {
                                        ""loadingAddress"": {
                                            ""street"": ""?d?? ?p?????? 10"",
                                            ""number"": ""10"",
                                            ""postalCode"": ""15123"",
                                            ""city"": ""????a""
                                        },
                                        ""deliveryAddress"": {
                                            ""street"": ""?d?? ?atast?µat?? 25"",
                                            ""number"": ""25"",
                                            ""postalCode"": ""54622"",
                                            ""city"": ""Tessa??????""
                                        },
                                        ""startShippingBranch"": 1,
                                        ""completeShippingBranch"": 5
                                    }
                                }
                            }
                        }
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                invoiceDoc.Should().NotBeNull();

                var invoice = invoiceDoc!.invoice[0];

                // Verify move purpose for transfer
                invoice.invoiceHeader.movePurpose.Should().Be(2, "move purpose 2 indicates transfer");

                // Verify shipping branches
                invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranch.Should().Be(1);
                invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranch.Should().Be(5);

                // Verify addresses include branch numbers
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.number.Should().Be("10");
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.number.Should().Be("25");

                _output.WriteLine("? Multi-branch transfer successfully processed");
                _output.WriteLine($"  From Branch: {invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranch}");
                _output.WriteLine($"  To Branch: {invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranch}");
            }
        }

        /// <summary>
        /// Test Case 4: Partial override (only some fields) from JSON
        /// </summary>
        [Fact]
        public async Task PartialOverride_OnlyDispatchDate_ShouldProcessSuccessfully()
        {
            // Arrange
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-01"",
                ""cbReceiptReference"": ""RECEIPT-2025-100"",
                ""cbReceiptMoment"": ""2025-06-18T15:00:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 5,
                        ""Description"": ""Service Item"",
                        ""Amount"": 250.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T15:00:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""Card Payment"",
                        ""Amount"": 250.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T15:00:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {
                            ""invoice"": {
                                ""invoiceHeader"": {
                                    ""dispatchDate"": ""2025-06-19""
                                }
                            }
                        }
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                invoiceDoc.Should().NotBeNull();

                var invoice = invoiceDoc!.invoice[0];

                // Verify only dispatch date is set
                invoice.invoiceHeader.dispatchDateSpecified.Should().BeTrue();
                invoice.invoiceHeader.dispatchDate.Date.Should().Be(new DateTime(2025, 6, 19).Date);

                // Verify other fields are NOT set
                invoice.invoiceHeader.dispatchTimeSpecified.Should().BeFalse();
                invoice.invoiceHeader.movePurposeSpecified.Should().BeFalse();
                invoice.invoiceHeader.otherDeliveryNoteHeader.Should().BeNull();

                _output.WriteLine("? Partial override with only dispatch date successfully processed");
            }
        }

        /// <summary>
        /// Test Case 5: XML generation and validation with overrides
        /// </summary>
        [Fact]
        public async Task CompleteDeliveryNote_ShouldGenerateValidXML()
        {
            // Arrange
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-01"",
                ""cbReceiptReference"": ""XML-TEST-001"",
                ""cbReceiptMoment"": ""2025-06-18T12:00:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 20,
                        ""Description"": ""Test Product"",
                        ""Amount"": 200.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T12:00:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""Payment"",
                        ""Amount"": 200.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T12:00:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {
                            ""invoice"": {
                                ""invoiceHeader"": {
                                    ""dispatchDate"": ""2025-06-18T12:00:00Z"",
                                    ""dispatchTime"": ""2025-06-18T12:00:00Z"",
                                    ""movePurpose"": 1,
                                    ""otherDeliveryNoteHeader"": {
                                        ""loadingAddress"": {
                                            ""street"": ""?apad?aµ??t? 24"",
                                            ""number"": ""0"",
                                            ""postalCode"": ""56429"",
                                            ""city"": ""??a ???a?p?a""
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);
            var xml = AADEFactory.GenerateInvoicePayload(invoiceDoc!);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                xml.Should().NotBeNullOrEmpty();

                // Verify XML contains override fields
                xml.Should().Contain("<dispatchDate>");
                xml.Should().Contain("<dispatchTime>");
                xml.Should().Contain("<movePurpose>1</movePurpose>");
                xml.Should().Contain("<loadingAddress>");
                xml.Should().Contain("?apad?aµ??t? 24");

                // Verify XML can be deserialized back
                var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
                using var stringReader = new StringReader(xml);
                var deserializedDoc = (InvoicesDoc)xmlSerializer.Deserialize(stringReader)!;

                deserializedDoc.Should().NotBeNull();
                deserializedDoc.invoice[0].invoiceHeader.dispatchDateSpecified.Should().BeTrue();
                deserializedDoc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("?apad?aµ??t? 24");

                _output.WriteLine("? XML generated and validated successfully");
                _output.WriteLine("");
                _output.WriteLine("Generated XML (excerpt):");
                _output.WriteLine(xml.Length > 500 ? xml.Substring(0, 500) + "..." : xml);
            }
        }

        /// <summary>
        /// Test Case 6: Invalid JSON should fail gracefully
        /// </summary>
        [Fact]
        public async Task InvalidOverrideJSON_ShouldHandleGracefully()
        {
            // Arrange - JSON with malformed override structure
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-01"",
                ""cbReceiptReference"": ""INVALID-001"",
                ""cbReceiptMoment"": ""2025-06-18T15:00:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 1,
                        ""Description"": ""Test"",
                        ""Amount"": 10.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T15:00:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""Cash"",
                        ""Amount"": 10.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T15:00:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": null
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull("null override should not cause errors");
                invoiceDoc.Should().NotBeNull("invoice should still be created without overrides");

                _output.WriteLine("? Null override handled gracefully without errors");
            }
        }

        /// <summary>
        /// Test Case 7: Empty override object should not affect invoice
        /// </summary>
        [Fact]
        public async Task EmptyOverride_ShouldNotAffectInvoice()
        {
            // Arrange
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""TERMINAL-01"",
                ""cbReceiptReference"": ""EMPTY-001"",
                ""cbReceiptMoment"": ""2025-06-18T15:00:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 1,
                        ""Description"": ""Test"",
                        ""Amount"": 10.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T15:00:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""Cash"",
                        ""Amount"": 10.00,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T15:00:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {}
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                invoiceDoc.Should().NotBeNull();

                var invoice = invoiceDoc!.invoice[0];

                // Verify no override fields are set
                invoice.invoiceHeader.dispatchDateSpecified.Should().BeFalse();
                invoice.invoiceHeader.dispatchTimeSpecified.Should().BeFalse();
                invoice.invoiceHeader.movePurposeSpecified.Should().BeFalse();
                invoice.invoiceHeader.otherDeliveryNoteHeader.Should().BeNull();

                _output.WriteLine("? Empty override object processed without affecting invoice");
            }
        }

        /// <summary>
        /// Test Case 8: Real-world scenario - Restaurant delivery order
        /// </summary>
        [Fact]
        public async Task RestaurantDelivery_WithCompleteData_ShouldProcessSuccessfully()
        {
            // Arrange - Realistic restaurant delivery scenario
            var jsonRequest = @"{
                ""ftCashBoxID"": ""11111111-1111-1111-1111-111111111111"",
                ""ftPosSystemId"": ""22222222-2222-2222-2222-222222222222"",
                ""cbTerminalID"": ""POS-RESTAURANT-01"",
                ""cbReceiptReference"": ""ORDER-" + DateTime.UtcNow.Ticks + @""",
                ""cbReceiptMoment"": ""2025-06-18T19:30:00Z"",
                ""cbChargeItems"": [
                    {
                        ""Position"": 1,
                        ""Quantity"": 2,
                        ""Description"": ""??tsa ?a??a??ta"",
                        ""Amount"": 16.00,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T19:30:00Z""
                    },
                    {
                        ""Position"": 2,
                        ""Quantity"": 1,
                        ""Description"": ""Sa??ta ?????t???"",
                        ""Amount"": 6.50,
                        ""VATRate"": 24.00,
                        ""ftChargeItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T19:30:00Z""
                    }
                ],
                ""cbPayItems"": [
                    {
                        ""Quantity"": 1,
                        ""Description"": ""???ta"",
                        ""Amount"": 22.50,
                        ""ftPayItemCase"": 5139205309155246080,
                        ""Moment"": ""2025-06-18T19:30:00Z""
                    }
                ],
                ""ftReceiptCase"": 5139205309155246080,
                ""ftReceiptCaseData"": {
                    ""GR"": {
                        ""mydataoverride"": {
                            ""invoice"": {
                                ""invoiceHeader"": {
                                    ""dispatchDate"": ""2025-06-18T19:45:00Z"",
                                    ""dispatchTime"": ""2025-06-18T19:45:00Z"",
                                    ""movePurpose"": 1,
                                    ""otherDeliveryNoteHeader"": {
                                        ""loadingAddress"": {
                                            ""street"": ""???at?a? 150"",
                                            ""number"": ""150"",
                                            ""postalCode"": ""54636"",
                                            ""city"": ""Tessa??????""
                                        },
                                        ""deliveryAddress"": {
                                            ""street"": ""?s?µ?s?? 45"",
                                            ""number"": ""45"",
                                            ""postalCode"": ""54623"",
                                            ""city"": ""Tessa??????""
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }";

            // Act
            var receiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(jsonRequest);
            var receiptResponse = CreateExampleResponse();
            var (invoiceDoc, error) = _aadeFactory.MapToInvoicesDoc(receiptRequest!, receiptResponse);

            // Assert
            using (new AssertionScope())
            {
                error.Should().BeNull();
                invoiceDoc.Should().NotBeNull();

                var invoice = invoiceDoc!.invoice[0];

                // Verify restaurant details
                invoice.invoiceDetails.Should().HaveCount(2);
                invoice.invoiceDetails[0].netValue.Should().BeGreaterThan(0);

                // Verify delivery information
                invoice.invoiceHeader.dispatchTime.Hour.Should().Be(19);
                invoice.invoiceHeader.dispatchTime.Minute.Should().Be(45);
                invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("???at?a? 150");
                invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.street.Should().Be("?s?µ?s?? 45");

                _output.WriteLine("? Restaurant delivery order successfully processed");
                _output.WriteLine($"  Items: {invoice.invoiceDetails.Length}");
                _output.WriteLine($"  Total: {invoice.invoiceSummary.totalGrossValue:F2} EUR");
                _output.WriteLine($"  Dispatch Time: {invoice.invoiceHeader.dispatchTime:HH:mm:ss}");
            }
        }
    }
}

