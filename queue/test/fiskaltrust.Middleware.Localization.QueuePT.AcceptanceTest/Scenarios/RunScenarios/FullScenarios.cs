using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class FullScenarios : AbstractScenarioTests
{

    public FullScenarios() : base(Guid.Parse("a8466a96-aa7e-40f7-bbaa-5303e60c7943"), Guid.NewGuid())
    {

    }

    [Fact]
    public async Task RunScenario()
    {
        using var scope = new AssertionScope();
        // 5_1 A simplified invoice (Article 40 of the CIVA) for a customer who has provided their VAT number
        var receipt_5_1 = """
            {
              "cbReceiptReference": "1dadd294-0f2e-4af5-b0a4-326d9d44a34d",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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
        receipt_5_1_Response.ftReceiptIdentification.Should().Be("ft0#FS ft20257d14/1");

        // 5_2 A annulled invoice (Article 36 of the CIVA) and its PDF after annulment, which visibly states that the document has been annulled, not forgetting the entry in the application database and in the relevant SAF-T(PT) field.
        var receipt_5_2 = """
            {
              "cbReceiptReference": "d06af8dc-75c9-499d-9ad6-39811d015143",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": -1,
                  "Description": "Line item 1",
                  "Amount": -100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                { 
                  "Quantity": -1,
                  "Description": "Numerario",
                  "Amount": -100,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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
              "cbPreviousReceiptReference": "1dadd294-0f2e-4af5-b0a4-326d9d44a34d"
            }
            """;
        var (receipt_5_2_Request, receipt_5_2_Response) = await ProcessReceiptAsync(receipt_5_2);
        receipt_5_2_Response.ftState.State().Should().Be(State.Success);
        receipt_5_2_Response.ftReceiptIdentification.Should().Be("ft1#FS ft20257d14/1");

        // 5_3 A document that can be handed over to the customer to verify the transfer goods or the provision services
        var receipt_5_3 = """
            {
              "cbReceiptReference": "367e60f0-ec99-4090-871a-99acfddfb0fb",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018823,
              "cbUser": "Stefan Kert"
            }
            """;

        var (receipt_5_3_Request, receipt_5_3_Response) = await ProcessReceiptAsync(receipt_5_3);
        receipt_5_3_Response.ftState.State().Should().Be(State.Success);
        receipt_5_3_Response.ftReceiptIdentification.Should().Be("ft2#PF ft20253a3b/1");

        // 5_4 An invoice based on the document issued in point 5.3. {must generate the OrderReferences element)
        var receipt_5_4 = """
            {
              "cbReceiptReference": "ba7db496-ddfb-49d8-a182-fa41e38dfafb",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450022913,
              "cbPreviousReceiptReference": "367e60f0-ec99-4090-871a-99acfddfb0fb",
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
        receipt_5_4_Response.ftReceiptIdentification.Should().Be("ft3#FT ft2025b814/1");

        // 5_5 A credit note based on the invoice from point 5.4 (must generate the References element) If you have not complied with the previous point, you must create a credit note on another document
        var receipt_5_5 = """
            {
              "cbReceiptReference": "e1eff8b4-d021-44be-8d2a-58f5fef775f4",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605466800129,
              "cbPreviousReceiptReference": "ba7db496-ddfb-49d8-a182-fa41e38dfafb",
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
        receipt_5_5_Response.ftReceiptIdentification.Should().Be("ft4#NC ft2025128b/1");

        // 5_6 An invoice with 4 product lines, where the 1st line must contain a product at the reduced VAT rate, the 2nd line must contain a product exempt from VAT (the TaxExemptionReason element must be generated), the 3rd line must contain a product at the intermediate rate and the 4th line contain the product at the standard rate
        var receipt_5_6 = """
            {
              "cbReceiptReference": "a5f94391-59e1-4a03-8ebf-3e2f9b548fb8",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
                  "ftChargeItemCase": 5788286605450024472,  
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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

        // 5054200000001618
        var (receipt_5_6_Request, receipt_5_6_Response) = await ProcessReceiptAsync(receipt_5_6);
        receipt_5_6_Response.ftState.State().Should().Be(State.Success);
        receipt_5_6_Response.ftReceiptIdentification.Should().Be("ft5#FT ft2025b814/2");

        // 5_7 A document with 2 product lines with the following characteristics: the 1 line must refer to a transfer of goods or provision of services with quantity 100, unit price 0.SP and contain a line discount of 8.8%. The document must also be given an overall discount (generate the SettlementAmount element)
        var receipt_5_7 = """
            {
              "cbReceiptReference": "548fd241-0ae1-4cef-8a67-341ba9ed3e55",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_5_7_Request, receipt_5_7_Response) = await ProcessReceiptAsync(receipt_5_7);
        receipt_5_7_Response.ftState.State().Should().Be(State.Success);
        receipt_5_7_Response.ftReceiptIdentification.Should().Be("ft6#FS ft20257d14/2");

        // 5_8 A document in foreign currency
        var receipt_5_8 = """
            {
              "cbReceiptReference": "9d5ee916-e22f-4244-8fe7-6a471ccbe06d",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "Currency": "USD"
            }
            """;
        var (receipt_5_8_Request, receipt_5_8_Response) = await ProcessReceiptAsync(receipt_5_8);
        // Currency USD is not supported, so we expect an error state
        receipt_5_8_Response.ftState.State().Should().Be(State.Error);

        // 5_9 A document, for an identified customer but who has not indicated the TIN, in which the total field (GrossTotal) is less than €1.00 and the SystemEntryDate value is recorded until 10 am
        var receipt_5_9 = """
            {
              "cbReceiptReference": "7410a647-5ca7-4b1f-a538-d243062c8c4e",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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
        receipt_5_9_Response.ftReceiptIdentification.Should().Be("ft7#FS ft20257d14/3");

        // 5_10 A document for another identified client who has also not indicated their VAT number
        var receipt_5_10 = """
            {
              "cbReceiptReference": "eed675d3-750e-4677-99ac-8ed8db1e7ed8",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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
        receipt_5_10_Response.ftReceiptIdentification.Should().Be("ft8#FS ft20257d14/4");

        // 5_11 Two delivery or transport notes, one of which is valued and the other is not
        var receipt_5_11 = """
            {
              "cbReceiptReference": "bd086cbb-fccd-42b8-a12d-c8a8cf7d1ded",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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
              "cbReceiptReference": "16e17740-27f2-4e91-9858-137a7323a652",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018823,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_5_12_Request, receipt_5_12_Response) = await ProcessReceiptAsync(receipt_5_12);
        receipt_5_12_Response.ftState.State().Should().Be(State.Success);
        receipt_5_12_Response.ftReceiptIdentification.Should().Be("ft9#PF ft20253a3b/2");

        // 5_13 Other Type of documents
        var receipt_5_13 = """
            {
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943"
            }
            """;
        var (receipt_5_13_Request, receipt_5_13_Response) = await ProcessReceiptAsync(receipt_5_13);
        receipt_5_13_Response.ftState.State().Should().Be(State.Error);

        // 5_13_1 A budget or pro forma invoice
        var receipt_5_13_1 = """
            {
              "cbReceiptReference": "2f480b2f-fe1b-4b18-9603-d21a7e2b2094",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 187.5,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "A cr\u00E9dito",
                  "Amount": 187.5,
                  "ftPayItemCase": 5788286605450018825
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
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
        receipt_5_13_1_Response.ftReceiptIdentification.Should().Be("ftA#FT ft2025b814/3");

        // 5_13_2 A budget or pro forma invoice
        var receipt_5_13_2 = """
            {
              "cbReceiptReference": "f88b4d9b-7381-4c0e-b4cb-9d221d613a79",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 187.5,
                        "Description": "Receivable",
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018818,
              "cbPreviousReceiptReference": "2f480b2f-fe1b-4b18-9603-d21a7e2b2094",
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_5_13_2_Request, receipt_5_13_2_Response) = await ProcessReceiptAsync(receipt_5_13_2);
        receipt_5_13_2_Response.ftState.State().Should().Be(State.Success);
        receipt_5_13_2_Response.ftReceiptIdentification.Should().Be("ftB#RG ft2025a4fa/1");

        scope.Dispose();
        try
        {
            var xmlData = await ExecuteJournal(new JournalRequest
            {
                From = DateTime.Parse("2025-01-01T00:00:00Z").Ticks,
                To = DateTime.Parse("2025-12-31T00:00:00Z").Ticks,
                ftJournalType = (JournalType) 0x5054_2000_0000_0001,
            });
            var data = Encoding.GetEncoding(1252).GetString(xmlData);
            File.WriteAllBytes("C:\\GitHub\\market-pt\\doc\\certification\\Submissions\\2025-11-18\\phase1\\SAFT_fullscenario.xml", Encoding.GetEncoding(1252).GetBytes(data));
        }
        catch (Exception ex)
        {
            //   scope.AddReportable($"Error during journal export: {ex}");
            throw;
        }

    }

    [Fact]
    public async Task RunScenario_2()
    {
        using var scope = new AssertionScope();
        // T6 Create a simplified invoice (Article 40 of CIVA) for a customer who only provided their VAT number (with no other identification information in the database either from previous or subsequent document issuance) and corresponding PDF
        var receipt_6 = """
            {
              "cbReceiptReference": "afde2a23-d71d-4c6f-8ecd-fe988919e692",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerVATId": "199998132"
              }
            }
            """;

        var (receipt_6_Request, receipt_6_Response) = await ProcessReceiptAsync(receipt_6);
        receipt_6_Response.ftState.State().Should().Be(State.Success);
        receipt_6_Response.ftReceiptIdentification.Should().Be("ft0#FS ft20257d14/1");

        // T7 Create an invoice (Article 36 of CIVA) and then cancel it (if the application allows it), with the cancellation being the responsibility of a user different from the one who created the document. If it is possible to print a cancelled document, generate the corresponding PDF that visibly shows that the document is cancelled
        var receipt_7 = """
            {
              "cbReceiptReference": "d06af8dc-75c9-499d-9ad6-39811d015143",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450280961,
              "cbUser": "Christina Kert",
              "cbCustomer": {
                "CustomerVATId": "199998132"
              },
              "cbPreviousReceiptReference": "afde2a23-d71d-4c6f-8ecd-fe988919e692"
            }
            """;
        var (receipt_7_Request, receipt_7_Response) = await ProcessReceiptAsync(receipt_7);
        receipt_7_Response.ftState.State().Should().Be(State.Success);
        receipt_7_Response.ftReceiptIdentification.Should().Be("ft1#FS ft20257d14/1");

        // T8 If the application allows it, create a document that can be delivered to the customer to verify the transfer of goods or provision of services (order, table consultation, verification document, etc.) and corresponding PDF
        var receipt_8 = """
            {
              "cbReceiptReference": "f7e2ef47-f5c3-43b8-8db3-7764cb3efd83",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018823,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_8_Request, receipt_8_Response) = await ProcessReceiptAsync(receipt_8);
        receipt_8_Response.ftState.State().Should().Be(State.Success);
        receipt_8_Response.ftReceiptIdentification.Should().Be("ft2#PF ft20253a3b/1");

        // T9 If the application allows it, create a transport document issued under the Goods in Circulation Regime (delivery note, transport note, etc.) and corresponding PDF in 4 copies (quadruplicate)
        var receipt_9 = """
            {
              "cbReceiptReference": "b6f6795b-9732-4fe5-9efd-1b62c9929bb3",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605517127685,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_9_Request, receipt_9_Response) = await ProcessReceiptAsync(receipt_9);
        receipt_9_Response.ftState.State().Should().Be(State.Error);

        // T10 If the application allows it, create a transport document issued under the Goods in Circulation Regime (delivery note, transport note, etc.) and corresponding PDF in 4 copies (quadruplicate)
        var receipt_10 = """
            {
              "cbReceiptReference": "f017bb65-cd0a-44c4-96fc-0957ebb26918",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018817,
              "cbPreviousReceiptReference": "f7e2ef47-f5c3-43b8-8db3-7764cb3efd83",
              "cbUser": "Christina Kert"
            }
            """;
        var (receipt_10_Request, receipt_10_Response) = await ProcessReceiptAsync(receipt_10);
        receipt_10_Response.ftState.State().Should().Be(State.Success);
        receipt_10_Response.ftReceiptIdentification.Should().Be("ft3#FS ft20257d14/2");

        // T11 Create a credit note based on that invoice (must generate the References element). If you did not comply with the previous point, you should create a credit note on another document
        var receipt_11 = """
            {
              "cbReceiptReference": "0a865647-5de0-4149-82b4-19b40d998573",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": -1,
                  "Description": "Line item 1",
                  "Amount": -100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Quantity": -1,
                  "Description": "Numerario",
                  "Amount": -100,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605466796033,
              "cbPreviousReceiptReference": "f017bb65-cd0a-44c4-96fc-0957ebb26918",
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_11_Request, receipt_11_Response) = await ProcessReceiptAsync(receipt_11);
        receipt_11_Response.ftState.State().Should().Be(State.Success);
        receipt_11_Response.ftReceiptIdentification.Should().Be("ft4#NC ft2025128b/1");

        // T12 is not supported

        // T13 Create an invoice in the Cash VAT Regime (or another document, if the program does not issue invoices) with 5 lines with the following characteristics (display the PDF). The exemption reasons for the 2nd line and 5th line must be different. This document must be issued under these conditions, even if the program does not issue invoices in the Cash VAT Regime: 1st line: 1 unit, Product, €12, 6% tax rate; 2nd line: 1 unit, Product, €10, 0% tax rate (must generate TaxExemptionReason element); 3rd line: 1 unit, Product, €10, 13% tax rate; 4th line: 1 unit, Service, €10, 23% tax rate; 5th line: 1 unit, Product, €10, 0% tax rate (must generate TaxExemptionReason element)
        var receipt_13 = """
            {
              "cbTerminalID": "1",
              "cbReceiptReference": "f9abebd7-bf1e-47d5-b40b-50c859f1f817",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 12,
                  "VATRate": 6,
                  "ftChargeItemCase": 5788286605450018833
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 2",
                  "Amount": 10,
                  "VATRate": 0,
                  "ftChargeItemCase": 5788286605450024472 
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 3",
                  "Amount": 10,
                  "VATRate": 13,
                  "ftChargeItemCase": 5788286605450018834
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 4",
                  "Amount": 10,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018851
                },
                {
                  "Quantity": 1,
                  "Description": "Line item 5",
                  "Amount": 10,
                  "VATRate": 0,
                  "ftChargeItemCase": 5788286605450020376 
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 52,
                  "ftPayItemCase": 5788286605450018817,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "89429453-2a76-4495-9b62-773b203d443f",
              "ftReceiptCase": 5788286605450022913,
              "cbReceiptAmount": 52,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerVATId": "199998132"
              }
            }
            """;


        var (receipt_13_Request, receipt_13_Response) = await ProcessReceiptAsync(receipt_13);
        receipt_13_Response.ftState.State().Should().Be(State.Success);
        receipt_13_Response.ftReceiptIdentification.Should().Be("ft5#FT ft2025b814/1");

        // T14 is not supported

        // T15 If the application allows it, create a partial credit note relating to the invoice from the previous point where the unit price of the 1st line is corrected by €2.00 and the return of the product unit from the 3rd line occurred. Create the corresponding PDF
        var receipt_15 = """
            {
              "cbTerminalID": "1",
              "cbReceiptReference": "f753d49a-06a7-4508-94c8-daa1d022aedb",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": -1,
                  "Description": "Line item 3",
                  "Amount": -10,
                  "VATRate": 13,
                  "ftChargeItemCase": 5788286605450149906
                }
              ],
              "cbPayItems": [
                {
                  "Quantity": -1,
                  "Description": "Numerario",
                  "Amount": -10,
                  "ftPayItemCase": 5788286605450149889,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "89429453-2a76-4495-9b62-773b203d443f",
              "ftReceiptCase": 5788286605450022913,
              "cbPreviousReceiptReference": "f9abebd7-bf1e-47d5-b40b-50c859f1f817",
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Nuno Cazeiro",
                "CustomerStreet": "Demo street",
                "CustomerZip": "1050-189",
                "CustomerCity": "Lissbon",
                "CustomerVATId": "199998132"
              }
            }
            """;

        var (receipt_15_Request, receipt_15_Response) = await ProcessReceiptAsync(receipt_15);
        receipt_15_Response.ftState.State().Should().Be(State.Success);
        receipt_15_Response.ftReceiptIdentification.Should().Be("ft6#NC ft2025128b/2");

        // T16 Create a document with 2 non-exempt product/service lines (with quantities greater than 1): the 1st line must contain 100 units of product, unit price (UnitPrice) of €0.55 and a discount of 8.8%. The second line must contain 3.5 units (or 4 if decimal values are not allowed) with unit price (UnitPrice) of €3.45. A global discount of 10% must also be granted to the document, if the application allows it, and corresponding PDF (must generate the SettlementAmount element). Note: in document lines, the UnitPrice field must be without tax and reflect line discounts and global discounts (header)
        var receipt_16 = """
            {
              "cbTerminalID": "1",
              "cbReceiptReference": "c3829e81-67b8-4348-a358-0764498beafc",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 100,
                  "Description": "Line item 1",
                  "Amount": 55.00,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                },
                {
                  "Quantity": 1,
                  "Description": "Desconto",
                  "Amount": -4.84,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450280979
                },
                {
                  "Quantity": 4,
                  "Description": "Line item 1",
                  "Amount": 13.8,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "A cr\u00E9dito",
                  "Amount": 63.96,
                  "ftPayItemCase": 5788286605450018825,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "cbfeda37-5afb-41a5-a6f0-4a7030604baa",
              "ftReceiptCase": 5788286605450022913,
              "cbReceiptAmount": 63.96,
              "cbUser": "Stefan Kert"
            }
            """;

        var (receipt_16_Request, receipt_16_Response) = await ProcessReceiptAsync(receipt_16);
        receipt_16_Response.ftState.State().Should().Be(State.Success);
        receipt_16_Response.ftReceiptIdentification.Should().Be("ft7#FT ft2025b814/2");

        // T17 Create an invoice whose payment was recorded in the application (if the application allows it) - must generate structure 4.1.4.20.6 Payment
        var receipt_17 = """
            {
              "cbReceiptReference": "f532647a-7673-49b3-a94b-8ce8bd4b981e",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [       
                {
                    "Quantity": 1,
                    "Amount": 63.96,
                    "Description": "Test",
                    "VATRate": 0,
                    "ftChargeItemCase": 5788286605450018968
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 63.96,
                  "ftPayItemCase": 5788286605450018817,
                  "Position": 1
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018818,
              "cbPreviousReceiptReference": "c3829e81-67b8-4348-a358-0764498beafc",
              "cbUser": "Stefan Kert"
            }
            """;

        var (receipt_17_Request, receipt_17_Response) = await ProcessReceiptAsync(receipt_17);
        receipt_17_Response.ftState.State().Should().Be(State.Success);
        receipt_17_Response.ftReceiptIdentification.Should().Be("ft8#RG ft2025a4fa/1");

        // T18 If the application allows it, create a document (with non-exempt lines if the application allows it), in foreign currency and corresponding PDF
        var receipt_18 = """
            {
              "cbReceiptReference": "9d5ee916-e22f-4244-8fe7-6a471ccbe06d",
              "cbReceiptMoment": "{{$isoTimestamp}}",
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
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "Currency": "USD"
            }
            """;
        var (receipt_18_Request, receipt_18_Response) = await ProcessReceiptAsync(receipt_18);
        // Currency USD is not supported, so we expect an error state
        receipt_18_Response.ftState.State().Should().Be(State.Error);

        // T19 Create a document (with non-exempt lines if the application allows it) whose GrossTotal is less than €1.00 but greater than 0.00 and corresponding PDF. If the application allows, use an identified customer (name) who makes a purchase for the 1st time and therefore without a customer record, but who has not provided the VAT number (use VAT number 999999990)
        var receipt_19 = """
            {
              "cbReceiptReference": "5a8007c5-3338-42cd-8221-d7c6b54f9f56",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 0.90,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 0.90,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "33a09fa3-91cc-45af-be7c-7859897e85a7",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Jakob Kert"
              }
            }
            """;
        var (receipt_19_Request, receipt_19_Response) = await ProcessReceiptAsync(receipt_19);
        receipt_19_Response.ftState.State().Should().Be(State.Success);
        receipt_19_Response.ftReceiptIdentification.Should().Be("ft9#FS ft20257d14/3");

        // T20 Create a document for an identified customer (name) who also makes a purchase for the 1st time, different from the previous one, but who also did not indicate the VAT number (use VAT number 999999990) and corresponding PDF
        var receipt_20 = """
            {
              "cbReceiptReference": "6732aff2-9514-4fb1-b90c-9488f1da2fdc",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 0.90,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 0.90,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "c74a33e0-38e7-4a30-824a-835c19898b18",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "Christoph Kert"
              }
            }
            """;
        var (receipt_20_Request, receipt_20_Response) = await ProcessReceiptAsync(receipt_20);
        receipt_20_Response.ftState.State().Should().Be(State.Success);
        receipt_20_Response.ftReceiptIdentification.Should().Be("ftA#FS ft20257d14/4");

        // T21 Create a document with 2000 units of a non-exempt product or service with a low unit price, if possible, create in the application with 0.001
        var receipt_21 = """
            {
              "cbReceiptReference": "ce63f531-79e3-4efb-bd1c-7106f3f6f9d4",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 2000,
                  "Description": "Low price product",
                  "Amount": 20,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 20,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "cbbe9edb-4d9a-490d-9fb7-af8b3b90d2a5",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_21_Request, receipt_21_Response) = await ProcessReceiptAsync(receipt_21);
        receipt_21_Response.ftState.State().Should().Be(State.Success);
        receipt_21_Response.ftReceiptIdentification.Should().Be("ftB#FS ft20257d14/5");

        // T22 Create an invoice whose SystemEntryDate is before 10 AM
        var receipt_22 = """
            {
              "cbReceiptReference": "231260a6-9183-4743-a24c-1b69ad24033b",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 20,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 20,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "266aac1e-af3e-4934-b531-d726564b7e73",
              "ftReceiptCase": 5788286605450018817,
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_22_Request, receipt_22_Response) = await ProcessReceiptAsync(receipt_22);
        receipt_22_Response.ftState.State().Should().Be(State.Success);
        receipt_22_Response.ftReceiptIdentification.Should().Be("ftC#FS ft20257d14/6");

        // T23 is not supported
        // T24 is not supported
        // T25 is not supported
        // T26 is not supported
        // T27 is not supported

        // T28 Simulate the integration of 2 manual documents according to point 2.4 of Dispatch No. 8632/2014 of July 3 of the Director General of the Tax and Customs Authority where the 1st document belongs to series F with No. 23 from 14-01-2022 and the 2nd belongs to series D with No. 3 from 12-01-2022 and corresponding PDF. (Note that, according to point 2.4.2, a new document of the same type must be created that collects all elements of the manual document issued, meaning that all elements are free entry, for example: usual denomination, quantity, price, tax value, total tax and document, etc.)
        var receipt_28 = """
            {
              "cbReceiptReference": "F/23",
              "cbReceiptMoment": "2022-01-14T00:00:00",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Manual document F/23 - Product item",
                  "Amount": 50,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 50,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "632c29fe-61db-426a-a7fd-3df22a7ac949",
              "ftReceiptCase": 5788286605450543105,
              "ftReceiptCaseData": {
                "PT": {
                  "Series": "F",
                  "Number": 23
                }
              },
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_28_Request, receipt_28_Response) = await ProcessReceiptAsync(receipt_28);
        receipt_28_Response.ftState.State().Should().Be(State.Success);
        receipt_28_Response.ftReceiptIdentification.Should().Be("ftD#FS ft20250a62/1");

        // T28_2 Simulate the integration of 2 manual documents according to point 2.4 of Dispatch No. 8632/2014 of July 3 of the Director General of the Tax and Customs Authority where the 1st document belongs to series F with No. 23 from 14-01-2022 and the 2nd belongs to series D with No. 3 from 12-01-2022 and corresponding PDF. (Note that, according to point 2.4.2, a new document of the same type must be created that collects all elements of the manual document issued, meaning that all elements are free entry, for example: usual denomination, quantity, price, tax value, total tax and document, etc.)
        var receipt_28_2 = """
            {
              "cbReceiptReference": "D/3",
              "cbReceiptMoment": "2022-01-12T00:00:00",
              "cbChargeItems": [
                {
                  "Quantity": 2,
                  "Description": "Manual document D/3 - Service item",
                  "Amount": 75,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018851
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 75,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftPosSystemId": "76c746fc-7e94-4bd3-9506-ecfa69642335",
              "ftReceiptCase": 5788286605450543105,
              "ftReceiptCaseData": {
                "PT": {
                  "Series": "D",
                  "Number": 3
                }
              },
              "cbUser": "Stefan Kert"
            }
            """;
        var (receipt_28_2_Request, receipt_28_2_Response) = await ProcessReceiptAsync(receipt_28_2);
        receipt_28_2_Response.ftState.State().Should().Be(State.Success);
        receipt_28_2_Response.ftReceiptIdentification.Should().Be("ftE#FS ft20250a62/2");

        // T29 is not supported
        // T30 is not supported
        // T31 is not supported
        // T32 is not supported

        // T33_1_CM CM => If the application allows it, create a document that can be delivered to the customer to verify the transfer of goods or provision of services (order, table consultation, verification document, etc.) and corresponding PDF
        var receipt_33_1_CM = """
            {
              "cbReceiptReference": "1d7b26f4-a76a-43ea-93b5-0f337feb8528",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286605450018822,
              "cbUser": "Stefan Kert"
            }
            """;

        // 5788286609744986117
        // 5054200000000006

        var (receipt_33_1_CM_Request, receipt_33_1_CM_Response) = await ProcessReceiptAsync(receipt_33_1_CM);
        receipt_33_1_CM_Response.ftState.State().Should().Be(State.Success);
        receipt_33_1_CM_Response.ftReceiptIdentification.Should().Be("ftF#CM ft20259c2f/1");

        // T33_2_OR OR => If the application allows it, create a document that can be delivered to the customer to verify the transfer of goods or provision of services (order, table consultation, verification document, etc.) and corresponding PDF
        var receipt_33_2_OR = """
            {
              "cbReceiptReference": "ca7354eb-3265-477d-b17a-1ef2b9a57c2f",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Line item 1",
                  "Amount": 100,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [],
              "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
              "ftReceiptCase": 5788286614039953415,
              "cbUser": "Stefan Kert"
            }
            """;

        // 5054200200000007
        var (receipt_33_2_OR_Request, receipt_33_2_OR_Response) = await ProcessReceiptAsync(receipt_33_2_OR);
        receipt_33_2_OR_Response.ftState.State().Should().Be(State.Success);
        receipt_33_2_OR_Response.ftReceiptIdentification.Should().Be("ft10#OR ft20255389/1");

        scope.Dispose();

        var xmlData = await ExecuteJournal(new JournalRequest
        {
            From = DateTime.Parse("2025-01-01T00:00:00Z").Ticks,
            To = DateTime.Parse("2025-12-31T00:00:00Z").Ticks,
            ftJournalType = (JournalType) 0x5054_2000_0000_0001,
        });
        var data = Encoding.GetEncoding(1252).GetString(xmlData);
        File.WriteAllBytes("C:\\GitHub\\market-pt\\doc\\certification\\Submissions\\2025-11-18\\phase2\\SAFT_fullscenario.xml", Encoding.GetEncoding(1252).GetBytes(data));
    }
}