# Market PL — Poland

Fiscalization for Poland: a POS receipt is printed on a **POSNET Online fiscal register**, and the
register — not the middleware — is the fiscal device. It owns the fiscal document numbers, the PTU
(VAT) table, the daily report and the fiscal memory. The middleware's job is to translate a
`ReceiptRequest` into the register's protocol, to read back what the register recorded, and to refuse
what the register would reject.

Without a working register no sale may legally be recorded (Art. 111(3) VAT Act), so a register that
cannot be reached is an error state, never a silent skip.

This is the entry point for the whole Polish vertical: storage → `QueuePL` → `SCU.PL` → POSNET.

## The slice, top to bottom

| Layer | Where | What it does |
| --- | --- | --- |
| Storage | [`ftQueuePL`](../storage/src/fiskaltrust.storage/Models/ftQueuePL.cs), [`ftSignaturCreationUnitPL`](../storage/src/fiskaltrust.storage/Models/ftSignaturCreationUnitPL.cs) | The two PL configuration tables, alongside every other market's |
| Queue | [`Localization.QueuePL`](../queue/src/fiskaltrust.Middleware.Localization.QueuePL) | v2 localization: receipt/daily-operations/lifecycle/protocol processors, PLN validation, device-unreachable state |
| SCU contract | [`SCU.PL.Abstraction`](src/fiskaltrust.Middleware.SCU.PL.Abstraction) | `IPLSSCD` helpers, PL signature/state cases, PTU slot resolution, the shared reading of the receipt cases |
| SCU (no device) | [`SCU.PL.InMemory`](src/fiskaltrust.Middleware.SCU.PL.InMemory) | Counts fiscal documents and Z reports without hardware — for development and CI |
| SCU (device) | [`SCU.PL.PosNet`](src/fiskaltrust.Middleware.SCU.PL.PosNet) | The POSNET protocol: framing, transport (TCP and USB/COM), receipt mapping, read-back |

### QueuePL

* **Receipts** (`0x0000`, `0x0001`, `0x0004`) go to the SCU. `PaymentTransfer` and
  `PointOfSaleReceiptWithoutObligation` are no-ops; delivery note, table check and pro forma are
  refused. See [`ReceiptCommandProcessorPL`](../queue/src/fiskaltrust.Middleware.Localization.QueuePL/Processors/ReceiptCommandProcessorPL.cs).
* **Invoices** (`0x1xxx`) are deliberately *not* sent to the register — a Polish invoice is
  fiscalized through KSeF. They are persisted and signed "stored, not fiscalized", so introducing an
  `SCU.PL.KSeF` later is a configuration change, not a breaking one. See
  [`InvoiceCommandProcessorPL`](../queue/src/fiskaltrust.Middleware.Localization.QueuePL/Processors/InvoiceCommandProcessorPL.cs).
* **Currency** is PLN, on the receipt and on every charge and pay item (rfcs/0705-queue-single-currency).
  The data format defaults to EUR, so a PosCreator has to set it — hence the explicit validation in
  [`ReceiptValidatorPL`](../queue/src/fiskaltrust.Middleware.Localization.QueuePL/Validation/ReceiptValidatorPL.cs).
* **An unreachable register** becomes `StatePL.DeviceUnreachableError`. Detection is structural
  (network exception types) plus name-based for the SCU packages' `PLDeviceUnreachableException`,
  walking the type hierarchy — QueuePL does not reference the SCU assemblies. See
  [`PLSSCDErrorHandling`](../queue/src/fiskaltrust.Middleware.Localization.QueuePL/Processors/PLSSCDErrorHandling.cs).

### The POSNET SCU

The register is spoken to over its own framed protocol ([`Protocol/`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Protocol)),
either over the network interface or over the USB/COM port ([`Transport/`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Transport)).
Two properties of that protocol shape most of the code:

* **The register validates the arithmetic.** Unit price × quantity must equal the line value, on a
  sale line and on a reversal line alike (errors 2851/2852); a subtotal discount must add up. So the
  mapper computes in grosze and refuses a receipt the register would reject, with a sentence naming
  what the POS should send instead — rather than letting the printer answer with a number.
* **An answer only identifies itself by mnemonic.** There is no sequence number, so a late answer to
  a command that timed out can be mistaken for the next command's confirmation, shifting every answer
  that follows. The client therefore checks that the mnemonic matches, and reports a mismatch as an
  *ambiguous outcome* — the command was written, and what the register did with it is exactly what
  the answer does not say.

