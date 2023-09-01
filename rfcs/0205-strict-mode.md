- Feature Name: `strict_mode`
- Start Date: 2023-08-29
- RFC PR: [fiskaltrust/middleware#205](https://github.com/fiskaltrust/middleware/pull/205)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->

# Summary

There are some situations in the Middleware where warnings are logged, but the Middleware still continues to work.
In Strict Mode these warnings should be treated as errors and the Middleware should throw an Exception.

# Motivation

The Middleware should generally sign be as lenient as possible in production so the PosOperator is not prevented from issuing receipts.
This means we have to allow some incorrect receipts to be signed by the Middleware in production.

On the other hand in sandbox we want to be as strict as possible so the PosCreator can see and fix all issues before going to production.

For example the warning `"Aggregated sum of ChargeItem amounts ({chargeAmount}) does not match the sum of PayItem amount ({payAmount}). This is usually a hint for an implementation issue. Please see https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc for more details."` is logged when the sum of the ChargeItem amounts does not match the sum of the PayItem amounts.
This is usually a sign for an implementation issue but in production we don't want to halt operation because of that.

Some of those cases are also only warnings instead of exceptions because there are some edge cases where the warning is not an issue.
To allow for those edge cases the PosCreators need to be able to decide if they want to treat those warnings as errors or not.

If a PosCreators implementation does not work in Strict Mode they are responsible them selves for possible issues that arise from that.

# Guide-level explanation

To solve this problem we introduce the Strict Mode.
In Strict Mode warnings that are indicative of implementation issues will be treated as errors and the Middleware will throw an Exception.

Per default Strict Mode is enabled in sandbox and disabled in production.

The default setting can be overridden in the Queue configuration in the portal by setting the parameter `StrictMode` to `true` or `false`.

## Example

If Strict Mode is disabled and the sum of the ChargeItem amounts does not match the sum of the PayItem amounts the Middleware will sign the ReceiptRequest and log the warning
`"Aggregated sum of ChargeItem amounts ({chargeAmount}) does not match the sum of PayItem amount ({payAmount}). This is usually a hint for an implementation issue. Please see https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc for more details."`.

If Strict Mode is enabled and the sum of the ChargeItem amounts does not match the sum of the PayItem amounts the Middleware will throw an Exception and log the error
`"Aggregated sum of ChargeItem amounts ({chargeAmount}) does not match the sum of PayItem amount ({payAmount}). This is usually a hint for an implementation issue. Please see https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc for more details."`.

## Warning Level Configuration

The Strict Mode can be disabled for each warning individually by setting the parameter `StrictModeOverrides` to a JSON dictionary containing `"<warning-id>": true` or `"<warning-id>": false` for each warning to override in the Queue configuration in the portal.

### Example `StrictModeOverrides`

```json
{ "MissmatchingAmounts": false }
```

# Reference-level explanation

> This is the technical portion of the RFC.
> Try to capture the broad implementation strategy,
> and then focus in on the tricky details so that:
> 
> - Its interaction with other features is clear.
> - It is reasonably clear how the feature would be implemented.
> - Corner cases are dissected by example.
> - Discuss how this impacts the ability to read, understand, and maintain middleware code.
>   Code is read and modified far more often than written; will the proposed feature make code easier to maintain?
> 
> When necessary, this section should return to the examples given in the previous section and explain the implementation details that make them work.
> 
> When writing this section be mindful of the following:
> - **RFCs should be scoped:** Try to avoid creating RFCs for huge design spaces that span many features.
>   Try to pick a specific feature slice and describe it in as much detail as possible.
>   Feel free to create multiple RFCs if you need multiple features.
> - **RFCs should avoid ambiguity:** Two developers implementing the same RFC should come up with nearly identical implementations.
> - **RFCs should be "implementable":** Merged RFCs should only depend on features from other merged RFCs and existing features.
>   It is ok to create multiple dependent RFCs, but they should either be merged at the same time or have a clear merge order that ensures the "implementable" rule is respected.

# Drawbacks

It brings extra complexity to the Middleware.

# Rationale and alternatives

> - Why is this design the best in the space of possible designs?
> - What other designs have been considered and what is the rationale for not choosing them?
> - What objections immediately spring to mind? How have you addressed them?
> - What is the impact of not doing this?

# Unresolved questions

* Do we need to provide the possibility to set warning levels for each warning individually?
* How is this related to the Basic Receipt Check in the portal?
  Should the Basic Receipt Check check also adhere to the `StrictMode` and `StrictModeOverrides`?
  Can we do this with a common code base?

> - What parts of the design do you expect to resolve through the RFC process before this gets merged?
> - What parts of the design do you expect to resolve through the implementation of this feature before before the feature PR is merged?
> - What related issues do you consider out of scope for this RFC that could be addressed in the future independently of the solution that comes out of this RFC?

# \[Optional\] Future possibilities

> Think about what the natural extension and evolution of your proposal would be and how it would affect the middleware and ecosystem as a whole in a holistic way.
> Try to use this section as a tool to more fully consider all possible interactions with the project in your proposal.
> Also consider how this all fits into the roadmap for the project and of the relevant sub-team.
> 
> This is also a good place to "dump ideas", if they are out of scope for the RFC you are writing but otherwise related.
> 
> Note that having something written down in the future-possibilities section is not a reason to accept the current or a future RFC;
> such notes should be in the section on motivation or rationale in this or subsequent RFCs.
> The section merely provides additional information.