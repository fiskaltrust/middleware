# fiskaltrust Middleware Localization - Default Queue

## Overview

The Default Queue project within the fiskaltrust Middleware system serves as a template for developing market-specific implementations.
This skeleton provides a flexible starting point, encouraging development consistency across different markets.

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

### Secure Signature Creation Devices (SSCD)

- `SscdXX.cs`: Represents the SSCD component for a specific market.
- `IXXSSCDProvider.cs` & `XXSSCDProvider.cs`: Define the SSCD provider interface and implementation.

### Cases Enumeration

- `Cases.cs`: Enumerates various cases for handling specific functionalities and scenarios within the system.

## Naming Convention

In this project, you may notice the use of "XX" and "DEFAULT" in various class and file names.

When using this as the basis for a new market _both_ "XX" and "DEFAULT" should be replaced with the market country code (e.g. "IT" for Italy).

The "DEFAULT" files include a working minimum implementation that is used in the DEFAULT Queue. This implementation needs to be adapted to the new market.

The "XX" files serve as a placeholders representing a feature that might be needed in a real market but not in the DEFAULT queue.

For example:
- `SscdXX.cs` would be renamed to `SscdIT.cs` for the Italian market.
- `QueueDEFAULTConfiguration.cs` might be tailored to represent configurations specific to your target market.

This convention helps maintain a clear and consistent structure, facilitating customization for different markets.

## Getting Started implementing a new market

1. **Clone the Repository**: Clone the project to your local machine to begin working with the code.
2. **Understand the Basics**: The Default Queue project is a foundational template designed to be customized for various markets. Familiarize yourself with the goals and structure of the project by reading the [fiskaltrust documentation](https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc).
3. **Explore the Components**: Dive into the code to explore the components, classes, and functionalities. Understanding how they work together will help you customize the project for your specific needs.
4. **Update fiskaltrust.interface**: Update the fiskaltrust.interface project to add your markets SSCD interface (See [below](#update-fiskaltrust.storage)).
5. **Create an SCU**: Create a new SCU project for your market. The SCU is the component that communicates with the SSCD.
6. **Update fiskaltrust.storage**: Update the fiskaltrust.storage project to add your market's specific data base needs (See [below](#update-fiskaltrust.interface)).
7. **Adapt for Your Market**: This template is meant to be adapted. Modify the code to align with the regulations and practices of your target market.
8. **Test Your Changes**: Ensure that your adaptations work as intended by running appropriate tests. Following established testing practices ensures that your customizations maintain the overall integrity of the system.

> _*Note:* Steps 4-7 can be completed in any order and will probably need to be worked on in parallel._
> _However, it is recommended to start them in the order listed above._

> _*Note:* As this project is a template, significant customization may be required._
> _Collaborate with stakeholders and refer to local regulations to ensure that the final implementation meets the needs of your specific market._

## Update fiskaltrust.interface

The [fiskaltrust.interface](https://github.com/fiskaltrust/middleware-interface-dotnet) repository contains the Queue interface (`IPOS`) and the interfaces for the SSCDs (`IDESSCED`, `IITSSCD` etc.).
To implement a new market you will need to add a new SSCD interface to this repository which is used by the Queue implementation to communicate with the SCU.

This `IXXSSCD` interface should not be designed for a specific SSCD but rather for a specific market as the interface should work for all SSCDs that are available for this market.
Try to model the interface as close to the laws and regulations of the market as possible to achieve this.

## Update fiskaltrust.storage

The [fiskaltrust.storage](https://dev.azure.com/fiskaltrust/department-develop-research/_git/fiskaltrust.storage) is a project that contains the database models and the for the middleware and the fiskaltrust.Portal.
It needs to be updated to include the Queue and SCU tables for your market.

The tables `ftQueueXX`, `ftJournalXX` and `ftSignaturCreationUnitXX` need to be created.
Try to add all fields that might be needed for your market to these tables.

Things to take into consideration are the state of the Queue and SCU for the `ftQueueXX` and `ftSignaturCreationUnitXX` tables that might be needed for processing a `ReceiptRequest` or `JournalRequest`.

The `ftJournalXX` table should contain all fields from the `ReceiptResponse` that are relevant for later `ReceiptRequest`s. 
This might include the `cbReceiptResponse` for references to older receipts etc.
The `ftJournalXX` table should contain fields that will be helpful for performant `JournalRequest`s.

Everything not included in the `ftJournalXX` table that is needed will need to be parsed from the JSON `ReceiptResponse` stored in the `ftQueueItem` table which potentially incurs high performance costs.

On the other hand storing huge amounts of data will lead to rapid database growth which will also lead to problems later on.
This balance is something that needs to be considered when designing the storage.

## Contributing

Contributions to enhance the Default Queue project are welcome. Follow the contributing guidelines and ensure alignment with the project's architecture and goals.


## Support

For support, questions, or collaboration, please contact the [fiskaltrust team](https://github.com/orgs/fiskaltrust/discussions).

