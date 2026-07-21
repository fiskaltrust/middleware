# fiskaltrust.Middleware.SCU.IT.EpsonRTServer

SCU for the **Epson RT Server** (Registratore Telematico Server, e.g. FP-S series) — the multi-till fiscal
server counterpart of the Epson RT Printer. It provides the same feature set as the Custom RT Server SCU,
but talks to a physical Epson device over its SOAP/XML protocol.

Reference documents (EPSON Italia, confidential):

- *RT Server Security Communication Protocol* (FP SRV 084 EN) — token, CCDC, `fpserver.cgi` API, error codes
- *RT Server Fiscal ePOS Metadata Development Guide* (FP SRV 083 EN) — `printerFiscalReceipt` metadata

Everything marked **[device-validated]** below was verified against a real RT Server (serial `99SEA004010`,
firmware 06.01) by emitting accepted (`code=0`) documents through this SCU's full pipeline.

---

## Architecture

| Component | Responsibility |
|---|---|
| `EpsonRTServerSCU` | `IITSSCD` state machine: init / sales / refund / void / daily closing / zero receipt; per-till state (token, CCDC chain, counters) persisted on disk |
| `EpsonRTServerClient` | SOAP over `cgi-bin/fpserver.cgi` + `cgi-bin/fpmate.cgi`, HTTP Basic auth. Registered as **singleton** (one shared `HttpClient`) |
| `EpsonRTServerMapping` | `ReceiptRequest` → `printerFiscalReceipt` metadata + `createReceipt` command; local CCDC computation |
| `EpsonRTServerCommunicationQueue` | On-disk cache + strictly ordered background transmission (offline resilience) |
| `EpsonRTServerErrorCodes` | RT error table (`0` … `-52`) from the security protocol spec |
| `Models/EpsonToken` | Parser for the 49-char token returned by `createToken` |

The receipt-case routing (`ProcessReceiptAsync`) mirrors the Custom RT Server SCU; item-line semantics
(discounts, vouchers, storno, subtotal adjustments) mirror the EpsonRTPrinter SCU.

---

## The CCDC (Codice Controllo Documento Commerciale)

Unlike the Custom RT Server (HMAC key issued by the backend and kept locally), the Epson integrity code is a
**keyless SHA-256 blockchain** seeded by a server-issued token. It is computed **locally** by this SCU.

**[device-validated]** The exact formula (reverse-engineered from an accepted request, then confirmed with
our own generated XML — the server echoes back the identical fingerprint):

```
CCDC = sha256_hex_lowercase( "<receipt><hash fingerPrint=\"{sectionA}\"/>{printerFiscalReceipt}</receipt>" )
```

Critical details, all of which produce `-22 hash error` if violated:

- The hash covers the **entire `<receipt>` element exactly as transmitted**, including the `<hash>` tag
  itself; `<receiptSecurity>` is excluded.
- The `<hash>` tag inside `<receipt>` has **no space** before `/>`.
- Lowercase hex output.
- Because the server re-hashes the exact bytes it receives, the hashed string **must be byte-identical** to
  the transmitted one. `EpsonRTServerMapping` builds the `<receipt>` element once and reuses the same string
  for hashing and sending. Never re-serialize or pretty-print it in between.
- Amounts use a **dot** decimal separator (`1.00`, not `1,00`).

`sectionA` is the **token** for the first document after a `createToken`, then the **previous document's
CCDC** for every following document (blockchain).

### Token

`createToken` returns the token in `addInfo/token`. Layout (49 chars) **[device-validated]**:

| Segment | Length | Example |
|---|---|---|
| RT serial number | 11 | `99SEA004010` |
| Till ID | 8 | `FISK0001` |
| Random | 5 | `12345` |
| Date (YYYYMMDD) | 8 | `20260702` |
| Z report number | 4 | `0743` |
| **Next expected doc number** | 4 | `0001` |
| Daily amount (last 2 digits = decimals) | 9 | `000000000` |

The token is the authoritative source for seeding the local counters (`TillState`): the SCU uses
`nextDocNumber - 1` as `LastDocNumber`. A `createToken` also **reseeds the chain mid-session** — this is the
recovery path for blockchain errors (see below).

### fiscalInformation payment semantics

**[device-validated]** — getting these wrong yields `-39 receipt payments error`:

- The buckets (`cashAmount`, `checkAmount`, `ePayAmount`, `ticketAmount`, `noPayAmount*`, `discountPayment`)
  contain the **tendered** amounts.
- `changeAmount` = change due (`tendered - recAmount`, cash only).
- `paidAmount` = the **net** amount paid: `tendered - changeAmount == recAmount`. Sending the gross tendered
  amount as `paidAmount` is rejected.

---

## Offline handling

Two modes, selected by `SendReceiptsSync`:

**Synchronous (`true`)** — the caller waits for the `createReceipt` round-trip (~266 ms over WAN, less on
LAN). A device outage fails the receipt immediately.

**Asynchronous (`false`)** — recommended for production, but **requires an explicitly configured
`ServiceFolder` (or `CacheDirectory`)**: this is the only durable location the queue will ever use to buffer
documents. If neither is configured, there is no fallback — the SCU logs a warning and enforces synchronous
signing instead, so a stateless/cloud host can never buffer fiscal documents to ephemeral disk:

1. The CCDC is computed locally, so the POS gets its signatures (doc number, CCDC, QR data) **immediately**,
   even with the device unreachable. The document is cached on disk
   (`<ServiceFolder>/epsonrtservercache/<scu-id>/<till-id>/`).
2. A background loop transmits the cache every 2 s, **strictly in blockchain order per till** (the server
   validates the chain — order is not optional).
3. Network errors retry forever (being offline is a normal condition). Documents **actively rejected** by
   the server stop the till's sequence and, after `MaxDocumentSendRetries` (default 5) rejections, are
   parked in a `failed/` subfolder for manual analysis instead of blocking the queue.
4. The **zero receipt** reports `DocumentsInCache` and `SigningDeviceAvailable` in its state data — use it
   for monitoring.
5. The **daily closing** drains the cache first (`ProcessAllReceipts`) and fails visibly (with the cached
   count as a signature) if it cannot.

### Regulatory bounds on the offline window

The cache cannot lag arbitrarily; two hard constraints from the spec / V11.1 tax-authority rules:

| Constraint | Effect |
|---|---|
| **60 minutes / 10 receipts** (V11.1) | 10+ fiscal receipts received with a delay of more than 60 minutes flag the till as *offline* (`-52`). Receipts are still accepted, but the anomaly is tracked. |
| **Midnight** (forced till closure, FW 6.00) | The cache must drain within the same fiscal day; receipts dated the previous day risk `-36` / `-50` (date mismatch). |

**Operational recommendation:** alarm when `DocumentsInCache > 0` for more than ~45 minutes, and never let
the cache survive past midnight.

### State-sync recovery

If the server answers `-21 blockchain`, `-22 hash`, `-23 daily amount` or `-25 receipt number`, the local
state (chain seed and/or counters) is out of sync — e.g. after an externally triggered closure. The SCU
automatically requests a **new token** (which carries the authoritative counters), reseeds, rebuilds the
document and retries **once**. On any rejection the local chain does **not** advance (otherwise every
subsequent document would be refused too). State persistence is atomic (write-temp-then-swap) so a crash
cannot corrupt the chain seed.

### Server busy (-8)

`-8 Server busy` is transient (e.g. while a closure or Z report is being processed): every request is
retried up to `ServerBusyRetries` times with `ServerBusyRetryDelayInMs` between attempts.

### Daily closing and the server-level Z report

The fiskaltrust daily-closing receipt maps to the **till** closure (`createDailyClosure`), after draining the
cache. The **server-level** Z report (`printZReport` on `fpmate.cgi`) is a device-wide operation that
transmits the daily takings to the tax authority and keeps the device busy for a long time; on multi-till
installations it must be left to the RT Server's own schedule. It is therefore **opt-in** via
`PerformServerZReportOnDailyClosing` (default `false`). Repeatedly triggering server Z reports in short
succession can render the device unresponsive for extended periods.

### Till map programming

`createTills` **replaces the entire till map**. During the initial-operation receipt (with
`AutoProgramTillMap`), the SCU first reads the current map (`createReport/tillMap`) and only reprograms it —
preserving all existing tills — when the queue's till is missing. `zRepNumber` is intentionally omitted in
the map (it is only meant for SD-card substitution).

---

## Concurrency and throughput

Measured against the test device (over WAN; LAN in-store will be faster):

| Parallel `serverInfo` requests | Success | Wall time |
|---:|---:|---:|
| 1 | 1/1 | 2.0 s |
| 2 | 2/2 | 1.6 s |
| 4 | 4/4 | 2.9 s |
| 8 | 8/8 | 5.5 s |
| 16 | 16/16 | 9.5 s |

Interpretation:

- The device **accepts at least 16 concurrent connections without errors**, but wall time grows linearly:
  processing is **internally serialized** (single-threaded fiscal engine; `-8 Server busy` exists as an
  error code). Extra connections are just queued.
- With a warm keep-alive connection: **~62 ms** per read, **~266 ms** per `createReceipt` (WAN) — roughly
  **3–4 documents/second total** device throughput.
- **Within one till, parallelism is impossible by protocol**: document N+1 needs document N's CCDC. The SCU
  enforces this with a per-SCU-id semaphore around `ProcessReceiptAsync`.
