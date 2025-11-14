# MyData Override Feature Documentation

## Overview

The `mydataoverride` feature allows POS systems to have fine-grained control over specific MyData invoice fields that may not be directly mapped from standard receipt properties. This is particularly useful for delivery notes, shipping scenarios, and other specialized invoice types where additional Greek tax authority requirements must be met.

## Use Cases

- **Delivery Notes**: Specify loading and delivery addresses for goods transportation
- **Shipping Documentation**: Set dispatch dates, times, and shipping branches
- **Transportation Documents**: Define move purposes for goods movement tracking
- **Multi-location Businesses**: Control shipping branch information

## How It Works

The `mydataoverride` feature uses the `ftReceiptCaseData` field in the `ReceiptRequest` to pass override values that are applied directly to the MyData invoice before transmission to AADE (Greek Tax Authority).

All override fields are **optional** and only the specified fields will be overridden. Standard receipt processing continues normally, and overrides are applied at the final stage before XML generation.

## Supported Override Fields

### Invoice Header Overrides

All override fields are located under `ftReceiptCaseData.GR.mydataoverride.invoice.invoiceHeader`:

| Field | Type | MyData XML Field | Description |
|-------|------|------------------|-------------|
| `dispatchDate` | `string` (yyyy-MM-dd) | `dispatchDate` | The date when goods are dispatched |
| `dispatchTime` | `string` (HH:mm:ss) | `dispatchTime` | The time when goods are dispatched |
| `movePurpose` | `int` | `movePurpose` | Code indicating the purpose of goods movement |
| `startShippingBranch` | `int` | `otherDeliveryNoteHeader.startShippingBranch` | Branch ID where shipping starts |
| `completeShippingBranch` | `int` | `otherDeliveryNoteHeader.completeShippingBranch` | Branch ID where shipping completes |

### Delivery Note Header Overrides

Located under `ftReceiptCaseData.GR.mydataoverride.invoice.invoiceHeader.otherDeliveryNoteHeader`:

#### Loading Address
| Field | Type | MyData XML Field | Description |
|-------|------|------------------|-------------|
| `loadingAddress.street` | `string` | `loadingAddress.street` | Street name where goods are loaded |
| `loadingAddress.number` | `string` | `loadingAddress.number` | Street number (defaults to "0" if null) |
| `loadingAddress.postalCode` | `string` | `loadingAddress.postalCode` | Postal code of loading location |
| `loadingAddress.city` | `string` | `loadingAddress.city` | City name of loading location |

#### Delivery Address
| Field | Type | MyData XML Field | Description |
|-------|------|------------------|-------------|
| `deliveryAddress.street` | `string` | `deliveryAddress.street` | Street name where goods are delivered |
| `deliveryAddress.number` | `string` | `deliveryAddress.number` | Street number (defaults to "0" if null) |
| `deliveryAddress.postalCode` | `string` | `deliveryAddress.postalCode` | Postal code of delivery location |
| `deliveryAddress.city` | `string` | `deliveryAddress.city` | City name of delivery location |

## Receipt Request Structure

### Basic Structure

```json
{
  "ftReceiptCaseData": {
    "GR": {
      "mydataoverride": {
        "invoice": {
          "invoiceHeader": {
            "dispatchDate": "2025-06-18",
            "dispatchTime": "10:44:19",
            "movePurpose": 1,
            "startShippingBranch": 0,
            "completeShippingBranch": 0,
            "otherDeliveryNoteHeader": {
              "loadingAddress": {
                "street": "???????????? 24",
                "number": "0",
                "postalCode": "56429",
                "city": "??? ???????? - ???????????"
              },
              "deliveryAddress": {
                "street": "??????? 22",
                "number": "0",
                "postalCode": "54622",
                "city": "???????????"
              }
            }
          }
        }
      }
    }
  }
}
```

## Complete Examples

### Example 1: Simple Delivery Note with Dispatch Information

