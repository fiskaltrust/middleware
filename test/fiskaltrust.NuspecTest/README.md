## NuspecTest project

this project resembles the AndriodLauncher Project

## Background

When we build the nuget package for sqlite, the .nuspec file needs to configured correctly.

When adding a new package and it is forgotten to update the .nuspec file the released package can not be used in a PackageReference which is needed for the Android Launcher and SignatureCloud.DE.

## solution

Create a smoketest thats running in CI which publishes and packs the Queue.SQLite to a local nuget directory and then tries to use this package from local to the project that resembales AndriodLauncher Project.

When this test fails, it means the nuspec file in fiskaltrust.Middleware.Queue.SQLite project is not configured properly.

## Steps

- Added a new project (fiskaltrust.Middleware.Queue.Sqlite.NuspecTest.csproj) that resembles the Android launcher project
- Added a template named test-nuspec.template.yaml run on the fiskaltrust.Middleware.Queue pipeline

  - publishes and packs the SQLite queue to a local feed. the localfeed contains a SQLite.1.3.0-local package.`This pack is created with a nuspec configuration file in fiskaltrust.Middleware.Queue.SQLite project and the version 1.3.0-local comes from there which must not be changed because its fixed in NuspecTest project`

  - Added pipeline tasks to build NuspecTest project and force the project to use the Queue.Sqlite package from local feed by using NuGet.config.
    Nuget.config have to contain localfeed

    ` <add key="localfeed" value=".\localfeed" />`