- Across different tills the chains are independent and requests can be interleaved, but the device
  serializes internally — **total throughput is shared, not multiplied, by the number of tills**.

Design guidance: do not engineer for parallelism towards the device. One shared `HttpClient` (already a
singleton), serialized requests per till (already enforced), and the asynchronous queue to absorb bursts —
in async mode the POS never perceives the device round-trip.

---

## Configuration

| Key | Default | Notes |
|---|---|---|
| `ServerUrl` | — (required) | e.g. `https://192.168.1.10` — `cgi-bin/*.cgi` is appended automatically |
| `Username` / `Password` | `epson` / `epson` | HTTP Basic auth (device default user) |
| `SendReceiptsSync` | `true` | Set `false` for offline-resilient queue mode (recommended); requires an explicitly configured `ServiceFolder`/`CacheDirectory` — otherwise synchronous signing is enforced regardless of this setting |
| `IgnoreRTServerErrors` | `false` | `true` logs rejections instead of throwing — the chain still does not advance on rejection |
| `MaxDocumentSendRetries` | `5` | Rejections before a cached document is parked in `failed/` |
| `ServerBusyRetries` / `ServerBusyRetryDelayInMs` | `5` / `2000` | Retry policy for transient `-8 Server busy` |
| `PerformServerZReportOnDailyClosing` | `false` | Opt-in server-level Z report after the till closure (see above) |
| `RTServerHttpTimeoutInMs` | `15000` | HTTP client timeout |
| `DisableSSLValidation` | `false` | Devices ship with self-signed certificates — usually needed |
| `AutoProgramTillMap` | `true` | Programs the till (`createTills`) during the initial-operation receipt |
| `ServiceFolder` / `CacheDirectory` | personal folder | State cache and document queue locations |

The `ftCashBoxIdentification` **must be exactly 8 characters** (4-char store id + 4-char till id) and present
in the RT Server till map.

## Error codes

`EpsonRTServerErrorCodes` maps the full table (`0` … `-52`) from the security protocol. Noteworthy:

- `-8` server busy · `-20` till not in map · `-21`/`-22` blockchain/hash (auto-recovered, see above)
- `-25` receipt number error (hash was OK — validation is hash-first)
- `-37` duplicate receipt — `success=true`, logged only
- `-38`/`-39` payment vs. fiscal-information mismatches
- `-52` till deemed offline (V11.1)

### createReceipt: blocking rejection vs. accepted-with-warning

Per the RT Server "effects of the errors on the RT Server behaviour" table (**Create Receipt**
column), most negative codes on a `createReceipt` mean **"Receipt accepted with error in log
file"** — the document **is fiscally registered by the server**; the code is only a non-blocking
anomaly to log. Only a subset means **"Receipt not accepted"** (a real, blocking rejection). The
SCU classifies accordingly (`EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning`):

| Class | Codes | SCU behaviour |
|---|---|---|
| **Rejected (blocking)** | `-1..-8`, `-20`, `-28`, `-29`, `-32`, `-33`, `-34`, and any unknown negative | Sync: throw, chain does **not** advance. Queue: retry, park in `failed/` after `MaxDocumentSendRetries`. |
| **Accepted with warning** | `-27`, `-35`, `-36..-52` | Document consumed / chain advanced; a warning `SignaturItem` (`rt-server-receipt-warning`) is added. Never parked. `-52` (till offline, V11.1) is the common case. |
| **Lottery not registered** | `-43`, `-44` | Accepted, but the deferred lottery code was ignored → warning `rt-server-lottery-not-registered`. |
| **State out of sync** | `-21..-25` | Token-reseed recovery (see above) — kept separate pending device validation of whether the receipt is truly accepted for these codes. |

The warning signatures use `SignatureTypeWarning` (`0x4954_2000_0020_2000`); the `0020` group keeps
them off the customer-facing fiscal document (PDF). In **async** mode the accept/warning happens in
the background queue after the receipt already returned `StateOk`, so the warning is logged
(`LogWarning`) rather than placed on the original receipt.

## Known limitations / follow-ups

- **Instant lottery** (`createReport/codeRequest` with CEK AES encryption against the RT public key) is not
  implemented — only `GetPublicKeyAsync`. Deferred lottery (`printRecLotteryID`) is supported.
- **Deposit (Acconto, adjType 10)** and **Free of Charge (Omaggio, adjType 11)** are not emitted — no
  `ReceiptCaseHelper` flag exists to detect them (same gap as EpsonRTPrinter).
- Electronic-payment attributes introduced with FW 6.00 (`epaymentMode`/`epaymentDate`/`epaymentID`) and
  `recTotTicketNum` on `printRecTotal` are not emitted yet.
- Reprint and non-fiscal print receipt cases are not handled (printer-oriented; not part of the RT Server
  model).
