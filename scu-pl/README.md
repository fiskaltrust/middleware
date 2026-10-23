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
* **A Polish SCU never throws out of `ProcessReceiptAsync`.** Every failure comes back as an error
  response — the plain error state (`…_EEEE_EEEE`, no PL-local error flags for now) with a `FAILURE`
  signature that says what to do next ([`SetError`](src/fiskaltrust.Middleware.SCU.PL.Abstraction/Helpers/PLReceiptResponseExtensions.cs)):
  an unreachable register is retried; an **unknown outcome** (the command reached the register, but
  no readable answer came back — `PLDeviceOutcomeUnknownException`) is checked on the register
  first, because a document may have been printed and a resend would duplicate it. QueuePL passes
  these responses through and writes no closing journal for an error. What it still catches is the
  transport to a remote SCU failing: a refused connection is retried, a timeout or a dropped
  connection is treated like an unknown outcome. See
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
  that follows. The client therefore checks that the mnemonic matches (an asynchronous `!sprn`/`!sdev`
  is answered under its plain mnemonic), and reports a mismatch as an *ambiguous outcome* — the
  command was written, and what the register did with it is exactly what the answer does not say.

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
* **A sale is never reported as failed if the register will still issue it.** A POSNET accepts a whole
  receipt even when it cannot print (cover open, no paper), holds the closed receipt and prints and
  fiscalizes it the moment the fault is cleared — no command withdraws it. Measured on a real
  printer: idle, it reports ready even with the cover open (`!sprn pr0`, `!sdev ds0 qeT`); only
  while it holds the receipt does it report `pr5 ds3 qeN`. So the SCU:
  * refuses a sale while the register is not idle (`!sprn`/`!sdev` before `trinit` and again before
    `trend`) — the receipt is **not issued**;
  * treats every failure before `trend` as **not issued** and cancels the open receipt — a receipt
    only becomes fiscal when `trend` closes it;
  * after a `trend` that was not confirmed, **waits without a time limit** until the register is idle
    again and then decides from its completed-receipt counter (`scnt bn`): issued → the sale completes
    normally, not issued → error. An error is never returned for a receipt the register still holds.

  The **daily report** gets the same guarantee — it is held and printed later like a receipt: refused
  unless the register is idle, and an unanswered `dailyrep` is settled once the register is idle again
  from `scnt rd` (fiscal memory) or `scnt hn` (every printout — the one that moves on a register in
  training mode), read before and after.

  The `FAILURE` message of a refused sale says "The receipt was NOT issued" ("The daily report was NOT
  issued" for a closing), so the POS can resend it. See `ResolveSilentTrendAsync` in [`PosNetPLSSCD`](src/fiskaltrust.Middleware.SCU.PL.PosNet/PosNetPLSSCD.cs).
* **The zero receipt returns the printer's state** in `ftStateData.PL.PrinterStatus` (ready, the
  problem in words, mechanism and device state, queue, paper near end, open transaction, fiscal and
  read-only mode), like the Italian RT printer SCUs. It runs outside the device lock and only uses the
  asynchronous status commands, so it answers while a sale waits for an open cover — that is how a POS
  finds out why the sale is waiting. See [`PosNetPrinterState`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Client/PosNetPrinterState.cs).
