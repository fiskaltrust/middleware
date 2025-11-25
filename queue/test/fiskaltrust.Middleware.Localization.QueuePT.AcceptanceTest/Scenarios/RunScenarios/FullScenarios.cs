using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Models;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class FullScenarios : AbstractScenarioTests
{

    public FullScenarios() : base(Guid.Parse("e88001df-7883-4978-819e-7260e2e57b6f"), Guid.NewGuid())
    {
        
    }

    [Fact]
    public async Task RunScenario()
    {
        // 5_1 A simplified invoice (Article 40 of the CIVA) for a customer who has provided their VAT number
        var receipt_5_1 = """
            {
              "cbReceiptReference": "cd6eb598-6aa3-4b13-9197-3791a8760791",
              "cbReceiptMoment": "2025-09-16T04:16:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 100,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": "199998132"
              }
            }
            """;

        var (receipt_5_1_Request, receipt_5_1_Response) = await ProcessReceiptAsync(receipt_5_1);
        receipt_5_1_Response.ftState.State().Should().Be(State.Success);

        // 5_2 A annulled invoice (Article 36 of the CIVA) and its PDF after annulment, which visibly states that the document has been annulled, not forgetting the entry in the application database and in the relevant SAF-T(PT) field.
        var receipt_5_2 = """
            {
              "cbReceiptReference": "d06af8dc-75c9-499d-9ad6-39811d015143",
              "cbReceiptMoment": "2025-09-16T04:16:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 100,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450280961,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": "199998132"
              },
              "cbPreviousReceiptReference": "cd6eb598-6aa3-4b13-9197-3791a8760791"
            }
            """;
        var (receipt_5_2_Request, receipt_5_2_Response) = await ProcessReceiptAsync(receipt_5_2);
        receipt_5_2_Response.ftState.State().Should().Be(State.Success);

        // 5_3 A document that can be handed over to the customer to verify the transfer goods or the provision services
        var receipt_5_3 = """
            {
              "cbReceiptReference": "78d44b9b-c5e6-4fca-83b0-97d7e7ab81b4",
              "cbReceiptMoment": "2025-09-16T04:18:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450031108,
              "cbUser": "Stefan Kert"
            }
            """;

        var (receipt_5_3_Request, receipt_5_3_Response) = await ProcessReceiptAsync(receipt_5_3);
        receipt_5_3_Response.ftState.State().Should().Be(State.Success);

        // 5_4 An invoice based on the document issued in point 5.3. {must generate the OrderReferences element)
        var receipt_5_4 = """
            {
              "cbReceiptReference": "7454ba2b-000d-4681-b9ad-f3d9f0c3c0ff",
              "cbReceiptMoment": "2025-09-16T04:19:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 150,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450022913,
              "cbPreviousReceiptReference": "78d44b9b-c5e6-4fca-83b0-97d7e7ab81b4",
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": "199998132"
              }
            }
            """;
        var (receipt_5_4_Request, receipt_5_4_Response) = await ProcessReceiptAsync(receipt_5_4);
        receipt_5_4_Response.ftState.State().Should().Be(State.Success);

        // 5_5 A credit note based on the invoice from point 5.4 (must generate the References element) If you have not complied with the previous point, you must create a credit note on another document
        var receipt_5_5 = """
            {
              "cbReceiptReference": "46fc3f0e-baf4-46bb-a097-2f3445f1aea0",
              "cbReceiptMoment": "2025-09-16T04:20:53",
              "cbChargeItems": [
                {
                  "Quantity": -1,
                  "Description": "Line item 1",
                  "Amount": -150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Quantity": -1,
                  "Description": "Numerario",
                  "Amount": -150,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605466800129,
              "cbPreviousReceiptReference": "7454ba2b-000d-4681-b9ad-f3d9f0c3c0ff",
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": "199998132"
              }
            }
            """;
        var (receipt_5_5_Request, receipt_5_5_Response) = await ProcessReceiptAsync(receipt_5_5);
        receipt_5_5_Response.ftState.State().Should().Be(State.Success);

        // 5_6 An invoice with 4 product lines, where the 1st line must contain a product at the reduced VAT rate, the 2nd line must contain a product exempt from VAT (the TaxExemptionReason element must be generated), the 3rd line must contain a product at the intermediate rate and the 4th line contain the product at the standard rate
        var receipt_5_6 = """
            {
              "cbReceiptReference": "8ba262ae-5d9f-48fd-971a-bc7db50440ca",
              "cbReceiptMoment": "2025-09-16T04:21:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 6,
                  "ftChargeItemCase": 5788286605450018833,
                  "Position": 1
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 2",
                  "Amount": 50,
                  "VATRate": 0,
                  "ftChargeItemCase": 5788286605450035224,
                  "Position": 2
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 25,
                  "VATRate": 13,
                  "ftChargeItemCase": 5788286605450018834,
                  "Position": 3
                },
                {
                  "Quantity": 1,
                  "Description": "Service Line item 1",
                  "Amount": 12.5,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018851,
                  "Position": 4
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 187.5,
                  "ftPayItemCase": 5788286605450018817,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450022913,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": "199998132"
              }
            }
            """;
        var (receipt_5_6_Request, receipt_5_6_Response) = await ProcessReceiptAsync(receipt_5_6);
        receipt_5_6_Response.ftState.State().Should().Be(State.Success);

        // 5_7 A document with 2 product lines with the following characteristics: the 1 line must refer to a transfer of goods or provision of services with quantity 100, unit price 0.SP and contain a line discount of 8.8%. The document must also be given an overall discount (generate the SettlementAmount element)
        var receipt_5_7 = """
            {
              "cbReceiptReference": "9d5ee916-e22f-4244-8fe7-6a471ccbe06d",
              "cbReceiptMoment": "2025-09-16T04:22:53",
              "cbChargeItems": [
                {
                  "Quantity": 100,
                  "Description": "Line item 1",
                  "Amount": 55.00,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835,
                  "Position": 1
                },
                {
                  "Quantity": 1,
                  "Description": "Discount Line item 1",
                  "Amount": -4.84000,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450280979,
                  "Position": 1
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 12.5,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835,
                  "Position": 2
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 62.66,
                  "ftPayItemCase": 5788286605450018817,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_5_7_Request, receipt_5_7_Response) = await ProcessReceiptAsync(receipt_5_7);
        receipt_5_7_Response.ftState.State().Should().Be(State.Success);

        // 5_8 A document in foreign currency
        var receipt_5_8 = """
            {
              "cbReceiptReference": "9d5ee916-e22f-4244-8fe7-6a471ccbe06d",
              "cbReceiptMoment": "2025-09-16T04:22:53",
              "cbChargeItems": [
                {
                  "Quantity": 100,
                  "Description": "Line item 1",
                  "Amount": 55.00,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835,
                  "Position": 1
                },
                {
                  "Quantity": 1,
                  "Description": "Discount Line item 1",
                  "Amount": -4.84000,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450280979,
                  "Position": 1
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 12.5,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835,
                  "Position": 2
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 62.66,
                  "ftPayItemCase": 5788286605450018817,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "Currency": "USD",
            }
            """;
        var (receipt_5_8_Request, receipt_5_8_Response) = await ProcessReceiptAsync(receipt_5_8);
        // Currency USD is not supported, so we expect an error state
        receipt_5_8_Response.ftState.State().Should().Be(State.Error);

        // 5_9 A document, for an identified customer but who has not indicated the TIN, in which the total field (GrossTotal) is less than €1.00 and the SystemEntryDate value is recorded until 10 am
        var receipt_5_9 = """
            {
              "cbReceiptReference": "c597063c-8b12-489c-891b-08e8d51b2cec",
              "cbReceiptMoment": "2025-09-16T04:24:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 0.50,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 0.50,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": null
              }
            }
            """;
        var (receipt_5_9_Request, receipt_5_9_Response) = await ProcessReceiptAsync(receipt_5_9);
        receipt_5_9_Response.ftState.State().Should().Be(State.Success);

        // 5_10 A document for another identified client who has also not indicated their VAT number
        var receipt_5_10 = """
            {
              "cbReceiptReference": "bd086cbb-fccd-42b8-a12d-c8a8cf7d1ded",
              "cbReceiptMoment": "2025-09-16T04:25:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 150,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Stefan Kert",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-190",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": null
              }
            }
            """;
        var (receipt_5_10_Request, receipt_5_10_Response) = await ProcessReceiptAsync(receipt_5_10);
        receipt_5_10_Response.ftState.State().Should().Be(State.Success);

        // 5_11 Two delivery or transport notes, one of which is valued and the other is not
        var receipt_5_11 = """
            {
              "cbReceiptReference": "bd086cbb-fccd-42b8-a12d-c8a8cf7d1ded",
              "cbReceiptMoment": "2025-09-16T04:25:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 150,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605517127685,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Stefan Kert",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-190",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": null
              }
            }
            """;
        var (receipt_5_11_Request, receipt_5_11_Response) = await ProcessReceiptAsync(receipt_5_11);
        // Transport documents are not supported today so this should fail
        receipt_5_11_Response.ftState.State().Should().Be(State.Error);

        // 5_12 A budget or pro forma invoice
        var receipt_5_12 = """
            {
              "cbReceiptReference": "00880b47-dccf-44b5-b01e-e5567206736e",
              "cbReceiptMoment": "2025-09-16T04:27:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450031108,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_5_12_Request, receipt_5_12_Response) = await ProcessReceiptAsync(receipt_5_12);
        receipt_5_12_Response.ftState.State().Should().Be(State.Success);

        // 5_13 Other Type of documents
        var receipt_5_13 = """
            {
              
            }
            """;
        var (receipt_5_13_Request, receipt_5_13_Response) = await ProcessReceiptAsync(receipt_5_13);
        receipt_5_13_Response.ftState.State().Should().Be(State.Error);

        // 5_13_1 A budget or pro forma invoice
        var receipt_5_13_1 = """
            {
              "cbReceiptReference": "c46856c7-537d-455e-804a-6f168f8ae53c",
              "cbReceiptMoment": "2025-09-16T04:28:53",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 150,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "A cr\u00E9dito",
                  "Amount": 150,
                  "ftPayItemCase": 5788286605450018825
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450022913,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerId": null,
                "CustomerType": null,
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerCountry": null,
                "CustomerVATId": "199998132"
              }
            }
            """;
        var (receipt_5_13_1_Request, receipt_5_13_1_Response) = await ProcessReceiptAsync(receipt_5_13_1);
        receipt_5_13_1_Response.ftState.State().Should().Be(State.Success);

        // 5_13_2 A budget or pro forma invoice
        var receipt_5_13_2 = """
            {
              "cbReceiptReference": "16ae19df-f482-46f3-93df-f82da7197507",
              "cbReceiptMoment": "2025-09-16T04:29:53",
              "cbChargeItems": [],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 187.5,
                  "ftPayItemCase": 5788286605450018817,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "e88001df-7883-4978-819e-7260e2e57b6f",
              "ftReceiptCase": 5788286605450018818,
              "cbPreviousReceiptReference": "c46856c7-537d-455e-804a-6f168f8ae53c",
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_5_13_2_Request, receipt_5_13_2_Response) = await ProcessReceiptAsync(receipt_5_13_2);
        receipt_5_13_2_Response.ftState.State().Should().Be(State.Success);

        var xmlData = await ExecuteJournal(new JournalRequest
        {
            From = DateTime.Parse("2025-01-01T00:00:00Z").Ticks,
            To = DateTime.Parse("2025-12-31T00:00:00Z").Ticks,
            ftJournalType = (JournalType) 0x5054_2000_0000_0001,
        });

        File.WriteAllBytes("C:\\GitHub\\market-pt\\doc\\certification\\Submissions\\2025-09\\phase1\\SAFT_fullscenario.xml", xmlData);
    }
}