Notable mapping rules, all covered by unit tests:

* **Storno** (`ChargeItemCase` void) reverses a quantity of a named or preceding sale position. The
  quantity follows from the amount and the unit price the position was printed with, because a
  `Quantity` the POS sends cannot be told apart from the receipt model's default of 1.
* **Rabat/narzut** attaches to the position it names in `Position`, else to the position in front of
  it, else to the subtotal. It is refused when the position in front of it has just been reversed —
  a storno carries no discount of its own.
* **PTU slots** are resolved against the table the *register* reports (`sfsk`), not against a
  configured one, unless `VatRateTable` pins it deliberately (e.g. for a recording).
* **The daily report** is dated from the receipt moment in `Europe/Warsaw`. A register stands in
  Poland by law, and a middleware host in UTC would otherwise close the wrong day around midnight.

#### Configuration

The SCU takes its parameters from the cashbox configuration; see
[`PosNetConfiguration`](src/fiskaltrust.Middleware.SCU.PL.PosNet/PosNetConfiguration.cs) for the
full set and the defaults.

| Parameter | Meaning |
| --- | --- |
| `DeviceUrl` | `tcp://192.168.1.50:6666` (or `host:port`), or `serial://COM9` / `usb://COM9` / `/dev/ttyACM0` |
| `ConnectTimeoutMs`, `SendTimeoutMs`, `ReceiveTimeoutMs` | Must be positive; a command whose answer is never waited for would be reported as ambiguous on a register that answered |
| `SerialBaudRate`, `SerialParity`, `SerialStopBits`, `SerialHandshake` | Serial line settings; the defaults are the device's own (115200 8N1, no flow control, which is what a USB virtual COM port needs) |
| `VatRateTable` | Empty by default — the register's own table is used |

Everything arrives from the Portal as strings, so numbers are read from strings and a cleared field
reads as the device default. A parameter of the wrong shape is reported as a PL validation error
naming the setting, not as a raw `JsonException`.

## Tests

294 tests, all runnable without hardware.

| Suite | Count | Needs |
| --- | --- | --- |
| [`SCU.PL.UnitTest`](test/fiskaltrust.Middleware.SCU.PL.UnitTest) | 204 | nothing |
| [`SCU.PL.AcceptanceTest`](test/fiskaltrust.Middleware.SCU.PL.AcceptanceTest) | 39 | the in-process printer emulator |
| [`SCU.PL.EndToEnd.AcceptanceTest`](test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest) | 14 | recorded cassettes, or a real register when `SCU_PL_POSNET_DEVICE_URL` is set |
| [`QueuePL.UnitTest`](../queue/test/fiskaltrust.Middleware.Localization.QueuePL.UnitTest) | 25 | nothing |
| [`QueuePL.AcceptanceTest`](../queue/test/fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest) | 12 | nothing |

```powershell
# the three SCU suites (257 tests)
dotnet test scu-pl/fiskaltrust.Middleware.SCU.PL.sln

# the two QueuePL suites (37 tests) — they live in the queue solution
dotnet test queue/test/fiskaltrust.Middleware.Localization.QueuePL.UnitTest
dotnet test queue/test/fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest
```

The end-to-end suite is the one worth reading first: it sends a committed JSON `ReceiptRequest`
through `QueuePL` into the PosNet SCU, and then holds the response, the commands that reached the
register and the register's own counters against what the request implies. Its
[README](test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest/README.md) documents the cassette
mechanism, how to point it at a real printer, and the two measurement gaps that remain (multi-rate
subtotal-discount rounding, and whether a non-fiscal register totalizes its `NIEFISKALNY` printouts).

CI: [`scu-pl-build.yml`](../.github/workflows/scu-pl-build.yml) builds the solution and runs the unit
and acceptance suites on `windows-latest`, and is triggered by changes to `scu-pl/` as well as to the
queue and storage projects the end-to-end suite drives.

## Running it by hand

