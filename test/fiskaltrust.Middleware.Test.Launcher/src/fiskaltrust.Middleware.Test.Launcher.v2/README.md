# Test.Launcher.v2

Starts an in-memory middleware for a v2 localization, wired to an SCU, without a cashbox on the
portal and without a database. Two ways to drive it:

* **scripted** (default) — `Program.cs` runs a fixed sequence of receipts and asserts the responses.
* **HTTP host** — set `MW_HOST_URL` and the same middleware is served over HTTP, so requests can
  come from Postman, curl or a POS.

## Run it

Everything is chosen through environment variables; the profiles in `Properties/launchSettings.json`
cover the usual combinations for F5.

| Variable | Default | Meaning |
| --- | --- | --- |
| `MW_MARKET` | `PL` | `ES` or `PL` |
| `MW_QUEUE_CONFIGURATION` | the cashbox configuration, else `queue-configuration[-pl].json` | queue package configuration file, overriding it |
| `MW_SCU_CONFIGURATION` | the cashbox configuration, else `scu-configuration-{bizkaia,pl-inmemory}.json` | SCU package configuration file, overriding it |
| `MW_HOST_URL` | — | set to serve over HTTP instead of running the scripted sequence |
| `MW_SKIP_ACTIVATION` | — | `1` leaves the queue inactive at startup (host mode only) |
| `MW_PL_ASSUME_FISCALIZED` | — | `1` reports the register as fiscalized so a non-fiscal test printer can be driven end to end (PL only) |
| `MW_CASHBOX_CONFIGURATION` | `configuration/<MARKET>/cashbox-configuration.json` | the cashbox configuration to run |

### Poland

```sh
# in-memory SCU, HTTP host
MW_MARKET=PL MW_HOST_URL=http://localhost:1500 dotnet run

# against the PosNet printer, from its own cashbox configuration
MW_MARKET=PL MW_CASHBOX_CONFIGURATION=configuration/PL/cashbox-configuration-posnet.json MW_HOST_URL=http://localhost:1500 dotnet run

# no host, just the scripted run
MW_MARKET=PL dotnet run
```

A Polish queue only activates against a register that reports itself as fiscalized — that is a
`serwis` act, not something the middleware can do. The in-memory SCU reports fiscalized, so it
activates; a non-fiscal test printer does not, and the host will say so in its index under
`Activation` and keep running so the failure can be inspected.

#### Making the test printer print

An inactive queue never forwards a receipt to the SCU, so a non-fiscal printer stays silent — not
because it could not print, but because nothing reaches it. `MW_PL_ASSUME_FISCALIZED=1` reports the
register as fiscalized (`AssumeFiscalizedPLSSCD`, launcher-only) so the queue activates and every
sale receipt is really printed, as a non-fiscal document (`NIEFISKALNY`):

```sh
# scripted run: init, zero receipt, two cash sales — two printouts
MW_MARKET=PL MW_CASHBOX_CONFIGURATION=configuration/PL/cashbox-configuration-posnet.json MW_PL_ASSUME_FISCALIZED=1 dotnet run

# HTTP host: POST a sample whenever you want a printout
MW_MARKET=PL MW_CASHBOX_CONFIGURATION=configuration/PL/cashbox-configuration-posnet.json MW_PL_ASSUME_FISCALIZED=1 MW_HOST_URL=http://localhost:1500 dotnet run
curl -X POST localhost:1500/samples/SignRequestReceipt_CashSaleReceipt
```

Which cases reach the paper is up to the SCU: the sale cases (`CashSaleReceipt`, `CardSaleReceipt`,
`NipReceipt`) print, the zero receipt only reads the status, and reports and returns are not
implemented in the PosNet SCU yet.

### Spain

```sh
MW_MARKET=ES dotnet run
```

## Endpoints

The host is a debug host. Its routes are named after the operations rather than following the
production launcher's REST contract.

| Route | |
| --- | --- |
| `GET /` | index: market, packages, ids, activation result, routes, samples |
| `POST /echo` | `EchoRequest` |
| `POST /sign` | `ReceiptRequest` |
| `POST /activate` | re-sends the initial-operation receipt |
| `POST /journal` | `JournalRequest` |
| `GET /journal/{type}` | `ActionJournal`, `ReceiptJournal`, `QueueItem`, `Configuration` |
| `GET /samples` | the committed business cases |
| `GET /samples/{name}` | the request that would be sent, placeholders resolved |
| `POST /samples/{name}` | signs it |

