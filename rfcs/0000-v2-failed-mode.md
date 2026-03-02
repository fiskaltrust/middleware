- Feature Name: `v2_failed_mode`
- Start Date: 2026-03-02
- RFC PR: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/pull/0000)
- Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000)

# Summary

We want to handle the failed mode of the queue inside the [Localization.v2](../queue/src/fiskaltrust.Middleware.Localization.v2) so that the implementation of the failed mode is consistent across all markets and the logic is not duplicated in each market's implementation of the queue.

# Motivation

In the 1.2 middleware and the AT, DE, FR and IT implementations of the queue in the 1.3 middleware, the failed mode is handled in the market-specific implementation of the queue. This means that the logic for handling the failed mode is duplicated and possibly different in each market's implementation of the queue.

Having a single implementation of the failed mode ensures maintainability and consistency across markets as well as simplifying market-specific implementations.

# Requirements

- The processing of receipts needs to be defferd to the market-speciffic queue.
- The lifecycle of the failed mode needs to be handled in the Localization.v2, including entering and exiting the failed mode.


# Guide-level explanation

## Terminology

- **Failed mode**: A state of the queue where communication with the SCU is not possible, and the queue needs to handle receipts differently until communication is re-established. The PosSystem does not need to store receipts for reprocessing, as the queue keeps track of the failure period and the affected receipts to provide accurate information once the failure is resolved with a **Zero receipt**.
- **Late signing**: A feature that allows receipts to be processed without communication with the Middleware, and then later signed once communication is re-established.
- **Handwritten receipts**: A feature that allows receipts to be produced and processed without a working PosSystem. These receipts then need to be sent to the middleware once the PosSystem is back online.
- **Zero receipt**: A special receipt sent by the PosSystem to attempt reconnection with the SCU and to signal the end of a failure period. The zero receipt does not represent an actual receipt but is used for control flow in the failure modes.

## Failure Scenarios

### Scenario 1: SCU failing

When communication between the middleware and the Signature Creation Unit (SCU/SSCD) fails and the market supports this scenario:

1. The first failed SCU call causes the queue enter failed mode.  
2. In failed mode receipt requests skip SCU communication entirely.  
   While in failed mode, the middleware sets the `ftState` flag `0x0000_0000_0000_0002` (scu-temporarily-out-of-service) on every response.  
3. The PosSystem sends a **zero receipt** to attempt reconnection. The zero receipt forces a communication retry with the SCU. If successful, the queuereturns `ftState` `0x0000_0000_0000_0000` (Successful), and includes signature items summarizing the failure period as per the market demands. If not successful the queue stays in failed mode.

If the market does not support this scenario the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` instead. This needs to be handled as Scenario 2 by the PosSystem, as the Middleware is effectively unreachable without a working SCU.

### Scenario 2: Middleware failing

When the cash register cannot reach the middleware at all or cannot receive a successful response from the middleware and the market supports late signing:

1. The PosSystem produces receipts locally (possibly marked with some hint like "Security mechanism not reachable") and stores them for reprocessing. If the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` the response can contain additional signature items that need to be printed.
2. Once communication is re-established, the PosSystem re-sends these receipts with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing).
3.  While receipts flagged as failed are being processed, the middleware responds with an `ftState` `0x0000_0000_0000_0008` (late-signing-active).
4. After all failed receipts have been re-sent, a **zero receipt** must be sent to close the failure period. The zero receipt ends late signing mode and and includes a signature item summarizing the catch-up period as per the market demands.

> ***Note:** During late-signing-mode the middleware can still process normal receipts that are not flagged as failed. Those will be signed normally not be included in the late signing summary.*