```json
{
  "ftCashBoxID": "11111111-1111-1111-1111-111111111111",
  "ftPosSystemId": "22222222-2222-2222-2222-222222222222",
  "cbTerminalID": "TERMINAL-01",
  "cbReceiptReference": "RECEIPT-2025-001",
  "cbReceiptMoment": "2025-06-18T10:30:00Z",
  "cbChargeItems": [
    {
      "Position": 1,
      "Quantity": 10,
      "Description": "Product A",
      "Amount": 100.00,
      "VATRate": 24.00,
      "ftChargeItemCase": 5332816059729461248,
      "Moment": "2025-06-18T10:30:00Z"
    }
  ],
  "cbPayItems": [
    {
      "Quantity": 1,
      "Description": "Cash",
      "Amount": 100.00,
      "ftPayItemCase": 5332816059729461248,
      "Moment": "2025-06-18T10:30:00Z"
    }
  ],
  "ftReceiptCase": 5332816059729461249,
  "ftReceiptCaseData": {
    "GR": {
      "mydataoverride": {
        "invoice": {
          "invoiceHeader": {
            "dispatchDate": "2025-06-18",
            "dispatchTime": "14:30:00",
            "movePurpose": 1
          }
        }
      }
    }
  }
}
```

### Example 2: Complete Delivery Note with Addresses

```json
{
  "ftCashBoxID": "11111111-1111-1111-1111-111111111111",
  "ftPosSystemId": "22222222-2222-2222-2222-222222222222",
  "cbTerminalID": "TERMINAL-01",
  "cbReceiptReference": "DELIVERY-2025-042",
  "cbReceiptMoment": "2025-06-18T08:00:00Z",
  "cbChargeItems": [
    {
      "Position": 1,
      "Quantity": 50,
      "Description": "????????? ?????? 1",
      "Amount": 500.00,
      "VATRate": 24.00,
      "ftChargeItemCase": 5332816059729461248,
      "Moment": "2025-06-18T08:00:00Z"
    },
    {
      "Position": 2,
      "Quantity": 30,
      "Description": "????????? ?????? 2",
      "Amount": 300.00,
      "VATRate": 24.00,
      "ftChargeItemCase": 5332816059729461248,
      "Moment": "2025-06-18T08:00:00Z"
    }
  ],
  "cbPayItems": [
    {
      "Quantity": 1,
      "Description": "???????",
      "Amount": 800.00,
      "ftPayItemCase": 5332816059729461248,
      "Moment": "2025-06-18T08:00:00Z"
    }
  ],
  "ftReceiptCase": 5332816059729461249,
  "ftReceiptCaseData": {
    "GR": {
      "mydataoverride": {
        "invoice": {
          "invoiceHeader": {
            "dispatchDate": "2025-06-18",
            "dispatchTime": "10:44:19",
            "movePurpose": 1,
            "startShippingBranch": 0,
            "completeShippingBranch": 0,
            "otherDeliveryNoteHeader": {
              "loadingAddress": {
                "street": "???????????? 24",
                "number": "0",
                "postalCode": "56429",
                "city": "??? ???????? - ???????????"
              },
              "deliveryAddress": {
                "street": "??????? 22",
                "number": "0",
                "postalCode": "54622",
                "city": "???????????"
              }
            }
          }
        }
      }
    }
  }
}
```

### Example 3: Partial Override (Only Dispatch Date)

```json
{
  "ftCashBoxID": "11111111-1111-1111-1111-111111111111",
  "ftPosSystemId": "22222222-2222-2222-2222-222222222222",
  "cbTerminalID": "TERMINAL-01",
  "cbReceiptReference": "RECEIPT-2025-100",
  "cbReceiptMoment": "2025-06-18T15:00:00Z",
  "cbChargeItems": [
    {
      "Position": 1,
      "Quantity": 5,
      "Description": "Service Item",
      "Amount": 250.00,
      "VATRate": 24.00,
      "ftChargeItemCase": 5332816059729461248,
      "Moment": "2025-06-18T15:00:00Z"
    }
  ],
  "cbPayItems": [
    {
      "Quantity": 1,
      "Description": "Card Payment",
      "Amount": 250.00,
      "ftPayItemCase": 5332816059729461248,
      "Moment": "2025-06-18T15:00:00Z"
    }
  ],
  "ftReceiptCase": 5332816059729461249,
  "ftReceiptCaseData": {
    "GR": {
      "mydataoverride": {
        "invoice": {
          "invoiceHeader": {
            "dispatchDate": "2025-06-19"
          }
        }
      }
    }
  }
}
```

