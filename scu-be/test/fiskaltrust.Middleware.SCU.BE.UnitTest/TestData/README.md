# ZwarteDoos SCU Test Data

This directory contains test data for the Belgium ZwarteDoos SCU implementation.

## Sample Test Data Files

### invoice-samples.json
Contains sample invoice data for testing various scenarios:
- Standard invoices with different VAT rates
- Multi-line invoices
- Edge cases (zero amounts, high precision decimals)

### configuration-samples.json
Contains sample configuration data for different test environments:
- Sandbox configuration
- Production-like configuration
- Error scenarios

## Usage

These files are automatically copied to the test output directory and can be used in unit tests for:
- Loading realistic test data
- Testing serialization/deserialization
- Validating business logic with various invoice structures

## Belgium-Specific Considerations

The test data reflects Belgian fiscal requirements:
- VAT rates (21%, 12%, 6%, 0%)
- Belgian company identification formats
- Currency formatting (EUR)
- Date/time formatting requirements