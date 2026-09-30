- Feature Name: `queue_single_currency`
- Start Date: 2026-07-08
- RFC PR: [fiskaltrust/middleware#705](https://github.com/fiskaltrust/middleware/pull/705)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->
- Markets: all — the rule lives in shared `localization.v2`; behavioural impact falls on non-EUR markets (`PL`, `DK`, …) and on EUR-market requests that send an explicit non-EUR currency

# Summary

A Queue processes and totalizes monetary amounts in a **single currency** — the **queue currency** — determined by its **market**. A receipt in any other currency is rejected.

This is a specification-and-consolidation effort: ES and PT already enforce EUR inside their own localizations, and this RFC lifts that enforcement into the shared `localization.v2` layer as one uniform rule for every market. No change to the v2 data format is required and the totalizers are unchanged. An *optional* changeover mechanism is described for the rare case of a market changing its currency.

# Motivation

Currency handling in the queue has never been specified. Most markets (AT, DE, FR, IT, GR) **ignore** the `Currency` field and effectively assume EUR — a non-EUR value is silently accepted and then mislabelled. ES and PT **reject** anything that is not EUR, but each with its own hardcoded rule (`CurrencyMustBeEur` → `OnlyEuroCurrencySupported`). There is no single, shared answer to a basic question: *what currency is a queue in, and what happens when a receipt arrives in a different one?* As we take on markets that do not use the euro (Poland/PLN is the immediate driver; Denmark/DKK already has a queue table), that answer has to be explicit, uniform, and correct — otherwise every new non-EUR market writes yet another bespoke rule (a `CurrencyMustBePln`), with nothing shared and nothing written down.

The answer we choose is that **a queue has exactly one currency and refuses anything else.** This is the same idea as a **book currency** in accounting: every entry in a ledger is kept in one currency. You never mix currencies in the books — a payment tendered in a foreign currency is handled separately, by recording its exchange rate. A queue *is* that ledger: it keeps a running total of everything it has processed, and that total only means something if everything in it is the same currency.

The rule matters most at the **sum counters (Summenzähler)**. Every queue totalizer — the generic `ftQueue.ftReceiptTotalizer`, the AT `ftCashTotalizer`, the ~150 per-rate/per-payment totalizers on `ftQueueFR` — is a plain `decimal` that blindly sums `ChargeItem.Amount`, and these totals feed the receipt-chain hashes and signatures. We **want** those counters to stay naïve — no per-currency logic, no FX. So the rule we enforce is precisely *"only accept receipts we can sum naïvely"*, i.e. all in the one queue currency. A queue can then never add PLN to EUR, because the off-currency receipt is rejected before it is ever counted.

This RFC does **not** change today's EUR-default behaviour. What it adds is: (1) a **specification** of the single-currency rule; (2) **one shared implementation** in `localization.v2` instead of N per-market copies; and (3) an *optional* changeover story. Expected outcome: a Polish queue signs and totalizes in PLN and a German queue in EUR — every market the same way — and the middleware refuses anything inconsistent instead of silently mislabelling it.

# Guide-level explanation

For a PosCreator the rule is simple: **send the currency that matches the queue's market on every receipt.** Get it wrong — or leave it out in a non-EUR market — and the receipt is rejected.

- In a **EUR market** (AT, DE, FR, IT, ES, PT, GR, …) the queue currency is EUR. `Currency` already defaults to EUR and is omitted from the JSON when it equals EUR, so if you send EUR (or nothing) you keep working exactly as before.
- In a **non-EUR market** (PL → PLN, DK → DKK) the queue currency is the local currency, and you must set `Currency` to it on every receipt **and on every charge item and pay item**.

The queue currency is not something the POS picks per receipt, and not a setting an operator configures. It stays stable while the queue runs; the one exception is a deliberate, staged [currency changeover](#currency-changeover-optional--separate-from-the-core-feature).

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

**Mixing currencies is impossible.** Sending a EUR receipt, then a PLN receipt, then a USD receipt into a Polish queue does *not* produce a total of `30`. Each request is validated against the queue currency, and the EUR and USD ones are rejected before they are ever counted.

Everything below this point is for the middleware implementers.

# Currency changeover (optional — separate from the core feature)

**This whole section is optional and independent of everything above.** Single-currency enforcement is the feature. Changeover is a separate question that arises in only one rare situation: **a country changing its official currency** (for example, adopting the euro). If that never happens for your market, none of this applies.

Even when it does happen, the **default answer needs no new feature at all:** start a **new queue** in the new currency on the changeover date. The old queue closes in the old currency, the new one opens in the new currency, and nothing is ever mixed.

An **in-place** changeover — keeping the *same* queue and switching its currency mid-life — is a **possible future extension, not something this RFC commits to building.** It is only worth pursuing if we decide, operationally, that a new queue is unacceptable. It is also harder: the queue's running total would otherwise add new-currency receipts onto an old-currency total — exactly the mixed-currency total this RFC exists to prevent — so doing it safely means resetting that total at the switch point (see [Changeover mechanics](#changeover-mechanics)). Whether to build it at all is an [open question](#unresolved-questions).

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

`Currency` already exists as a 179-value ISO-4217 enum (`EUR=0`, `PLN=8`, `DKK=5`, `USD=12`, …) on `ReceiptRequest`, `ChargeItem` and `PayItem` in the external `fiskaltrust.interface` package. It defaults to `EUR` (the enum's zero value), serializes as a string, and is omitted from output when it equals the default. We build on it as-is and deliberately do **not** change its default (see [Rationale](#rationale-and-alternatives)).

## Deriving the queue currency

No new column on `ftQueue`. The queue already carries `CountryCode` ([`ftQueue.cs`](../storage/src/fiskaltrust.storage/Models/ftQueue.cs)), which selects the market localization. The queue currency is a **hardcoded function of the market**, owned by each market's v2 localization module — for almost every market a single constant (`EUR`, `PLN`, …):

```cs
// pseudo code — owned by each market's v2 localization module
public static Currency GetQueueCurrency() => Currency.PLN;   // PL; EUR markets return Currency.EUR
```

*Version* enters only for a scheduled changeover, and means the **middleware release version** (recorded as `ProcessingVersion` on the queue item) — **not** the v1/v2 architecture. A market mid-changeover branches on that version (or an effective date) to return the old currency before the switch and the new one after; every other market ignores version entirely.

## Validation

Today [`Currency.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/Rules/Atoms/Request/Currency.cs) defines the atom `CurrencyMustBeEur` (`RuleFor(x => x.Currency).Equal(Currency.EUR)`, error code `OnlyEuroCurrencySupported`), wired in only via ES and PT (`QueueES/ValidationFV/Rules/ReceiptValidations.cs`, `QueuePT/ValidationFV/Rules/ReceiptValidations.cs`).

Generalize it to `CurrencyMustMatchMarket(expected)` and wire it into **every** market's `ReceiptValidations` through the shared [`MarketValidator`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/MarketValidator.cs). EUR markets pass `Currency.EUR` (identical behaviour to today); PL passes `Currency.PLN`; etc.

```cs
// pseudo code
public class CurrencyMustMatchMarket : AbstractValidator<ReceiptRequest>
{
    public CurrencyMustMatchMarket(Currency expected)
    {
        RuleFor(x => x.Currency)
            .Equal(expected)
            .WithErrorCode(nameof(CurrencyMustMatchMarket))
            .WithMessage(x => $"Expected currency {expected} for this queue but received {x.Currency}.");
    }
}
```

Add **charge-item / pay-item consistency**, mirroring the existing precedent [`ChargeItemValidations.CountryConsistency`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/Rules/Global/ChargeItemValidations.cs), which already compares `ftChargeItemCase.Country() == queue.CountryCode`. The new rule requires every `ChargeItem.Currency` and `PayItem.Currency` to equal the receipt currency:

```cs
// pseudo code — global item consistency, analogous to CountryConsistency
RuleForEach(x => x.cbChargeItems).Must((req, ci) => ci.Currency == req.Currency)
    .WithErrorCode(nameof(CurrencyMustMatchMarket));
RuleForEach(x => x.cbPayItems).Must((req, pi) => pi.Currency == req.Currency)
    .WithErrorCode(nameof(CurrencyMustMatchMarket));
```

This reuses the pattern ES/PT already apply in their refund validators, which compare `originalItem.Currency != refundItem.Currency`.

## Totalizers are unchanged

Accumulation stays exactly as it is:

- v2: [`QueueStorageProvider.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Storage/QueueStorageProvider.cs) — `queue.ftReceiptTotalizer += receiptjournal.ftReceiptTotal;`
- v1: [`SignProcessor.cs`](../queue/src/fiskaltrust.Middleware.Queue/SignProcessor.cs) — `UpdateQueuesLastReceipt`
- AT: `ftQueueAT.ftCashTotalizer`
- FR: the ~150 `*Totalizer` / `*CITotal*` / `*PITotal*` columns via [`QueueFRExtensions.cs`](../queue/src/fiskaltrust.Middleware.Localization.QueueFR/Extensions/QueueFRExtensions.cs)

They keep summing `ChargeItem.Amount` / `PayItem.Amount` as plain decimals. **Correctness now follows from validation**: because every accepted receipt is in the queue currency, every decimal added is in the same currency. The totalizer's currency is implicit (the queue currency), so we do **not** add a currency column. This should be documented at the accumulation sites: *"amounts are in the queue currency; single-currency is guaranteed by `CurrencyMustMatchMarket`."*

## Changeover mechanics

A changeover is not a per-receipt choice, and not a runtime setting a PosCreator flips. It rides on a **queue (middleware) update**:

- `GetQueueCurrency` is versioned. A changeover ships in a specific middleware release that returns the old currency before the effective date X and the new one after.
- When the queue is **updated** to that release, the switch happens at X. This is closer to a **queue migration that comes with a version update** than to an SCU switch or a manual toggle.
- For the *new-queue* path there is nothing to trigger — you create a fresh queue on the new release. Its totalizers start at zero in the new currency; nothing is mixed.
- For the *in-place* path (if ever built), the switch is **not totalizer-safe on its own**: because the totalizers keep accumulating unchanged, flipping the currency at X on a live queue would add new-currency receipts onto the old-currency total. The path is only valid if the affected totalizers are **reset/segmented at X** as part of that update/migration, reusing the existing FR rollover mechanism (`ResetShiftTotalizer` / `ResetDailyTotalizers` / …). Otherwise the post-X total is a PLN+EUR mix and its signature is meaningless.

Either way, no pre-X and post-X amounts are ever summed into the same total, and no existing totalizer is retroactively reinterpreted. An in-place switch that flips the currency but keeps accumulating into the old total is explicitly **not** supported.

## Corner cases

- **Omitted `Currency` in a non-EUR market** → defaults to EUR → rejected. (Desired.)
- **Receipt `PLN` but a line item defaulted to EUR** → item-consistency rule rejects it. (Forces the POS to be explicit — the intended hard check.)
- **EUR market, EUR or omitted currency** → passes both rules → no POS change, no new behaviour.
- **EUR market, explicit non-EUR currency** (e.g. `USD` sent to a German queue) → *ignored* today in AT/DE/FR/IT/GR, **rejected** after this change. This is the one EUR-market breaking case.
- **v1 requests** → no `Currency` field → unaffected, EUR-implicit; not accepted by v2-only non-EUR markets.
- **Foreign-currency payment** (paying an EUR receipt with USD cash) is **not** this feature. The DE `PayItemExtensions.GetCurrency` stub (returns `"???"`, `// TODO`) and the ME `CurrencyDetails.ExchangeRateToEuro` model are the separate FX-payment surface and stay out of scope (see [Future possibilities](#future-possibilities)).

# Drawbacks

- **Breaking for non-EUR markets** (they must now send the correct currency — and, under the item-consistency rule, on line and pay items too) and for the **narrow EUR-market case** of a request that *explicitly* sends a non-EUR currency (ignored today in AT/DE/FR/IT/GR, rejected after). The latter cohort must be found in a rollout audit before the rule goes live.
- **Item-level verbosity.** Requiring currency on every line/pay item in non-EUR markets is more work for POS integrators. (Alternative discussed in [Unresolved questions](#unresolved-questions).)
- **Per-market wiring risk.** Every market must opt the rule in; a forgotten market silently stays EUR-assuming. Mitigated by wiring through the shared `MarketValidator` with an explicit per-market currency, so a missing currency is a compile-time gap rather than a silent default.
- **Manual changeover.** A currency changeover must turn the totalizers over with the currency — a fresh queue (recommended), or a same-queue version switch that resets the totalizers at X. There is no automatic in-place switch — by design — and a same-queue switch that forgets the reset silently mixes currencies.

# Rationale and alternatives

The chosen design — **currency = f(market)**, enforced by validation, totalizers left as single-currency decimals — has the smallest surface: no data-format change, no schema change, and mixed currencies are impossible by construction. Putting the rule in the shared `localization.v2` layer (rather than copying ES/PT's per-market approach into each new market) is the point — one specified rule, wired uniformly, instead of N bespoke ones.

- **A `Currency` field on the queue (`ftQueue.Currency`)** — rejected. A persisted, configurable value can drift or be misconfigured, and it does not answer changeover (what happens to the stored value mid-life?). Deriving from the market keeps one source of truth and makes a changeover an explicit version bump.
- **A per-country default for the `Currency` datatype** — rejected. Changing the type's default per country breaks on a changeover and muddies serialization (omit-when-default is defined by the enum's zero being EUR). The type default stays EUR globally; correctness comes from validation instead.
- **`PosSystemMasterData.BaseCurrency`** — rejected. It exists but is unused, configurable master data — the same drift risk as a queue field, and not version-aware for changeover.
- **Mixed-currency totalizers (summing more than one currency in one total)** — rejected. Just as ledger entries are kept in a single **book currency**, the queue accumulates in a single market-locked currency. Mixing would demand FX rates, a rounding policy, and per-currency columns (consider FR's already-150 totalizers × N currencies) for no benefit, since the queue total is a technical receipt-chain sum, not cross-currency revenue. This does **not** reject foreign-currency *payments*: accepting a foreign tender against a book-currency receipt is legitimate and normal, but — exactly as in accounting — it is recorded as an exchange rate on the individual pay item (see [Future possibilities](#future-possibilities)), never mixed into the total.

**Impact of not doing this:** currency enforcement stays bespoke and unspecified — ES/PT keep their own hardcoded EUR gates, markets that ignore `Currency` keep silently mislabelling non-EUR values, and every new non-EUR market (PL, DK) needs its own from-scratch rule with nothing shared and no written spec to certify against.

# Prior art

- The ES/PT EUR gate (`CurrencyMustBeEur` / `OnlyEuroCurrencySupported`) is the seed this RFC generalizes.
- `ChargeItemValidations.CountryConsistency` (a charge item's `ftChargeItemCase` country must equal `queue.CountryCode`) is the exact structural precedent for a per-item currency-consistency rule.
- ES and PT refund validators already require `originalItem.Currency == refundItem.Currency`.
- DE distinguishes local vs. foreign cash by pay-item-case bits (`IsCashForeignCurrency`), but `GetCurrency` is an unfinished stub — evidence that FX payment is a separate, still-open concern.
- Montenegro's `CurrencyDetails { CurrencyCode, ExchangeRateToEuro }` is the only exchange-rate concept in the queue today (v1, ME-specific) — again FX, not queue currency.

# Unresolved questions

- **Where the market → currency constant lives**: a per-module constant surfaced through `MarketValidator`, or a shared registry keyed by `CountryCode`? Recommendation: per-market constant in the v2 localization module.
- **Item-level strictness**: must `ChargeItem.Currency` / `PayItem.Currency` be explicitly set to the market currency (safest, most verbose), or may they be left defaulted and *inherit* the receipt currency? Inheriting is friendlier but cannot distinguish "explicit EUR" from "defaulted EUR" (both serialize away), risking a silent mislabel. Recommendation: require an explicit match; revisit if POS feedback is strong.
- **In-place changeover**: support the same-queue path at all (with the mandatory totalizer reset at X), or always mandate a new queue? A new queue is the only path that is totalizer-safe without extra reset logic.
- **Error-code stability**: keep `OnlyEuroCurrencySupported` verbatim for EUR markets (message / certification stability) or replace it everywhere with a generic `CurrencyMustMatchMarket`? Certification impact to confirm.
- **Rollout audit**: do any live EUR-market integrations (AT/DE/FR/IT/GR, which ignore `Currency` today) currently send an explicit non-EUR `Currency` that `CurrencyMustMatchMarket(EUR)` would suddenly reject? Audit and coordinate before wiring the rule in.

# Future possibilities

- **Foreign-currency payments** — accepting a foreign tender against a book-currency (queue-currency) receipt: e.g. take in 100 USD, give change in local currency. The receipt stays in the queue currency; the foreign tender is proven by a recorded **exchange rate on each pay item**, so this lives at the `PayItem` level, not in the totalizer. The data model already carries the hooks — `PayItem.Currency`, the `PayItemCaseFlags.ForeignCurrency` flag, DE's foreign-cash pay-item cases (`IsCashForeignCurrency`, with the still-stubbed `GetCurrency`), Montenegro's `CurrencyDetails { CurrencyCode, ExchangeRateToEuro }`, and Belgium's `ForeignCurrencyInput` — so completing it is mostly wiring (record the rate per pay item) rather than new data structures. Distinct from, and compatible with, the queue's market-locked currency.
- **Cross-currency reporting / telemetry** — convert each queue's currency to a common reporting/base currency (`PosSystemMasterData.BaseCurrency`) for cross-market dashboards. A read-side concern, not queue semantics.
