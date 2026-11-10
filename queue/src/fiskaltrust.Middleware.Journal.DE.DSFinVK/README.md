# fiskaltrust.Middleware.Journal.DE.DSFinVK
[![Build Status](https://fiskaltrust.visualstudio.com/fiskaltrust/_build?definitionId=452) [![Build Status]

The fiskaltrust.Middleware.Journal.DE.DSFinVK is a fiscalization export required by German laws. It visualizes sales and splits up vat rates and business cases.

## Introduction
The German fiscalization export is very spezific. With providing this feature to our customers we enable them throughout their fiscalization process.

### Features
- The export can be called directly from the middleware by Journalcall or in the portal with uploaded data.
- The timerange can be defined when calling the export.

### Getting started
To starte the export:

- Middleware: send a request: {{base_url}}/json/v0/Journal?type=4919338167972134914
- Portal: go to https://portal-sandbox.fiskaltrust.de/Queue#/ select export - Dsfinvk  

## Architecture and repository structure
The DSFinVK export lives in the middleware repository and is consumed by the middleware (QueueDE journal) and by [service-datapipes](https://github.com/fiskaltrust/service-datapipes) via a git submodule.

It can be found under `queue/src`.
[fiskaltrust.Middleware.Journal.DE.DSFinVK](https://github.com/fiskaltrust/middleware/tree/main/queue/src/fiskaltrust.Middleware.Journal.DE.DSFinVK).

## Documentation & release notes
Documentation and release notes are maintained on GitHub (look for the `-doc` postfix in our repositories), and available on [docs.fiskaltrust.cloud](https://docs.fiskaltrust.cloud).

## Debugging using the Test Launcher

There are 3 way to test and run DSFinVK-Export

1. Using [integration test](https://github.com/fiskaltrust/middleware/tree/main/queue/test/fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest) to test with bin files. To get the binfiles:
- For Sandbox accounts: When receipts have been uploaded by the middleware (Helipad-Helper), which happens automatically when running the middleware all 5 minutes, the export can be started in the portal https://portal-sandbox.fiskaltrust.de/Queue#/. One can acces the bin files via 'Microsoft Azure Storeage Explorer':
	1. Select ft-msft-mca-development-common 
	2. Select fta24datapipesstorage
	3. Select Blob Containers
	4. Select activity-buffer
	5. enter queueid
	6. find export by datetime :(
Last successfull build (all branches) is available on Sandbox to test!
- For Portal Production extra permission needed to access bin files: ask your teamlead for the bin files for queueid

2. Test updated Export in Middleware with release Export to dev (nuget-feed)
	2.1 release your build [release - dsfinvk] (https://fiskaltrust.visualstudio.com/department-develop-research/_release?_a=releases&view=mine&definitionId=150)
    2.2 select Create Release
	2.3 Select your build
	2.4 Release dev 
	2.5 Available in dev -nuget-feed in mw https://pkgs.dev.azure.com/fiskaltrust/_packaging/dev/nuget/v3/index.json

3. Add Export as project to the middleware
   3.1 remove all references to the export
   3.2 delete bin an obj files in mw folder
   3.3 add projects dsfinvk export and export common to mw solution
   3.4 reference the added references to the projects needing dsfinvk export.


## License
The DSFinVK-Export is in private repository and owned by fiskaltrust. 
