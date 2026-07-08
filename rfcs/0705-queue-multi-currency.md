- Feature Name: `queue_multi_currency`
- Start Date: 2026-07-08
- RFC PR: [fiskaltrust/middleware#705](https://github.com/fiskaltrust/middleware/pull/705)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->
- Markets: `PL`, `DK`, and every non-EUR market (EUR markets unaffected except requests sending an explicit non-EUR currency)

# Summary

A Queue processes and totalizes monetary amounts in a **single currency at a time** — the **queue currency** — fixed by the queue's **market and localization version**, not assumed to be EUR and not chosen per-receipt. It stays stable while the queue runs and changes only through a deliberate, multi-step currency changeover. The v2 `Currency` field (already present on `ReceiptRequest`, `ChargeItem` and `PayItem`) becomes authoritative and is **hard-validated**: a request whose currency does not match the queue currency is rejected, so mixed-currency receipts are impossible by construction. No change to the v2 data format is required; this is a middleware validation and totalizer-semantics change scoped to the v2 processing pipeline.

# Motivation

Non-EUR markets — Poland (PLN) is the immediate driver, Denmark (DKK) already has a queue table — need the Queue to operate in local currency. Today the middleware implicitly treats everything as EUR:

- Most markets (AT, DE, FR, IT, GR) **ignore** the `Currency` field entirely and silently assume EUR.
- ES and PT **actively reject** anything that is not EUR (`CurrencyMustBeEur` → `OnlyEuroCurrencySupported`).
- There is no country→currency configuration to build on.

A Polish POS therefore cannot be served correctly: either its amounts are silently mislabelled as EUR, or (in an ES/PT-style market) it is rejected outright.

The problem is sharpest at the **sum counters (Summenzähler)**. Every queue totalizer — the generic `ftQueue.ftReceiptTotalizer`, the AT `ftCashTotalizer`, and the ~150 per-rate/per-payment totalizers on `ftQueueFR` — is a plain single-currency `decimal` that blindly sums `ChargeItem.Amount`. These totals feed the receipt-chain hashes and country signatures. If two currencies ever entered one queue you would literally be adding PLN to EUR, and every signature built on that total would be meaningless. The counter must not be a naïve `10 € + 10 PLN + 10 USD = 30`.

The queue total is **not an accounting revenue figure** — it is a technical running sum for the receipt chain. So we do not need multi-currency totalizers or FX conversion. We need one enforceable rule guaranteeing that everything in a given queue is in one known currency.

Expected outcome: a Polish queue signs and totalizes in PLN, a German queue in EUR, and the middleware refuses anything inconsistent instead of silently mislabelling it.

# Guide-level explanation

## The new concept: queue currency

Every queue operates in exactly **one currency at a time**, fixed by *which market and localization version the queue is* — not something the POS picks per receipt, and not a setting an operator configures. That currency stays stable while the queue runs; the one exception is a deliberate, multi-step **currency changeover** (see [below](#currency-changeover)). This is the same idea as a **book currency** in accounting: every entry is kept in one currency, and a foreign tender is handled separately by recording its exchange rate — never by mixing currencies in the books (see [Foreign-currency payments](#future-possibilities)).

- **EUR markets** (AT, DE, FR, IT, ES, PT, GR, …): the queue currency is EUR. Because `Currency` defaults to EUR and is *omitted from the JSON when it equals EUR*, existing POS integrations that send EUR or omit the field keep working with **no change**. The one exception: a request that *explicitly* sends a non-EUR `Currency` — silently ignored today in AT/DE/FR/IT/GR — will start being **rejected** (see the breaking-change note below).
- **Non-EUR markets** (PL → PLN, DK → DKK): the queue currency is the local currency. The POS **must** set `Currency` accordingly. If it omits the field, the value defaults to EUR, which does not match the queue currency, and the request is **rejected**.

This is a breaking change chiefly for **non-EUR markets**, which are onboarded deliberately — but it is **not** fully transparent for EUR markets either. Today AT/DE/FR/IT/GR *ignore* `Currency`, so a live v2 integration that sends an explicit non-EUR `Currency` (say `USD`) to a German queue is silently accepted now, but will be **rejected** once `CurrencyMustMatchMarket(EUR)` is wired in. EUR-market requests that send `EUR` or omit the field are unaffected; requests carrying an explicit non-EUR currency are the breaking case, and must be found in the [rollout audit](#unresolved-questions) before the rule goes live.

## Examples

**Poland — correct request (accepted):**

```json
{
  "cbReceiptReference": "R-2026-0001",
  "Currency": "PLN",
  "cbChargeItems": [
    { "Amount": 10.00, "VATRate": 23, "Currency": "PLN", "ftChargeItemCase": "..." }
  ]
}
```

The receipt is accepted; the totalizer accumulates `10.00` **in PLN**.

**Poland — missing currency (rejected):**

```json
{
  "cbReceiptReference": "R-2026-0002",
  "cbChargeItems": [ { "Amount": 10.00, "VATRate": 23, "ftChargeItemCase": "..." } ]
}
```

`Currency` is absent → defaults to `EUR` → does not match the queue currency `PLN` → **rejected**:

> Expected currency `PLN` for this queue but received `EUR`. (`CurrencyMustMatchMarket`)

**Mixing currencies is impossible.** Sending a EUR receipt, then a PLN receipt, then a USD receipt into a Polish queue does *not* produce a totalizer of `30`. Each request is validated against the queue currency, and the EUR and USD ones are rejected before they are ever counted. The mixed-currency counter simply cannot occur.

## Item-level currency must be consistent

`ChargeItem.Currency` and `PayItem.Currency` must match the receipt currency (which must match the queue currency). A receipt declaring `PLN` but carrying a `EUR` line item is rejected. Because item currency *also* defaults to EUR when omitted, a non-EUR POS must set the currency on the lines too — omitting it fails, which is the intended "hard check" behaviour (if the field is left off, it fails).

## Currency changeover

A queue's currency is stable while it runs, but it is **not** immutable forever — a country can change its currency (e.g. a future €-adoption). This must never happen silently on an existing queue; it is a deliberate, multi-step process.

The crux is the **sum counters**: the totalizers accumulate across the queue's whole life and this RFC does *not* reset them (see [§3](#3-totalizers--no-schema-change-semantics-documented)). So a changeover **must establish a fresh counter boundary at the switch point X** — the queue currency and its totalizers have to turn over *together*. If a queue simply started accepting the new currency at X, the first new-currency receipt would be added onto the old-currency total, producing exactly the mixed-currency total this RFC exists to prevent. Two supported paths:

1. **A new queue** *(recommended)* — start a fresh queue in the new currency from X. Its totalizers begin at zero in the new currency, so nothing is ever mixed. This is the clean default.
2. **Two transition versions on the same queue** — ship a pair of localization versions (current-currency and new-currency) that switch behaviour at X, **and reset/segment the affected totalizers at X** so the new-currency run starts from a fresh boundary (the same rollover mechanism FR already uses for shift/day/month/year totals). Without that reset this path is **invalid**, because it mixes currencies in the totalizer.

An in-place switch that flips the currency but keeps accumulating into the old total is explicitly **not** supported: it would retroactively reinterpret and mix amounts already totalized in the old currency.

# Reference-level explanation

## Scope: v2 only

`Currency` is a **v2** concept (`fiskaltrust.ifPOS.v2`). The v1 data format has no currency field and stays EUR-implicit. This RFC targets the v2 data format and the v2 processing pipeline (`fiskaltrust.Middleware.Localization.v2`).

## No data-format change is required

`Currency` already exists as a 179-value ISO-4217 enum (`EUR=0`, `PLN=8`, `DKK=5`, `USD=12`, …) on `ReceiptRequest`, `ChargeItem` and `PayItem` in the external **`fiskaltrust.interface`** package. It defaults to `EUR` (the enum's zero value), serializes as a string, and is omitted from output when it equals the default. We build on this as-is. We deliberately **do not** change its default (see [Rationale](#rationale-and-alternatives)).

## 1. Deriving the queue currency (market + version → currency)

No new column is added to `ftQueue`. The queue already carries `CountryCode` ([`storage/src/fiskaltrust.storage/Models/ftQueue.cs`](../storage/src/fiskaltrust.storage/Models/ftQueue.cs)), which selects the market localization. The queue currency is a **hardcoded function of (market, localization version)**, owned by each market's v2 localization module. For a market with a scheduled changeover the function branches on version (or effective date); otherwise it is a single constant.

```cs
// pseudo code — owned by each market's v2 localization module
// EUR markets return Currency.EUR (behaviour identical to today)
public static Currency GetQueueCurrency(/* version, if a changeover is in flight */)
    => Currency.PLN;   // PL
```

## 2. Validation — generalize the existing EUR gate

Today [`queue/src/fiskaltrust.Middleware.Localization.v2/Validation/Rules/Atoms/Request/Currency.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/Rules/Atoms/Request/Currency.cs) defines the atom `CurrencyMustBeEur` (`RuleFor(x => x.Currency).Equal(Currency.EUR)`, error code `OnlyEuroCurrencySupported`), wired in only via ES and PT (`QueueES/ValidationFV/Rules/ReceiptValidations.cs`, `QueuePT/ValidationFV/Rules/ReceiptValidations.cs`).

Generalize it to `CurrencyMustMatchMarket(expected)` and wire it into **every** market's `ReceiptValidations` through the shared `MarketValidator` ([`.../Validation/MarketValidator.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/MarketValidator.cs)). EUR markets pass `Currency.EUR` (identical behaviour to today); PL passes `Currency.PLN`; etc.

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

Add **charge-item / pay-item consistency**, mirroring the existing country-consistency precedent `ChargeItemValidations.CountryConsistency` ([`.../Rules/Global/ChargeItemValidations.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Validation/Rules/Global/ChargeItemValidations.cs)), which already compares `ftChargeItemCase.Country() == queue.CountryCode`. The new rule requires every `ChargeItem.Currency` and `PayItem.Currency` to equal the receipt currency:

```cs
// pseudo code — global item consistency, analogous to CountryConsistency
RuleForEach(x => x.cbChargeItems).Must((req, ci) => ci.Currency == req.Currency)
    .WithErrorCode(nameof(CurrencyMustMatchMarket));
RuleForEach(x => x.cbPayItems).Must((req, pi) => pi.Currency == req.Currency)
    .WithErrorCode(nameof(CurrencyMustMatchMarket));
```

This reuses the pattern ES/PT already apply in their refund validators, which compare `originalItem.Currency != refundItem.Currency`.

## 3. Totalizers — no schema change, semantics documented

Accumulation stays exactly as it is:

- v2: [`.../Storage/QueueStorageProvider.cs`](../queue/src/fiskaltrust.Middleware.Localization.v2/Storage/QueueStorageProvider.cs) — `queue.ftReceiptTotalizer += receiptjournal.ftReceiptTotal;`
- v1: [`queue/src/fiskaltrust.Middleware.Queue/SignProcessor.cs`](../queue/src/fiskaltrust.Middleware.Queue/SignProcessor.cs) — `UpdateQueuesLastReceipt`
- AT: `ftQueueAT.ftCashTotalizer`
- FR: the ~150 `*Totalizer` / `*CITotal*` / `*PITotal*` columns via [`QueueFR/Extensions/QueueFRExtensions.cs`](../queue/src/fiskaltrust.Middleware.Localization.QueueFR/Extensions/QueueFRExtensions.cs)

They keep summing `ChargeItem.Amount` / `PayItem.Amount` as plain decimals. **Correctness now follows from validation**: because every accepted receipt is in the queue currency, every decimal added is in the same currency. The totalizer's currency is implicit (the queue currency), so we do **not** add a currency column. This should be documented at the accumulation sites: *"amounts are in the queue currency; single-currency is guaranteed by `CurrencyMustMatchMarket`."*

## 4. Changeover mechanics

`GetQueueCurrency(version)` is the single switch point, but the switch is **not totalizer-safe on its own**: because [§3](#3-totalizers--no-schema-change-semantics-documented) keeps the totalizers accumulating unchanged, flipping the currency at X on a live queue would add new-currency receipts onto the old-currency total. A changeover therefore has to turn the totalizers over together with the currency:

- **New queue (recommended)** — a fresh queue starts its totalizers at zero in the new currency; nothing is mixed.
- **Same queue, two transition versions** — only valid if the affected totalizers are **reset/segmented at X**, reusing the existing FR rollover mechanism (`ResetShiftTotalizer` / `ResetDailyTotalizers` / …). Otherwise the post-X total is a PLN+EUR mix and its signature is meaningless.

Either way, no pre-X and post-X amounts are ever summed into the same total, and no existing totalizer is retro-reinterpreted.

## 5. Corner cases

- **Omitted `Currency` in a non-EUR market** → defaults to EUR → rejected. (Desired.)
- **Receipt `PLN` but a line item defaulted to EUR** → item-consistency rule rejects it. (Forces the POS to be explicit — the intended hard check.)
- **EUR market, EUR or omitted currency** → passes both rules → no POS change, no new behaviour.
- **EUR market, explicit non-EUR currency** (e.g. `USD` sent to a German queue) → *ignored* today in AT/DE/FR/IT/GR, **rejected** after this change. This is the one EUR-market breaking case.
- **v1 requests** → no `Currency` field → unaffected, EUR-implicit; not accepted by v2-only non-EUR markets.
- **Foreign-currency payment** (paying an EUR receipt with USD cash) is **not** this feature. The DE `PayItemExtensions.GetCurrency` stub (returns `"???"`, `// TODO`) and the ME `CurrencyDetails.ExchangeRateToEuro` model are the separate FX-payment surface and stay out of scope (see [Future possibilities](#future-possibilities)).

# Drawbacks

- **Breaking change for non-EUR markets** (plus a narrow EUR-market case). A non-EUR POS must now send the correct currency — and, under the item-consistency rule, on line and pay items too — or be rejected. EUR markets are untouched *except* for requests that explicitly send a non-EUR `Currency`, which AT/DE/FR/IT/GR ignore today but would reject after this change; that cohort must be found in the rollout audit before wiring the rule in.
- **Item-level verbosity.** Requiring currency on every line/pay item in non-EUR markets is more work for POS integrators. (Alternative discussed in [Unresolved questions](#unresolved-questions).)
- **Per-market wiring risk.** Every market's `ReceiptValidations` must opt into the rule; a forgotten market silently stays EUR-assuming. Mitigated by wiring through the shared `MarketValidator` with an explicit per-market currency, so a missing currency is a compile-time gap rather than a silent default.
- **Manual changeover.** A currency changeover must turn the totalizers over with the currency — a fresh queue (recommended), or a same-queue version switch that resets the totalizers at X. There is no automatic in-place switch — by design — and a same-queue switch that forgets the reset silently mixes currencies.

# Rationale and alternatives

The chosen design — **currency = f(market, version)**, enforced by validation, totalizers left as single-currency decimals — has the smallest surface: no data-format change, no schema change, and mixed currencies are impossible by construction.

- **Alternative A — a `Currency` field on the Queue (`ftQueue.Currency`).** Rejected. It adds a persisted, configurable dimension that can drift or be misconfigured, and it does not answer changeover (what happens to the stored value mid-life?). Deriving from market+version keeps a single source of truth and makes changeover an explicit version bump. *This is the conclusion reached in discussion: the country and the version define a hardcoded currency, not a queue field.*
- **Alternative B — a per-country default value for the `Currency` datatype.** Rejected. Changing the type's default per country breaks on a currency changeover and muddies serialization (omit-when-default is defined by the enum's zero being EUR). The type default stays EUR globally; correctness is enforced by validation instead.
- **Alternative C — use `PosSystemMasterData.BaseCurrency`.** Rejected. It exists but is unused, configurable master data — same drift/misconfiguration risk as a queue field, and it is not version-aware for changeover.
- **Alternative D — multi-currency totalizers (mixing currencies within one queue total).** Rejected. This mirrors a core accounting principle: just as ledger entries are kept in a single **book currency**, the queue accumulates in a single market-locked currency — the two are equivalent. Mixing currencies in one total would demand FX rates, a rounding policy, and per-currency columns (consider FR's already-150 totalizers × N currencies) for no benefit, since the queue total is a technical receipt-chain sum, not cross-currency revenue. This does **not** reject *foreign-currency payments*: accepting a foreign tender against a book-currency receipt is legitimate and normal, but — exactly as in accounting — it is recorded as an exchange rate on the individual pay item (see [Future possibilities](#future-possibilities)), and never mixes currencies in the total.

**Impact of not doing this:** non-EUR markets (PL, DK) cannot be served correctly; totalizers and the signatures built on them risk silently mixing currencies; and ES/PT's ad-hoc EUR gate remains the only currency handling in the middleware.

# Prior art

- The ES/PT EUR gate (`CurrencyMustBeEur` / `OnlyEuroCurrencySupported`) is the seed this RFC generalizes.
- `ChargeItemValidations.CountryConsistency` (a charge item's `ftChargeItemCase` country must equal `queue.CountryCode`) is the exact structural precedent for a per-item currency-consistency rule.
- ES and PT refund validators already require `originalItem.Currency == refundItem.Currency`.
- DE distinguishes local vs. foreign cash by pay-item-case bits (`IsCashForeignCurrency`), but `GetCurrency` is an unfinished stub — evidence that FX payment is a separate, still-open concern.
- Montenegro's `CurrencyDetails { CurrencyCode, ExchangeRateToEuro }` is the only exchange-rate concept in the queue today (v1, ME-specific) — again FX, not queue currency.

# Unresolved questions

- **Where the (market, version) → currency constant lives**: a per-module constant surfaced through `MarketValidator`, or a shared registry keyed by `CountryCode`? Recommendation: per-market constant in the v2 localization module.
- **Item-level strictness**: must `ChargeItem.Currency` / `PayItem.Currency` be explicitly set to the market currency (safest, most verbose), or may they be left defaulted and *inherit* the receipt currency? Inheriting is friendlier but cannot distinguish "explicit EUR" from "defaulted EUR" (both serialize away), risking a silent mislabel. Recommendation: require an explicit match; revisit if POS feedback is strong.
- **Canonical changeover path**: this RFC recommends a **new queue** as the default, because it is the only path that is totalizer-safe without extra reset logic. The same-queue two-version path stays available but must reset the totalizers at X. Confirm whether the same-queue path is worth supporting at all, or whether changeovers should simply mandate a new queue.
- **Error-code stability**: keep `OnlyEuroCurrencySupported` verbatim for EUR markets (message / certification stability) or replace it everywhere with a generic `CurrencyMustMatchMarket`? Certification impact to confirm.
- **Rollout audit**: do any live EUR-market integrations (AT/DE/FR/IT/GR, which ignore `Currency` today) currently send an explicit non-EUR `Currency` that `CurrencyMustMatchMarket(EUR)` would suddenly reject? Audit and coordinate before wiring the rule in.

# Future possibilities

- **Foreign-currency payments** — accepting a foreign tender against a book-currency (queue-currency) receipt: e.g. take in 100 USD, give change in local currency. The receipt stays in the queue currency; the foreign tender is proven by a recorded **exchange rate on each pay item**, so this lives at the `PayItem` level, not in the totalizer. The data model already carries the hooks — `PayItem.Currency`, the `PayItemCaseFlags.ForeignCurrency` flag, DE's foreign-cash pay-item cases (`IsCashForeignCurrency`, with the still-stubbed `GetCurrency`), Montenegro's `CurrencyDetails { CurrencyCode, ExchangeRateToEuro }`, and Belgium's `ForeignCurrencyInput` — so completing it is mostly wiring (record the rate per pay item) rather than new data structures. Distinct from, and compatible with, the queue's market-locked currency.
- **Multi-currency reporting / telemetry** — convert the queue currency to a reporting/base currency (`PosSystemMasterData.BaseCurrency`) for cross-market dashboards. A read-side concern, not queue semantics.
- **Per-workstation queues** — raised in the same discussion in the context of per-register (per-workstation) day-end, which needs a queue per workstation rather than one per outlet. Orthogonal to currency, but it shares the "one queue = one well-defined scope" principle and warrants its own RFC.
