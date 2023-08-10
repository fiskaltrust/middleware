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

## Naming Convention

In this project, you may notice the use of "XX" in various class and file names. This "XX" serves as a placeholder representing a specific market or country code. When adapting the code for a particular market, replace "XX" with the appropriate market identifier.

For example:
- `SscdXX.cs` would be renamed to `SscdIT.cs` for the Italian market.
- `QueueDEFAULTConfiguration.cs` might be tailored to represent configurations specific to your target market.

This convention helps maintain a clear and consistent structure, facilitating customization for different markets.

## Getting Started

1. **Clone the Repository**: Clone the project to your local machine to begin working with the code.
2. **Understand the Basics**: The Default Queue project is a foundational template designed to be customized for various markets. Familiarize yourself with the goals and structure of the project by reading the [fiskaltrust documentation](https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc).
3. **Explore the Components**: Dive into the code to explore the components, classes, and functionalities. Understanding how they work together will help you customize the project for your specific needs.
4. **Adapt for Your Market**: This template is meant to be adapted. Modify the code to align with the regulations and practices of your target market.
5. **Test Your Changes**: Ensure that your adaptations work as intended by running appropriate tests. Following established testing practices ensures that your customizations maintain the overall integrity of the system.

> Note: As this project is a template, significant customization may be required. Collaborate with stakeholders and refer to local regulations to ensure that the final implementation meets the needs of your specific market.


## Contributing

Contributions to enhance the Default Queue project are welcome. Follow the contributing guidelines and ensure alignment with the project's architecture and goals.


## Support

For support, questions, or collaboration, please contact the [fiskaltrust team](https://github.com/orgs/fiskaltrust/discussions).