If the market does not support the late receipts, the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` instead when sending a receipt with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing).

### Scenario 3: PosSystem Failing

This scenario is very similar to the middleware failing scenario, but instead of the PosSystem being unable to reach the middleware, the entire PosSystem is non-functional (e.g. power outage).

When the entire PosSystem is non-functional, and the market supports handwritten receipts:

1. Handwritten receipts need to be handed out. 
2. Once the PosSystem comes back online, the PosSystem sends these receipts with the `ftReceiptCase` Flag `0x0000_0000_0008_0000` (process-handwritten).
3.  While receipts flagged as handwritten are being processed, the middleware responds with an `ftState` `0x0000_0000_0000_0008` (late-signing-active).
4. After all handwritten receipts have been sent, a **zero receipt** must be sent to close the failure period. The zero receipt ends late signing mode and and includes a signature item summarizing the catch-up period as per the market demands.

If the market does not support the processing of handwritten receipts, the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` instead when sending a receipt with the `ftReceiptCase` Flag `0x0000_0000_0008_0000` (process-handwritten).

> Explain the proposal as if it was already included in the middleware and you were teaching it to a PosCreator. That generally means:
> 
> - Introducing new named concepts.
> - Explaining the feature, ideally through simple examples of solutions to concrete problems.
> - If applicable, provide sample error messages, deprecation warnings, or migration guidance.

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

> Why should we *not* do this?

# Rationale and alternatives

> - Why is this design the best in the space of possible designs?
> - What other designs have been considered and what is the rationale for not choosing them?
> - What objections immediately spring to mind? How have you addressed them?
> - What is the impact of not doing this?

# Prior art

## Failed mode behavior

### Scenario 1: SCU failing

When communication between the middleware and the Signature Creation Unit (SCU/SSCD) fails:

1. **Entering failed mode:** The first failed SCU call causes the queue to record the failure moment and the originating queue item ID, and increments a failure counter. In each market, this is tracked via market-specific fields on the market queue table (`SSCDFailCount`, `SSCDFailMoment`, `SSCDFailQueueItemId`).

2. **Circuit breaker behavior:** Once `SSCDFailCount > 0`, subsequent receipt requests skip SCU communication entirely to avoid long timeouts on every request.

3. **Response signaling:** While in SSCD failed mode, the middleware sets the `ftState` flag `0x0000_0000_0000_0002` (scu-temporarily-out-of-service) on every response to inform the PosSystem that the SCU is unreachable.

4. **Exiting failed mode:** The PosSystem must send a **zero receipt** to attempt reconnection. The zero receipt command forces a communication retry with the SCU. If successful, the queue replays all failed receipts then resets `SSCDFailCount = 0`, clears `SSCDFailMoment` and `SSCDFailQueueItemId`, returns `ftState = 0x0000_0000_0000_0000` (Successful), and includes signature items summarizing the failure period (e.g. the count of affected receipts and the time window).

### Scenario 2: Middleware failing

When the PosSystem cannot reach the middleware at all (e.g. network outage, middleware host down):

1. The PosSystem produces receipts locally (possibly marked with some hint like "Security mechanism not reachable").
2. Once communication is re-established, the PosSystem must re-send these receipts with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing).
3. The middleware tracks these re-sent receipts via `UsedFailedCount`, `UsedFailedQueueItemId`, `UsedFailedMomentMin`, and `UsedFailedMomentMax` fields on the market-specific queue table. The min/max moments track the time window of the failure based on `cbReceiptMoment` of the re-sent receipts.
4. While receipts flagged as failed are being processed, the middleware responds with an `ftState` indicating "Late Signing Mode" (`0x0000_0000_0000_0008` late-signing-active).
5. After all failed receipts have been re-sent, a **zero receipt** must be sent to close the failure period. The zero receipt resets `UsedFailedCount = 0` and the associated fields, and includes a signature item summarizing the catch-up period.

### Scenario 3: PosSystem Failing

This scenario is very similar to the middleware failing scenario, but instead of the PosSystem being unable to reach the middleware, the entire PosSystem is non-functional (e.g. power outage). The handling is the same as Scenario 2, but with a different `ftReceiptCase` Flag (`0x0000_0000_0008_0000` process-handwritten) to indicate that these are handwritten receipts.

# Unresolved questions

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
