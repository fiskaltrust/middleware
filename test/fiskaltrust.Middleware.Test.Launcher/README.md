This solution contains the test launcher for the v2 localizations (ES, IT, PL). The launcher for the older
localizations was retired together with the last v1 market it served (IT); the v1 markets (AT, DE, FR, ME) are
started with `queue/test/Manual/fiskaltrust.Middleware.Queue.Test.Launcher`.

It's meant as a way to quickly test and debug the localizations and scus.

The test launcher starts an in memory queue in a certain market that's connected to a specific scu-type.

## Goals

* Debugging a certain business case with it should be as easy as selecting the business case.
* Where possible the test launcher should create the whole cashbox configuration on the fly so the whole project is "plug and play".
* Everything should work in memory/local only where possible.

## State

