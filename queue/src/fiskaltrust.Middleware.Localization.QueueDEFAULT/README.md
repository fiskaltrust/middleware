# fiskaltrust Middleware Localization - Default Queue

## Overview

The Default Queue project within the fiskaltrust Middleware system serves as a template for developing market-specific implementations. This skeleton provides a flexible starting point, encouraging development consistency across different markets.

## Key Components

### Bootstrapping

- `QueueDEFAULTBootstrapper.cs`: Configures services specific to the DEFAULT queue.

### Configuration

- `QueueDEFAULTConfiguration.cs`: Encapsulates the DEFAULT queue configuration.
- `CountrySpecificSettings.cs`: Defines settings specific to the country's regulations.

### Processing

- `SignProcessorDEFAULT.cs`: Processes signature requests for the DEFAULT market.
- `JournalProcessorDEFAULT.cs`: Manages journal requests like retrieving cash box lists.

### Receipt Commands

- `DailyClosingReceiptCommand.cs`, `InitialOperationReceiptCommand.cs`, `MonthlyClosingReceiptCommand.cs`, `OutOfOperationReceiptCommand.cs`, `PosReceiptCommand.cs`, `YearlyClosingReceiptCommand.cs`, `ZeroReceiptCommand.cs`: Define various receipt commands for different operations, aiding in the creation and handling of different types of receipts.

### Extensions

- `ChargeItemExtensions.cs`, `PayItemExtensions.cs`, `ReceiptRequestExtensions.cs`: Provide extension methods for handling charge, pay, and receipt request items.
- `ServiceCollectionExtensions.cs`: Extensions for service collection configuration.

### Factories

- `SignatureItemFactoryDEFAULT.cs`: Creates signature items for the DEFAULT country.
- `RequestCommandFactory.cs`: Manages request command creation.

### Country-Specific Components

- `CountryDefaultQueue.cs`: Dummy queue for country-specific handling.
- `CountrySpecificQueueRepository.cs`: Manages country-specific queues.

### Security Signature Creation Devices (SSCD)

- `SscdXX.cs`: Represents the SSCD component for a specific market.
- `IXXSSCDProvider.cs` & `XXSSCDProvider.cs`: Define the SSCD provider interface and implementation.

### Cases Enumeration

- `Cases.cs`: Enumerates various cases for handling specific functionalities and scenarios within the system.

## Getting Started

1. **Clone the Repository**: Clone the project to your local machine.
2. **Explore the Components**: Familiarize yourself with the components, including classes responsible for specific functionalities.
3. **Read the Official Documentation**: Check the [fiskaltrust documentation](https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc) for comprehensive details.
4. **Customize for Your Market**: Adapt the code to fit your target market's requirements.
5. **Run Tests**: Validate your adaptations with appropriate testing.

## Contributing

Contributions to enhance the Default Queue project are welcome. Follow the contributing guidelines and ensure alignment with the project's architecture and goals.


## Support

For support, questions, or collaboration, please contact the [fiskaltrust team](https://github.com/orgs/fiskaltrust/discussions).