[`Test.Launcher.v2`](../test/fiskaltrust.Middleware.Test.Launcher/src/fiskaltrust.Middleware.Test.Launcher.v2)
hosts the middleware from a cashbox configuration — in-memory SCU or a real printer — and serves the
end-to-end suite's business cases over HTTP, so a printout is one POST away. Its
[README](../test/fiskaltrust.Middleware.Test.Launcher/src/fiskaltrust.Middleware.Test.Launcher.v2/README.md#poland)
has the Polish recipes. The v1 launcher cannot host `QueuePL` (net461 vs. net8.0) and says so.

---

# Review guide

## Scope: PL only

Verified against `main`: every change is either a new PL file or a PL entry added alongside the
existing markets. The shared files that were touched, and why:

| File | Change |
| --- | --- |
| `storage/.../IConfigurationRepository.cs`, `IReadOnlyConfigurationRepository.cs` | Six PL methods added — see the note below |
| `queue/.../StorageBaseInitConfiguration.cs`, `BaseStorageBootStrapper.cs` | `QueuesPL` / `SignaturCreationUnitsPL` alongside AT, BE, DE, ES, FR, GR, IT, ME |
| `queue/.../Localization.v2/JournalProcessor.cs` | Two PL lists added to the configuration journal |
| `queue/.../Storage.{InMemory,AzureTableStorage}` | PL repositories, table entities, `Migration_008_PL` |
| `queue/.../Storage.{EF,MySQL,SQLite}` | The new interface methods, throwing `NotImplementedException` — PL is not offered on these backends |
| `queue/fiskaltrust.Middleware.sln` | The three QueuePL projects (plus a VS version-line bump) |
| `queue/test/Manual/.../Program.cs` | The v1 launcher refuses `PL` with a pointer to the v2 launcher, instead of failing later on a missing bootstrapper |
| `.github/workflows/queue-acceptance-tests.yml` | Installs the .NET 8 SDK next to 6.0; QueuePL and its acceptance test target net8.0 while the rest of that matrix is net461;net6.0 |
| `.gitignore` | `.opencode` |
| `test/.../Test.Launcher.v2` | See the two notes below |

**Two things a reviewer should weigh in on:**

1. **`IConfigurationRepository` / `IReadOnlyConfigurationRepository` gained six members.** Every
   in-repo implementation is updated, but any implementation outside this repository will no longer
   compile. If that matters, the alternative is a separate PL-specific interface.
2. **`Test.Launcher.v2` is shared with ES, and two things there changed for both markets.** Its
   default market is now `MW_MARKET ?? "PL"` where it used to be hardcoded `"ES"` — an ES developer
   now has to set `MW_MARKET=ES`. And the builder takes the cashbox and pos-system ids from the
   configuration instead of generating them per start, filling in only the init tables the
   configuration does not carry (`Add` → `AddUnlessConfigured`). ES behaviour is otherwise unchanged;
   the `VeriFactuInMemoryClient` edit is namespace disambiguation only.

## Reading order

The branch is 42 commits; each one is scoped and its message says why. Reading the code instead:

1. [`PLEndToEndHarness`](test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest/PLEndToEndHarness.cs)
   and one business case under `BusinessCases/` — what the whole slice is asserted to do.
2. [`PosNetReceiptMapper`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Transaction/PosNetReceiptMapper.cs)
   — the densest file, and where the register's arithmetic rules live. Its tests
   ([`PosNetReceiptMapperTests`](test/fiskaltrust.Middleware.SCU.PL.UnitTest/PosNet/PosNetReceiptMapperTests.cs),
   `PosNetDiscountMappingTests`, `PosNetReversalMappingTests`) are the specification.
3. [`PosNetPLSSCD`](src/fiskaltrust.Middleware.SCU.PL.PosNet/PosNetPLSSCD.cs) — which receipt case
   becomes which register operation, and what is read back.
4. [`PosNetClient`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Client/PosNetClient.cs) and
   [`SerialPosNetTransport`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Transport/SerialPosNetTransport.cs)
   — the ambiguous-outcome handling, which is what a till operator will actually hit.
5. `QueuePL`'s processors — thin by design; the interesting parts are the invoice/KSeF decision and
   the device-unreachable state.

## Known limits

* KSeF is not implemented; invoice cases are stored and marked as not fiscalized.
* PL storage is offered on InMemory and Azure Table Storage only.
* The monthly and yearly closing cases are refused by the SCU with a validation error; only the daily report (`0x2011`) is implemented.
* Multi-rate subtotal-discount rounding is compared with one grosz of slack per PTU slot, because
  how the register rounds it has not been measured on hardware.
