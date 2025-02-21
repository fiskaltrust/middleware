fiskaltrust.Middleware Interface
=========================

[![Nuget](https://img.shields.io/nuget/v/fiskaltrust.interface?label=NuGet.org)](https://nuget.org/packages/fiskaltrust.interface)
[![Build Status](https://fiskaltrust.visualstudio.com/department-develop-research/_apis/build/status/fiskaltrust.if/fiskaltrust.middleware-interface-dotnet?branchName=master)](https://fiskaltrust.visualstudio.com/department-develop-research/_build/latest?definitionId=334)

A NuGet package that can be used to include the free fiskaltrust Middleware into POS systems.

## Overview
The fiskaltrust Middleware is a software that can be used in POS systems to fullfil legal requirements with minimal impact and development effort. It works both on Windows and Linux, and can be integrated via gRPC, WCF or REST. Simply put, the fiskaltrust.Middleware publishes the `IPOS` interface via one (or multiple) of these protocols, and the POS system can use that endpoint to sign receipts and request exports (= journals).

This repository contains the .NET interface as a NuGet package; due to the open nature of the used protocols, other programming languages are supported too.

## Getting Started
To implement the Middleware into your POS system, please include the latest version of the NuGet package and take a look at the [interface documentation](https://github.com/fiskaltrust/interface-doc).

Additionally, please have a look at the [demo repository](https://github.com/fiskaltrust/demo/), which contains minimal sample applications for a broad variety of programming languages. Some usage examples can also be taken from the [tests](test/fiskaltrust.ifPOS.Tests/v1/IPOS/Wcf).

## Clients
For even simpler usage, we offer client packages for the supported communication protocols. Just follow the link in the Badge to install the respective package, and use the snippets above to create an instance of IPOS.

### gRPC
[![Nuget](https://img.shields.io/nuget/v/fiskaltrust.Middleware.Interface.Client.Grpc?label=nuget)](https://nuget.org/packages/fiskaltrust.Middleware.Interface.Client.Grpc)

```cs
var pos = await GrpcPosFactory.CreatePosAsync(new GrpcClientOptions 
{ 
    Url = new Uri(url), 
    RetryPolicyOptions = new RetryPolicyOptions { ... } // Optional
});
```

### HTTP/REST
[![Nuget](https://img.shields.io/nuget/v/fiskaltrust.Middleware.Interface.Client.Http?label=nuget)](https://nuget.org/packages/fiskaltrust.Middleware.Interface.Client.Http)

```cs
var pos = await HttpPosFactory.CreatePosAsync(new HttpPosClientOptions
{ 
    Url = new Uri(url), 
    CommunicationType = HttpCommunicationType.Json,     // Or HttpCommunicationType.Xml
    CashboxId = cashboxId,
    AccessToken = "<ACCESS_TOKEN>"                      // Only required for SignaturCloud
    UseUnversionedLegacyUrls = false                    // Optional. Set `true` for fiskaltrust.Middleware < 1.3
    RetryPolicyOptions = new RetryPolicyOptions { ... } // Optional
});
```

### fiskaltrust.Middleware < 1.3

Versions before v1.3 of the fiskaltrust.Middleware only support the `fiskaltrust.ifPOS.v0` and need to use unversioned legacy urls.

```cs
ifPOS.v0.IPOS pos = await HttpPosFactory.CreatePosAsync(new HttpPosClientOptions
{ 
    Url = new Uri(url), 
    CommunicationType = HttpCommunicationType.Json,
    CashboxId = cashboxId,
    AccessToken = "<ACCESS_TOKEN>"
    UseUnversionedLegacyUrls = true                     // Needs to be true
    RetryPolicyOptions = new RetryPolicyOptions { ... }
});
```

### SOAP/WCF
[![Nuget](https://img.shields.io/nuget/v/fiskaltrust.Middleware.Interface.Client.Soap?label=nuget)](https://nuget.org/packages/fiskaltrust.Middleware.Interface.Client.Soap)

```cs
var pos = await SoapPosFactory.CreatePosAsync(new ClientOptions
{ 
    Url = new Uri(url), 
    RetryPolicyOptions = new RetryPolicyOptions { ... } // Optional
});
```

## Contributions
If you want to contribute to this repository, please review this README file to understand how it is structured and which tools are used.

## Versioning
Currently, the _Minor_ version is incremented for each country that is added to the interface. Starting with the future version 2.0, we will switch to [semantic versioning](https://semver.org/).

For the list of currently available versions, please have a look at the [NuGet Version History](https://www.nuget.org/packages/fiskaltrust.interface/).

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
