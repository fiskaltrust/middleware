- Feature Name: `v2_failed_mode`
- Start Date: 2026-03-02
- RFC PR: [fiskaltrust/middleware#0630](https://github.com/fiskaltrust/middleware/pull/0630)

# Summary

We want to handle the failed mode of the queue inside the [Localization.v2](../queue/src/fiskaltrust.Middleware.Localization.v2) so that the implementation of the failed mode is consistent across all markets and the logic is not duplicated in each market's implementation of the queue.

# Motivation

In the 1.2 middleware and the AT, DE, FR and IT implementations of the queue in the 1.3 middleware, the failed mode is handled in the market-specific implementation of the queue. This means that the logic for handling the failed mode is duplicated and possibly different in each market's implementation of the queue.

Having a single implementation of the failed mode ensures maintainability and consistency across markets as well as simplifying market-specific implementations.

# Requirements

- The processing of receipts needs to be deferred to the market-specific queue.
- The lifecycle of the failed mode needs to be handled in the Localization.v2, including entering and exiting the failed mode.


# Guide-level explanation

## Terminology

- **Failed mode**: A state of the queue where communication with the SCU is not possible, and the queue needs to handle receipts differently until communication is re-established. The PosSystem does not need to store receipts for reprocessing, as the queue keeps track of the failure period and the affected receipts to provide accurate information once the failure is resolved with a **Zero receipt**.
- **Late signing**: A feature that allows the PosSystem to produce receipts without communication with the Queue, and then later sign them once communication with the Queue is re-established.
- **Handwritten receipts**: A feature that allows receipts to be produced and processed without a working PosSystem. These receipts then need to be sent to the Queue once the PosSystem is back online.
- **Zero receipt**: A special receipt sent by the PosSystem to attempt a communication reconnection and to signal the end of a failure period. The zero receipt does not represent an actual receipt but is used for control flow in the failure modes.

## Failure Scenarios

### Scenario 1: SCU failing

When communication between the Queue and the Signature Creation Unit (SCU/SSCD) fails and the market supports this scenario:

1. The first failed SCU call causes the queue to enter failed mode.  
2. In failed mode receipt requests skip SCU communication entirely. They are stored by the queue and will be processed automatically according to market regulations once the failure is resolved (See step 3.).  
   While in failed mode, the Queue sets the `ftState` flag `0x0000_0000_0000_0002` (scu-temporarily-out-of-service) on every response and the PosSystem can continue to operate normally.  
3. The PosSystem sends a **zero receipt** to attempt reconnection. The zero receipt forces a communication retry with the SCU. If successful, the queue processes all receipts accumulated during the failure period, returns `ftState` `0x0000_0000_0000_0000` (Successful), and includes signature items summarizing the failure period as per the market demands. If not successful, the queue stays in failed mode.

If the market does not support this scenario the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` instead. In those markets this Scenario does not exist and Scenario 2 and Scenario 3 are the only failure scenarios that can occur.

### Scenario 2: Queue failing

When the cash register cannot reach the Queue at all or cannot receive a successful response from the Queue and the market supports late signing:

1. The PosSystem can not reach the Queue, produces receipts locally (possibly marked with some hint like "Security mechanism not reachable") and stores them for reprocessing.
2. Once communication is re-established, the PosSystem re-sends these receipts with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing).
3. While receipts flagged as failed are being processed, the Queue responds with an `ftState` `0x0000_0000_0000_0008` (late-signing-active).
4. After all failed receipts have been re-sent, a **zero receipt** must be sent to close the failure period. The zero receipt ends late signing mode and includes a signature item summarizing the catch-up period as per the market demands.

> ***Note:** During late-signing mode the Queue will only process receipts that are flagged for late signing. The late signing mode needs to be ended with a zero receipt as described in step 4 before the Queue will process normal receipts again. The queue will return an `ftState` `0x0000_0000_EEEE_EEEE` when receipts without the late-signing flag are sent until the zero receipt is sent and late signing mode is ended. Normal receipts need to be stored by the PosSystem and sent with the late-signing flag aswell*

If the market does not support the late receipts, the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` instead when sending a receipt with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing).

### Scenario 3: PosSystem Failing

This scenario is very similar to the Queue failing scenario, but instead of the PosSystem being unable to reach the Queue, the entire PosSystem is non-functional (e.g. power outage).

When the entire PosSystem is non-functional, and the market supports handwritten receipts:

1. Handwritten receipts need to be handed out. 
2. Once the PosSystem comes back online, the PosSystem sends these receipts with the `ftReceiptCase` Flag `0x0000_0000_0008_0000` (process-handwritten).
3. While receipts flagged as handwritten are being processed, the Queue responds with an `ftState` `0x0000_0000_0000_0008` (late-signing-active).
4. After all handwritten receipts have been sent, a **zero receipt** must be sent to close the failure period. The zero receipt ends late signing mode and includes a signature item summarizing the catch-up period as per the market demands.

> ***Note:** During late-signing mode the Queue will only process receipts that are flagged as handwritten. The late signing mode needs to be ended with a zero receipt as described in step 4 before the Queue will process normal receipts again. The queue will return an `ftState` `0x0000_0000_EEEE_EEEE` when receipts without the handwritten flag are sent until the zero receipt is sent and late signing mode is ended. Normal receipts need to be stored by the PosSystem and sent with the late-signing flag after the handwritten late-signing period is ended with a zero receipt.*