* **Every sale is anchored in the register.** The receipt number alone is no key, because a register
  with a daily counter repeats it every day. After `trend` the SCU therefore also reads the
  protected-memory document number (`eclastdocnoget ty0`), the register's clock (`rtcget`) and the
  daily report the receipt is booked into (`scnt.rd + 1`, signed as `CurrentDailyReportNumber`, not
  as the closing's `ZReportNumber`), and signs each of them. Status commands describe the *last*
  document, not this one, so the receipt counters are read before `trinit` and again as the last
  readback. Only if the register completed exactly one receipt (`bn`) and numbered exactly one (`bt`,
  which a receipt canceled elsewhere also advances) does the SCU sign anything. The clock is signed
  only with a known offset: `tm` must state it, and `da` is read as Warsaw time, to the minute. A
  readback the register could not answer (connection lost after `trend` — measured by pulling the
  USB cable during the footer lines) is **waited for without a time limit** and read again once the
  register is idle, so a cut never costs a fiscal receipt its number. An unprovable readback from a
  register that answers (another document came in between) leaves the values out rather than
  failing a receipt that is already printed. See [`PosNetReceiptReadback`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Transaction/PosNetReceiptReadback.cs).

  Anything that fails after `trend` (footer lines, payment slip) is reported as
  `AdditionalPrintoutNotPrinted` ending in "The fiscal receipt was issued: do not send it again."

* **The receipt footer** ([`PosNetPrintout`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Transaction/PosNetPrintout.cs), middleware#845).
  Layout measured on a POSNET against a real H&M PL receipt:

  | What | From | Register |
  | --- | --- | --- |
  | cashier in the footer line (`00008 #02 238689`) | `cbUser` (a string/number; a structured user object is left out) | `ftrcfg cc`, ≤ 32 |
  | register number in the footer line | `cbTerminalID` | `ftrcfg cn`, ≤ 8 |
  | barcode, QR code | `PL.printout.barcode` / `qrCode` | `ftrcfg bc/bb`, `qrcode` |
  | key left, value right | `PL.printout.lines[]` with `label` (e.g. `itemCount`, `transactionNumber`) | `trftrln id0–40` |
  | free text, left-aligned, normal size | `PL.printout.lines[]` with `align: "left"` | `trftrln id41` — the spec lists it without parameter, but the device prints the text |
  | free text as before | `PL.printout.lines[]` with only `text` | `trftrln id25` — right-aligned behind a 15-character column |
  | up to 3 info lines, optionally centred | `PL.printout.infoLines`, `infoLinesAlign: "center"` | `ftrcfg ln` (LF-separated, `&c`), `lb0` |

  A value longer than its field is refused by the register with error 10 (`PROTO_ERR_DATA_LENGTH`),
  not truncated. The SCU checks the limits itself and rejects the receipt with a validation error
  naming the field (e.g. a `cbTerminalID` over 8 characters) before anything is sent: that is an
  integration error the POS has to fix. The frame encoder carries LF as content (the protocol's line
  separator in `ftrcfg ln`), and still refuses STX, ETX and TAB.

#### Configuration

The SCU takes its parameters from the cashbox configuration; see
[`PosNetConfiguration`](src/fiskaltrust.Middleware.SCU.PL.PosNet/PosNetConfiguration.cs) for the
full set and the defaults.

| Parameter | Meaning |
| --- | --- |
| `DeviceUrl` | `tcp://192.168.1.50:6666` (or `host:port`), or `serial://COM9` / `usb://COM9` / `/dev/ttyACM0`, or `usbhost://1424:10B0` (vendor:product id, hex) for a USB printer the host opens itself — on Android, see below |
| `ConnectTimeoutMs`, `SendTimeoutMs`, `ReceiveTimeoutMs` | Must be positive; a command whose answer is never waited for would be reported as ambiguous on a register that answered |
| `SerialBaudRate`, `SerialParity`, `SerialStopBits`, `SerialHandshake` | Serial line settings; the defaults are the device's own (115200 8N1, no flow control, which is what a USB virtual COM port needs). A `usbhost://` printer gets them as its CDC line coding; the handshake does not apply there |
| `VatRateTable` | Empty by default — the register's own table is used |
| `OutcomeResolutionPollIntervalMs` | Pause between two status reads while waiting for a register that went silent after `trend` (default 1000). There is no limit on the wait itself |

Everything arrives from the Portal as strings, so numbers are read from strings and a cleared field
reads as the device default. A parameter of the wrong shape is reported as a PL validation error
naming the setting, not as a raw `JsonException`.

## Tests

318 tests, all runnable without hardware.

| Suite | Count | Needs |
| --- | --- | --- |
| [`SCU.PL.UnitTest`](test/fiskaltrust.Middleware.SCU.PL.UnitTest) | 228 | nothing |
| [`SCU.PL.AcceptanceTest`](test/fiskaltrust.Middleware.SCU.PL.AcceptanceTest) | 39 | the in-process printer emulator |
| [`SCU.PL.EndToEnd.AcceptanceTest`](test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest) | 14 | recorded cassettes, or a real register when `SCU_PL_POSNET_DEVICE_URL` is set |
| [`QueuePL.UnitTest`](../queue/test/fiskaltrust.Middleware.Localization.QueuePL.UnitTest) | 25 | nothing |
| [`QueuePL.AcceptanceTest`](../queue/test/fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest) | 12 | nothing |

```powershell
# the three SCU suites (410 tests)
dotnet test scu-pl/fiskaltrust.Middleware.SCU.PL.sln

# the two QueuePL suites (45 tests) — they live in the queue solution
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

### On Android

The production Android launcher is a legacy Xamarin.Android app that cannot reference the net8.0
PL packages, so Android is exercised with a test host of its own:
[`PL.Android.TestHost`](test/Manual/fiskaltrust.Middleware.PL.Android.TestHost) runs `QueuePL` with
the PosNet SCU in-process (in-memory storage — there is no PL SQLite storage yet), built by the same
`CashBoxBuilderPL` the desktop launcher uses, with one button per business case.

- **Over the network**, `tcp://…` works unchanged, also from the Android emulator.
- **Over USB**, Android gives an app no serial port, so the printer is opened through the USB host
  API: `usbhost://1424:10B0` and
  [`SCU.PL.PosNet.Android`](src/fiskaltrust.Middleware.SCU.PL.PosNet.Android), which a host adds
  with `services.AddPosNetAndroidUsbHost(context)`. The protocol side —
  [`UsbHostPosNetTransport`](src/fiskaltrust.Middleware.SCU.PL.PosNet/Transport/UsbHostPosNetTransport.cs)
  — is platform-neutral and unit-tested; the Android link only moves bytes. It needs a physical
  device with USB-OTG — the emulator cannot pass a USB device through on Windows — and has not yet
  been run against a printer.

```powershell
# with an emulator or a device attached (adb devices)
dotnet build scu-pl/test/Manual/fiskaltrust.Middleware.PL.Android.TestHost -f net10.0-android -t:Run
adb logcat -s PLTestHost
```

The Android projects are not in the solution: building them needs the .NET `android` workload,
which CI does not install.

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
