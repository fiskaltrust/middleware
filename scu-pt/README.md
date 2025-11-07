# fiskaltrust Middleware SCU Portugal

This project contains the Signature Creation Unit (SCU) implementation for Portugal.

## Structure

- **src/fiskaltrust.Middleware.SCU.PT.Abstraction**: Contains the abstract types, constants, and interfaces used by the Portugal SCU implementations
  - `SignatureTypePT`: Defines signature types specific to Portugal
  - `ChargeItemCaseNatureOfVatPT`: Defines VAT nature cases for Portugal
  - `PTConstants`: Portugal-specific constants
  - `PTInvoiceElement`: Model for invoice elements in Portugal

- **src/fiskaltrust.Middleware.SCU.PT.InMemory**: Contains the in-memory SCU implementation for Portugal
  - `InMemorySCU`: In-memory implementation of the IPTSSCD interface

- **test/fiskaltrust.Middleware.SCU.PT.UnitTest**: Contains unit tests for the SCU implementations

## Building

To build the solution:

```powershell
dotnet build fiskaltrust.Middleware.SCU.PT.sln
```

## Testing

To run the tests:

```powershell
dotnet test fiskaltrust.Middleware.SCU.PT.sln
```

## Integration with QueuePT

The QueuePT project references this SCU project to provide signature creation functionality for Portuguese receipts. The SCU abstraction layer allows for future implementations (e.g., hardware-based SCUs) to be added without changing the QueuePT code.
