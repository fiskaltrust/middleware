This solution contains two test launchers one for the localization v2 and one for the older localizations.

It's meant as a way to quickly test and debug the localizations and scus.

Those test launchers start an in memory queue in a certain market that's connected to a specific scu-type.

## Goals

* Debugging a certain business case with it should be as easy as selecting the business case.
* Where possible the test launcher should create the whole cashbox configuration on the fly so the whole project is "plug and play".
* Everything should work in memory/local only where possible.

## State

