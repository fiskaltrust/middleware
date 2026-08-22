# Queue single-currency — summary

*Short, readable companion to the full RFC: [`0705-queue-single-currency.md`](0705-queue-single-currency.md). Read that one for the implementation detail, code, and corner cases.*

## In one sentence

Every queue works in **exactly one currency**, decided by its market; the middleware rejects any receipt that doesn't match, so a queue can never mix currencies. (Same idea as a *book currency* in accounting.)

## The problem

Currency handling today is ad-hoc and unwritten:

- Most markets (AT, DE, FR, IT, GR) **ignore** the `Currency` field and assume EUR.
- ES and PT **reject non-EUR**, but each with its own hardcoded rule.

So "a queue accepts one, market-correct currency" exists only as bespoke code in two markets — and every new non-EUR market (PL, DK) would reinvent it.

## What this RFC does

1. **Specifies** the single-currency rule.
2. **Moves** the enforcement into the shared `localization.v2` layer, so every market does it the same way.
3. Adds an **optional** currency-changeover story.

It does **not** change today's EUR-default behaviour — it just makes a wrong currency **fail** instead of being silently accepted.

## How it works

- **Queue currency = the market's currency**, hardcoded per market module (EUR, PLN, …). No new DB column, no config.
- **Validation**: generalise the existing ES/PT `CurrencyMustBeEur` check into a per-market `CurrencyMustMatchMarket`, wired into every market. A request whose `Currency` ≠ the market currency is rejected; charge items and pay items must match too.
- **Counters stay dumb**: the totalizers keep summing `decimal`s blindly — which is fine *because* validation guarantees one currency per queue. `10 € + 10 PLN` can't happen; the off-currency receipt is rejected first.
- **No data-format change**: the `Currency` field already exists on `ReceiptRequest` / `ChargeItem` / `PayItem` (and defaults to EUR).

## Currency changeover (optional)

If a country changes currency, don't flip a live queue silently. Recommended: **start a new queue** (no special code needed). A same-queue switch is possible only if it *also* resets the totalizers at the switch point — otherwise it mixes currencies.

## Who's affected

- **EUR markets**: no change — *except* a request that explicitly sends a non-EUR currency (silently ignored today) will now be rejected. Audit for that traffic before rollout.
- **Non-EUR markets** (PL, DK): must send the correct currency; omitting it (→ EUR default) fails.

## Foreign-currency payments (out of scope)

Taking 100 USD against a PLN receipt is a **payment** concern — recorded as an exchange rate per pay item, never mixed into the queue total. The data model already has the hooks. Separate from this RFC.

## Open questions

- Must charge/pay items set the currency explicitly, or inherit it from the receipt?
- Support the same-queue changeover path at all, or always require a new queue?
- Keep `OnlyEuroCurrencySupported` for EUR markets, or a generic error code everywhere?