### Example 4: Multi-Branch Shipping Scenario

```json
{
  "ftCashBoxID": "11111111-1111-1111-1111-111111111111",
  "ftPosSystemId": "22222222-2222-2222-2222-222222222222",
  "cbTerminalID": "TERMINAL-WAREHOUSE",
  "cbReceiptReference": "TRANSFER-2025-055",
  "cbReceiptMoment": "2025-06-18T09:15:00Z",
  "cbChargeItems": [
    {
      "Position": 1,
      "Quantity": 100,
      "Description": "????????? ?????????",
      "Amount": 1000.00,
      "VATRate": 24.00,
      "ftChargeItemCase": 5332816059729461248,
      "Moment": "2025-06-18T09:15:00Z"
    }
  ],
  "cbPayItems": [
    {
      "Quantity": 1,
      "Description": "????????? ????????",
      "Amount": 1000.00,
      "ftPayItemCase": 5332816059729461248,
      "Moment": "2025-06-18T09:15:00Z"
    }
  ],
  "ftReceiptCase": 5332816059729461249,
  "ftReceiptCaseData": {
    "GR": {
      "mydataoverride": {
        "invoice": {
          "invoiceHeader": {
            "dispatchDate": "2025-06-18",
            "dispatchTime": "09:30:00",
            "movePurpose": 2,
            "startShippingBranch": 1,
            "completeShippingBranch": 5,
            "otherDeliveryNoteHeader": {
              "loadingAddress": {
                "street": "???? ???????? 10",
                "number": "10",
                "postalCode": "15123",
                "city": "?????"
              },
              "deliveryAddress": {
                "street": "???? ???????????? 25",
                "number": "25",
                "postalCode": "54622",
                "city": "???????????"
              }
            }
          }
        }
      }
    }
  }
}
```

## C# Implementation Example

```csharp
using fiskaltrust.ifPOS.v2;
using System;
using System.Collections.Generic;

public class MyDataOverrideExample
{
    public ReceiptRequest CreateDeliveryNoteReceipt()
    {
        return new ReceiptRequest
        {
            ftCashBoxID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ftPosSystemId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            cbTerminalID = "TERMINAL-01",
            cbReceiptReference = "DELIVERY-001",
            cbReceiptMoment = DateTime.UtcNow,
            
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Position = 1,
                    Quantity = 10,
                    Description = "Product A",
                    Amount = 100.00m,
                    VATRate = 24.00m,
                    ftChargeItemCase = 0x4752_2000_0000_0000 | 0x0000_0000_0000_0001,
                    Moment = DateTime.UtcNow
                }
            },
            
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash",
                    Amount = 100.00m,
                    ftPayItemCase = 0x4752_2000_0000_0000,
                    Moment = DateTime.UtcNow
                }
            },
            
            ftReceiptCase = 0x4752_2000_0000_0001,
            
            ftReceiptCaseData = new
            {
                GR = new
                {
                    mydataoverride = new
                    {
                        invoice = new
                        {
                            invoiceHeader = new
                            {
                                dispatchDate = "2025-06-18",
                                dispatchTime = "10:44:19",
                                movePurpose = 1,
                                startShippingBranch = 0,
                                completeShippingBranch = 0,
                                otherDeliveryNoteHeader = new
                                {
                                    loadingAddress = new
                                    {
                                        street = "???????????? 24",
                                        number = "0",
                                        postalCode = "56429",
                                        city = "??? ???????? - ???????????"
                                    },
                                    deliveryAddress = new
                                    {
                                        street = "??????? 22",
                                        number = "0",
                                        postalCode = "54622",
                                        city = "???????????"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
```

## Move Purpose Codes

The `movePurpose` field typically corresponds to Greek tax authority codes for goods movement:

| Code | Description (Greek) | Description (English) |
|------|---------------------|----------------------|
| 1 | ?????? | Sale |
| 2 | ?????? ??? ?????????? ?????? | Sale on behalf of third parties |
| 3 | ???????????? | Sampling |
| 4 | ?????? | Exhibition |
| 5 | ????????? | Return |
| 6 | ?????? | Storage |
| 7 | ??????????? | Processing |
| 8 | ????????? ????? | Internal use |
| 9 | ???????? | Transfer |
| 10 | ???? | Other |

**Note**: Always refer to the latest AADE documentation for current move purpose codes.

## Field Validation and Behavior

### Date and Time Format
- **dispatchDate**: Must be in ISO 8601 date format `yyyy-MM-dd` (e.g., "2025-06-18")
- **dispatchTime**: Must be in time format `HH:mm:ss` (e.g., "10:44:19")

### Default Values
- **address.number**: If `null` or not provided, defaults to `"0"`
- All other fields: Remain unset if not provided (no default values applied)

### Null Safety
- All override fields are nullable
- If a parent object is not provided, child fields are ignored
- No errors are thrown for missing override data

### Greek Character Support
- Full UTF-8 support for Greek characters in all text fields
- Address fields support Greek street names and city names
- Examples: "????????????", "???????????", "??? ????????"

## XML Output Example

When overrides are applied, the generated MyData XML will include:

```xml
<invoiceHeader>
    <series>TEST-001</series>
    <aa>12345</aa>
    <issueDate>2025-06-18</issueDate>
    <invoiceType>1.1</invoiceType>
    
    <!-- Override fields -->
    <dispatchDate>2025-06-18</dispatchDate>
    <dispatchTime>10:44:19</dispatchTime>
    <movePurpose>1</movePurpose>
    
    <otherDeliveryNoteHeader>
        <loadingAddress>
            <street>???????????? 24</street>
            <number>0</number>
            <postalCode>56429</postalCode>
            <city>??? ???????? - ???????????</city>
        </loadingAddress>
        <deliveryAddress>
            <street>??????? 22</street>
            <number>0</number>
            <postalCode>54622</postalCode>
            <city>???????????</city>
        </deliveryAddress>
        <startShippingBranch>0</startShippingBranch>
        <completeShippingBranch>0</completeShippingBranch>
    </otherDeliveryNoteHeader>
</invoiceHeader>
```

## Best Practices

1. **Use Partial Overrides**: Only specify the fields you need to override, don't include unnecessary fields
2. **Validate Before Sending**: Ensure date/time formats are correct before sending the receipt
3. **Test in Sandbox**: Always test with sandbox mode first to verify override behavior
4. **Greek Character Encoding**: Ensure your application properly handles UTF-8 encoding for Greek characters
5. **Document Business Logic**: Keep track of when and why certain overrides are used in your business scenarios
6. **Coordinate with AADE Requirements**: Ensure your override usage complies with current Greek tax authority regulations

## Troubleshooting

### Common Issues

**Issue**: Overrides not applied to invoice
- **Solution**: Verify the JSON structure matches the documented format exactly, including property names

**Issue**: Invalid date/time format error
- **Solution**: Ensure dates are in `yyyy-MM-dd` format and times are in `HH:mm:ss` format

**Issue**: Greek characters not displaying correctly
- **Solution**: Ensure your application uses UTF-8 encoding throughout the request pipeline

**Issue**: Address number showing as null in XML
- **Solution**: This is expected behavior - omit the `number` field or set it to `"0"` explicitly

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-XX | Initial release of mydataoverride feature |

## Related Documentation

- [fiskaltrust Interface Documentation](https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc/greece)
- [Greek MyData API Documentation](https://www.aade.gr/mydata)
- [Receipt Cases Documentation](https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc/greece/reference-tables/receipt-case)

## Support

For technical support or questions about the mydataoverride feature:
- Email: support@fiskaltrust.gr
- Documentation: https://docs.fiskaltrust.cloud
- Portal: https://portal.fiskaltrust.gr

---

**Last Updated**: January 2025
**Feature Status**: Production Ready
