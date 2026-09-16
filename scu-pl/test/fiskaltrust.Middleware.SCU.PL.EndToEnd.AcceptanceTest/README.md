# SCU.PL end-to-end acceptance tests

JSON `ReceiptRequest` → `QueuePL` → PosNet SCU → POSNET register, and back: the response, the commands
that reached the register, and what the register reports having recorded are all held against the
business case that was sent.

## Business cases

`BusinessCases/<name>/*.json` — one request per folder, with `{{ ftCashBoxID }}` / `{{ ftPosSystemID }}`
placeholders. The same files are what the test launcher (`test/fiskaltrust.Middleware.Test.Launcher`)
serves under `POST /samples/<name>`; it links the folder, so a case added here shows up there.

## Where the register is

| Environment | Register |
| --- | --- |
| nothing set (CI) | the emulator: it replays `Cassettes/<TestName>.json` where one is committed, and answers from its device model (`PosNetDeviceModel`) otherwise |
| `SCU_PL_POSNET_DEVICE_URL=tcp://host:6666` | the printer at that address, over its network interface |
| `SCU_PL_POSNET_DEVICE_URL=serial://COM9` | the printer on that USB/COM port (over USB it enumerates as a virtual COM port; the serial settings default to the device's own, 115200 8N1) |
| plus `SCU_PL_POSNET_RECORD=1` | the printer, and the conversation is written to `Cassettes/<TestName>.json` — review the diff, a cassette is also written when the test failed |

```powershell
dotnet test scu-pl/test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest

$env:SCU_PL_POSNET_DEVICE_URL = 'tcp://192.168.178.58:6666'; $env:SCU_PL_POSNET_RECORD = '1'
dotnet test scu-pl/test/fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest
Remove-Item Env:SCU_PL_POSNET_DEVICE_URL, Env:SCU_PL_POSNET_RECORD
```

The suite never runs two tests at once, so a hardware run does not interleave transactions on the
printer. A fiscalized printer prints real fiscal receipts.

## What is verified

`PLEndToEndHarness.SignAndVerifyAsync` reads the register around the receipt over the SCU's own
connection (`stot`, `scnt` before; `strns`, `stot`, `scnt` after) and compares:

* the receipt's value per PTU slot, payments and change as `strns` reports them right after `trend`,
* the movement of the receipt totalizers and counters (`pn`, `bn`, `bt` by one; canceled receipts and
  the daily report number unchanged),
* the fiscal document number in the response against the register's last receipt number,

with what the `ReceiptRequest` implies (`FiscalFootprint.Of`). Discrepancies come back as sentences;
the test asserts there are none. The probe's commands are recorded into the cassette but kept out of
`SentMnemonics`, so assertions on what the SCU sent are unaffected by how much a test reads back.

Known limits: how the register rounds a subtotal discount across several PTU rates has not been
measured, so such receipts are compared with one grosz of slack per slot. Whether a non-fiscal
register totalizes its `NIEFISKALNY` printouts has not been measured either.