```sh
curl localhost:1500/
curl -X POST localhost:1500/samples/SignRequestReceipt_CashSaleReceipt
curl localhost:1500/journal/ActionJournal
```

There is no `/v2` prefix — the index at `GET /` lists the routes the running host actually has.

### A 200 does not mean the receipt was signed

Every request has to be signed, but a receipt the queue stored *without* signing is not a failed
request — the queue accepted and persisted it. So the signing routes answer `200` either way and the
outcome stays in `ftState`, where the middleware puts it; `400`/`500` remain reserved for a malformed
request or an unhandled exception. Read the body, not the status code.

The reason a receipt went unsigned is not in the response at all — the queue writes it to the action
journal. The host therefore prints it, so it is not lost between a `200` and a printer that stayed
quiet:

```
!! The receipt was stored, but not signed: the security mechanism is deactivated — the queue is not
   activated or out of operation, so the receipt never reached the SCU (ftState 0x504C200000000001).
   QueueId 678f5979-… has not been activated yet.
```

A failed startup activation says so on the console too, not only in the index. The journal is also
readable per request: `curl localhost:1500/journal/ActionJournal`.

The samples live in `json-requests/<MARKET>/<business case>/` and are read per request, so editing
one and posting it again does not need a restart. Their `cbReceiptReference` values are left as
written — the return receipt points at the cash sale, which only works with stable references. Post
your own body to `/sign` when you need a fresh reference.

Everything is in memory: a restart means a new cashbox, a new queue and a queue that has to be
activated again.

### The ids are in the configuration

`configuration/<MARKET>/cashbox-configuration.json` is the cashbox: `ftCashBoxId`, the queue package
under `ftQueues` with its `init_` tables, and the local SCU under `ftSignaturCreationDevices`. It is
the shape the portal issues and the production launcher consumes, read with the same
`JsonConvert.DeserializeObject<ftCashBoxConfiguration>` call — so what is exercised here is the
configuration path that runs in the field, not a launcher-specific one.

The ids are therefore not resolved from anywhere: they are pinned by standing in the file, and they
survive every restart. `configuration/PL/` ships two, both on the same cashbox id — the default one
on the in-memory SCU, and `cashbox-configuration-posnet.json` on the test printer.

Because the file also carries the `init_` tables, the launcher synthesizes nothing. `GET /` reports
which is which, so a configured cashbox is distinguishable from a filled-in one:

```json
"Tables": {
  "ConfiguredTables": ["init_ftCashBox", "init_ftQueue", "init_ftQueuePL", "init_ftSignaturCreationUnitPL"],
  "SynthesizedTables": []
}
```

Leave a table out and the launcher fills that one in; leave `ftCashBoxId` at all-zero, or run a
market that commits no configuration, and it invents ids per start as it always did.

`ftPosSystemId` is the one property in these files that a real cashbox configuration does not have —
a pos system belongs to the caller, not to the cashbox. `ftCashBoxConfiguration` has no property for
it and ignores it, so it is read off the same text and nothing else about the format changes: a
portal export simply has none, and the pos system is then generated per start. It reaches the samples
through their `{{ ftPosSystemID }}` placeholder.

A file named through `MW_CASHBOX_CONFIGURATION` and then not found stops the start. The default one
being absent does not, and that is what keeps the other markets working: without a cashbox
configuration the launcher reads the per-package files from the project folder under the names it has
always used — `queue-configuration.json` and `scu-configuration-bizkaia.json`, or the `-pl` ones for
Poland. None of them is committed (see `.gitignore`), so those files are whatever a market's own
developer put there, which is why the names and the folder must not move.

In short: PL runs from `configuration/PL/`, every other market runs exactly as it did before that
folder existed.

This pins identity, not state: storage stays in memory, so the queue still comes up new and needs
activating again on every start.

## Specify a configuration

`configuration/<MARKET>/queue-configuration.json` and `configuration/<MARKET>/scu-configuration.json`
hold a single package configuration each, as it would arrive inside a cashbox configuration. They are
what `MW_QUEUE_CONFIGURATION` and `MW_SCU_CONFIGURATION` point at by default, and the only source for
a market that commits no cashbox configuration.

The queue file needs at least an empty configuration:

```json
{
  "Configuration": {}
}
```

The SCU file names the package and its parameters, e.g.:

```json
{
  "Package": "fiskaltrust.Middleware.SCU.ES.TicketBaiAraba",
  "Configuration": {
    "CertificateBase64": "",
    "CertificatePassword": "",
    "EmisorNif": "",
    "EmisorApellidosNombreRazonSocial": ""
  }
}
```