If the market does not support the processing of handwritten receipts, the queue returns an `ftState` `0x0000_0000_EEEE_EEEE` instead when sending a receipt with the `ftReceiptCase` Flag `0x0000_0000_0008_0000` (process-handwritten).

## Concurrent failure scenarios

### Scenario 1 -> Scenario 2

It's possible that the Queue already is in failed-mode (Scenario 1) when the connection from the PosSystem to the Queue breaks (Scenario 2).

In that case the PosSystem needs to store receipts for reprocessing as in Scenario 2 and re-send them later once communication is re-established.

Now there's two possible ways forward.

1. A Zero receipt is sent to end the failed mode of Scenario 1. In this case the Queue exits failed mode normally. Then the PosSystem can send the stored receipts with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing) and the Queue enters late signing mode as in Scenario 2. A second zero receipt is sent to end late signing mode as in Scenario 2.
2. The PosSystem re-sends the stored receipts with the `ftReceiptCase` Flag `0x0000_0000_0001_0000` (process-late-signing) while the Queue is still in failed mode (This may be necessary if the SCU communication is still down and the Zero receipt is still failing). In this case the Queue enters late signing mode as in Scenario 2 while still being in failed mode and returns with the combined `ftState` `0x0000_0000_0000_000A` (`0x0000_0000_0000_0008 | 0x0000_0000_0000_0002` late-signing-active and scu-temporarily-out-of-service). Once communication with the SCU is re-established, the PosSystem sends a Zero receipt and exits failed mode (Scenario 1) and late signing mode (Scenario 2) at the same time.

### Scenario 2 -> Scenario 1

It's also possible that the SCU connection breaks (Scenario 1) during late signing (Scenario 2). In that case the Queue will also enter failed mode (as in Scenario 1) and return the combined `ftState` `0x0000_0000_0000_000A` like in "Scenario 1 -> Scenario 2". The PosSystem can continue to re-send late receipts as in Scenario 2, but the queue will not process them until the SCU connection is re-established and a Zero receipt is sent which will end both failed mode (Scenario 1) and late signing mode (Scenario 2) at the same time.

### Scenario 3 -> Scenario 1

This case is handled the same as Scenario 2 -> Scenario 1.

### Scenario 1 -> Scenario 3

This case is handled the same as Scenario 1 -> Scenario 2.

### Combinations of Scenario 3 and Scenario 2

It's possible the failure Scenario switches between Scenario 2 to Scenario 3. In that case each period of Scenario 2 and Scenario 3 needs to be handled as described in their respective sections and each terminated with a Zero receipt to end the failure period. The queue will not accept receipts flagged as late-signing while in late-signing mode for handwritten receipts and vice versa. If the PosSystem attempts to send a receipt flagged as late-signing while in late-signing mode for handwritten receipts, the queue returns an `ftState` `0x0000_0000_EEEE_EEEE`.


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

# \[Optional\] Prior art

> Discuss prior art, both the good and the bad, in relation to this proposal.
> A few examples of what this can include are:
> 
> - Does this feature exist in other markets and what experience have their community had?
> - Does this feature exist in other PosSystems and what experience have their community had?
> - Papers: Are there any published papers or great posts that discuss this?
>   If you have some relevant papers to refer to, this can serve as a more detailed theoretical background.
> 
> This section is intended to encourage you as an author to think about the lessons from other markets and projects, provide readers of your RFC with a fuller picture.
> If there is no prior art, that is fine - your ideas are interesting to us whether they are brand new or not.
> 
> Note that while precedent set by other projects and markets is some motivation, it does not on its own motivate an RFC.

# Unresolved questions

- The `ftState` `0x0000_0000_EEEE_EEEE` is used as a catch-all for multiple different error conditions:
  - Market doesn't support SCU failed mode (Scenario 1)
  - Market doesn't support late signing (Scenario 2)
  - Market doesn't support handwritten receipts (Scenario 3)
  - PosSystem sent a receipt incorrectly (e.g., non-late-signing receipt during late-signing mode)
  Those all represent a failure scenario where the PosSystem is making a mistake.  
  Because of this an `ftState` `0x0000_0000_EEEE_EEEE` can never be allowed as a trigger for Scenario 2 as the PosSystem has no way of knowing if something went wrong in the Queue or if it made a mistake (e.g. sending a not late-signing receipt while in late-signing mode).  
  Are there scenarios where we return an `ftState` `0x0000_0000_EEEE_EEEE` that should actually trigger a failure scenario instead?
- Should we allow normal receipts during late-signing mode? It complicates things but it also allows the PosSystem to continue operating without needing fully resolve the late-signing mode.
- Do we need to have different ftStates for late-signing mode for handwritten receipts and late-signing mode for late-signing receipts or is it enough to just have one late-signing mode ftState? With just one flag for late-signing mode, the PosSystem would need to keep track of what kind of mode it is in.
- Or do we allow late-signing mode to mix both late-signing receipts and handwritten receipts? In that case we can't really differentiate between how we handle two in the market Queue.

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
