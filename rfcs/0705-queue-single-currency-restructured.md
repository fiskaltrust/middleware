- Feature Name: `queue_single_currency`
- Start Date: 2026-07-08
- RFC PR: [fiskaltrust/middleware#705](https://github.com/fiskaltrust/middleware/pull/705)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->
- Markets: all (behavioural impact on non-EUR markets such as `PL`, `DK`)

<!--
Restructured draft applying review feedback on section altitude:
Summary = what · Motivation = why · Guide = what a PosCreator does · Reference = how · Rationale = why this design.
Intended to supersede 0705-queue-single-currency.md once the structure is agreed.
-->

# Summary

A Queue processes and totalizes monetary amounts in a **single currency** — the **queue currency** — determined by its **market**. A receipt in any other currency is rejected.

# Motivation

Currency handling in the queue has never been specified. Some markets assume every receipt is in EUR; others reject anything that is not EUR. There is no single, shared answer to a basic question: *what currency is a queue in, and what happens when a receipt arrives in a different one?* As we take on markets that do not use the euro (Poland, Denmark, …), that answer has to be explicit, uniform, and correct.

The answer we choose is that **a queue has exactly one currency and refuses anything else.** This is the same idea as a **book currency** in accounting: every entry in a ledger is kept in one currency. You never mix currencies in the books — a payment tendered in a foreign currency is handled separately, by recording its exchange rate. A queue *is* that ledger: it keeps a running total of everything it has processed, and that total only means something if everything in it is the same currency. Keeping one currency per queue keeps the books sound and keeps that running total a plain sum, with no currency juggling.

# Guide-level explanation

For a PosCreator the rule is simple: **send the currency that matches the queue's market on every receipt.** Get it wrong — or leave it out in a non-EUR market — and the receipt is rejected.

- In a **EUR market** (AT, DE, FR, IT, ES, PT, GR, …) the queue currency is EUR. `Currency` already defaults to EUR, so if you send EUR (or nothing) you keep working exactly as before.
- In a **non-EUR market** (PL → PLN, DK → DKK) the queue currency is the local currency, and you must set `Currency` to it on every receipt.

The examples below are the whole of what you need to know.

### ✅ EUR queue — currency omitted → accepted

```json
// German (EUR) queue — Currency left out, so it defaults to EUR
{ "cbReceiptReference": "R-1",
  "cbChargeItems": [ { "Amount": 10.00, "VATRate": 19 } ] }
```

Accepted. The queue total goes up by `10.00` EUR. Existing EUR integrations need no change.

### ✅ PLN queue — currency set correctly → accepted

```json
// Polish (PLN) queue
{ "cbReceiptReference": "R-2", "Currency": "PLN",
  "cbChargeItems": [ { "Amount": 10.00, "VATRate": 23, "Currency": "PLN" } ] }
```

Accepted. The queue total goes up by `10.00` PLN.

### ❌ PLN queue — currency omitted → rejected

```json
// Polish (PLN) queue — Currency left out, so it defaults to EUR
{ "cbReceiptReference": "R-3",
  "cbChargeItems": [ { "Amount": 10.00, "VATRate": 23 } ] }
```

Rejected — the defaulted EUR does not match the queue's PLN:

```json
{ "Error": "CurrencyMustMatchMarket",
  "Message": "Expected currency PLN for this queue but received EUR." }
```

### ❌ EUR queue — an explicit non-EUR currency → rejected

```json
// German (EUR) queue — explicit USD
{ "cbReceiptReference": "R-4", "Currency": "USD",
  "cbChargeItems": [ { "Amount": 10.00, "VATRate": 19, "Currency": "USD" } ] }
```

Rejected. Note this is **new**: today AT/DE/FR/IT/GR ignore `Currency`, so this receipt is silently accepted; under this RFC it fails.

```json
{ "Error": "CurrencyMustMatchMarket",
  "Message": "Expected currency EUR for this queue but received USD." }
```

### ❌ Any queue — a line item in a different currency → rejected

```json
// Polish (PLN) queue — receipt says PLN, but one item defaulted to EUR
{ "cbReceiptReference": "R-5", "Currency": "PLN",
  "cbChargeItems": [ { "Amount": 10.00, "VATRate": 23 } ] }
```

Rejected — every charge item and pay item must carry the same currency as the receipt:

```json
{ "Error": "CurrencyMustMatchMarket",
  "Message": "Charge item 0 has currency EUR but the receipt currency is PLN." }
```

*(The error envelopes above are illustrative; the exact shape follows the standard v2 validation-error response.)*

Everything below this point is for the middleware implementers.

# Currency changeover (optional — separate from the core feature)

**This whole section is optional and independent of everything above.** Single-currency enforcement is the feature. Changeover is a separate question that arises in only one rare situation: **a country changing its official currency** (for example, adopting the euro). If that never happens for your market, none of this applies.

Even when it does happen, the **default answer needs no new feature at all:** start a **new queue** in the new currency on the changeover date. The old queue closes in the old currency, the new one opens in the new currency, and nothing is ever mixed.

An **in-place** changeover — keeping the *same* queue and switching its currency mid-life — is a **possible future extension, not something this RFC commits to building.** It is only worth pursuing if we decide, operationally, that a new queue is unacceptable. It is also harder: the queue's running total would otherwise add new-currency receipts onto an old-currency total, so doing it safely means resetting that total at the switch point (see the Reference level). Whether to build it at all is an [open question](#unresolved-questions).

In short:

| Path | Work required | Status |
|------|---------------|--------|
| Single-currency enforcement | Build it | **Required** — this is the RFC |
| Changeover via a new queue | None (operational) | Always available |
| In-place changeover on one queue | Reset totalizers at switch point | **Optional**, maybe never |

# Reference-level explanation

## Scope: v2 only

`Currency` is a **v2** concept (`fiskaltrust.ifPOS.v2`). The v1 data format has no currency field and stays EUR-implicit. This RFC targets the v2 data format and the v2 processing pipeline (`fiskaltrust.Middleware.Localization.v2`).

## No data-format change

`Currency` already exists as a 179-value ISO-4217 enum (`EUR=0`, `PLN=8`, `DKK=5`, `USD=12`, …) on `ReceiptRequest`, `ChargeItem` and `PayItem` in the external `fiskaltrust.interface` package. It defaults to `EUR`, serializes as a string, and is omitted when it equals the default. We build on it as-is and do not change its default.

## Deriving the queue currency

No new column on `ftQueue`. The queue already carries `CountryCode` ([`ftQueue.cs`](../storage/src/fiskaltrust.storage/Models/ftQueue.cs)), which selects the market localization. The queue currency is a **hardcoded function of the market**, owned by each market's v2 localization module — for almost every market a single constant (`EUR`, `PLN`, …):

```cs
// pseudo code — owned by each market's v2 localization module
public static Currency GetQueueCurrency() => Currency.PLN;   // PL; EUR markets return Currency.EUR
```

*Version* enters only for a scheduled changeover, and means the **middleware release version** (recorded as `ProcessingVersion` on the queue item) — **not** the v1/v2 architecture. A market mid-changeover branches on that version (or an effective date); every other market ignores it.

## Validation

Generalize the existing ES/PT atom `CurrencyMustBeEur` ([`Currency.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/Rules/Atoms/Request/Currency.cs)) into `CurrencyMustMatchMarket(expected)` and wire it into **every** market through the shared `MarketValidator`. Add charge-item / pay-item consistency, mirroring the existing `ChargeItemValidations.CountryConsistency` precedent.

```cs
// pseudo code
RuleFor(x => x.Currency).Equal(marketCurrency);                       // receipt-level
RuleForEach(x => x.cbChargeItems).Must(ci => ci.Currency == req.Currency);
RuleForEach(x => x.cbPayItems).Must(pi => pi.Currency == req.Currency);
```

## Totalizers are unchanged

The totalizers (`ftQueue.ftReceiptTotalizer`, AT `ftCashTotalizer`, the FR per-rate/per-payment totals) keep summing `decimal`s exactly as today. They need no currency column and no new logic: validation guarantees every accepted receipt is in the queue currency, so every value added is the same currency.

## How a changeover is triggered

Not a per-receipt choice, and not a runtime setting a PosCreator flips. It rides on a **queue (middleware) update**:

- `GetQueueCurrency` is versioned. A changeover ships in a specific middleware release that returns the old currency before the effective date X and the new one after.
- When the queue is **updated** to that release, the switch happens at X. This is closer to a **queue migration that comes with a version update** than to an SCU switch or a manual toggle.
- For the *new-queue* path there is nothing to trigger — you create a fresh queue on the new release.
- For the *in-place* path (if ever built), the one-time totalizer reset at X runs as part of that update/migration.

So: self-chosen? No. SCU-switch-like? No. A migration that arrives with a queue update? Yes — that is the model.

# Drawbacks

- **Breaking for non-EUR markets** (they must now send the correct currency) and for the **narrow EUR-market case** of a request that *explicitly* sends a non-EUR currency (ignored today, rejected after). The latter must be found in a rollout audit before the rule goes live.
- **Per-market wiring:** every market must opt the rule in; mitigated by wiring through the shared `MarketValidator` so a missing currency is a compile-time gap, not a silent default.

# Rationale and alternatives

The design keeps the smallest surface: enforce at validation, leave the totalizers and the data format untouched, and derive the currency instead of storing it. Putting the rule in the shared `localization.v2` layer (rather than copying ES/PT's per-market approach into each new market) is the point — one specified rule, wired uniformly, instead of N bespoke ones.

- **A `Currency` field on the queue** — rejected: a persisted, configurable value can drift and does not answer changeover. Deriving from the market keeps one source of truth.
- **A per-country default for the `Currency` datatype** — rejected: breaks on changeover and muddies serialization; the type default stays EUR globally and correctness comes from validation.
- **`PosSystemMasterData.BaseCurrency`** — rejected: unused, configurable, not version-aware.
- **Mixed-currency totalizers (summing more than one currency in one total)** — rejected. Just as ledger entries are kept in one book currency, the queue accumulates in one currency; mixing would need FX rates, rounding rules and per-currency columns for no benefit. This does **not** reject foreign-currency *payments* — see Future possibilities.

# Prior art

- The ES/PT EUR gate (`CurrencyMustBeEur` / `OnlyEuroCurrencySupported`) is the seed this RFC generalizes.
- `ChargeItemValidations.CountryConsistency` (a charge item's country must equal `queue.CountryCode`) is the exact precedent for per-item currency consistency.
- ES/PT refund validators already require the original and refund lines to share a currency.

# Unresolved questions

- **Item-level strictness:** must charge/pay items set the currency explicitly, or inherit it from the receipt? Explicit is safest but can't tell "explicit EUR" from "defaulted EUR". Recommendation: require an explicit match.
- **In-place changeover:** support the same-queue path at all, or always mandate a new queue?
- **Error-code stability:** keep `OnlyEuroCurrencySupported` for EUR markets, or a generic `CurrencyMustMatchMarket` everywhere? (Certification impact.)

# Future possibilities

- **Foreign-currency payments** — accepting a foreign tender against a book-currency receipt (100 USD in, change in local currency). The receipt stays in the queue currency; the tender is proven by a recorded **exchange rate per pay item**, never mixed into the total. The data model already carries the hooks (`PayItem.Currency`, `PayItemCaseFlags.ForeignCurrency`, ME's `CurrencyDetails { CurrencyCode, ExchangeRateToEuro }`, BE's `ForeignCurrencyInput`), so it is mostly wiring.
- **Cross-currency reporting** — convert each queue's currency to a common reporting currency for cross-market dashboards. A read-side concern, not queue semantics.
