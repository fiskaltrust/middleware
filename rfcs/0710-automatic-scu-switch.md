- Feature Name: `automatic_scu_switch`
- Start Date: 2026-07-17
- RFC PR: [fiskaltrust/middleware#710](https://github.com/fiskaltrust/middleware/pull/710)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->
- Markets: `DE` + `v2`

# Summary

In this RFC we introduce the possibility to configure an SCU switch to be performed automatically by the queue during daily closing.
The init-SCU-switch and finish-SCU-switch receipts are then executed automatically during processing of the next daily closing receipt.

> ***Note:** This RFC describes the topic in terms of the German market, as this is currently the only market where such a process is needed.*  
> *For other markets, the same applies; just replace TSE with the SSCD of the market and replace the market code `DE` with the correct market code.*

# Motivation

<details>
<summary>

## Background

</summary>

The SCU switch currently is a five-step process:
1. Configure the SCU switch with source SCU and target SCU
2. Perform a daily-closing receipt
3. Perform the init-SCU-switch receipt
4. For hardware TSEs: Switch out the old TSE device for the new TSE device
5. Perform the finish-SCU-switch receipt

</details>

The current SCU switch process is designed that way because of the assumption that you need to physically switch the TSE devices out in the middle of the process.
This assumption turned out to be mostly irrelevant with the advent of cloud TSEs.

When switching to a cloud TSE, there is no physical device that needs to be switched out and that manual step is not needed anymore.
Because of this when switching to a cloud TSE we can automatically perform the SCU switch.

When switching to a cloud TSE we can already check if the target SCU is reachable before starting the process with the init-SCU-switch.
If the target SCU is not reachable we can abort the process before it starts.
(With the manual SCU switch process we have to first perform the init-SCU-switch before we can check if the target SCU is reachable.
If the switch is incorrectly configured or the target SCU is broken this sometimes leads to broken states that _have_ to be reset with a void init-SCU-switch receipt.
We can catch those cases before they occur with the automatic switch.)

The process this RFC describes makes it possible to automatically switch to cloud SCUs during daily closing.

This automation enables us to:
- Bundle the error-prone manual SCU switch process into a single receipt that automatically handles all errors
- Perform SCU switches where the only PosDealer interaction happens in the portal. No input by the PosOperator or PosSystem is needed.
- Allow our support to switch queues over to a new working SCU (e.g. if a cloud TSE outage leads to broken/deleted TSEs) without PosDealer interaction and without the need for the PosSystem to implement the SCU-switch process.

The automatic SCU switch reuses all of the logic and processes from the init-SCU-switch and finish-SCU-switch receipts.  
Because of this we don't need to re-evaluate the legal implications of the individual steps or downstream processes.
We know that the init-SCU-switch works and we know that the finish-SCU-switch works. That's why we can execute them automatically and bundle them into the daily closing receipt.
(In the manual process the daily-closing receipt also needs to happen immediately before the init-SCU-switch receipt is sent).


<details>
<summary>Aside on automatically switching hardware TSEs</summary>

Technically with this RFC it is also possible to switch from one hardware TSE to another hardware TSE automatically if both are plugged into different ports at the same time.

E.g. if the source SCU's TSE path is `D:/` and the target SCU's TSE path is `E:/` the switch could automatically be performed.

This would require both TSEs to be plugged in during the daily-closing,
would require careful configuration of the CashBox
and synchronization of the CashBox rebuild with the hardware setup.

For those reasons the manual switch is still the preferred method when switching to a hardware TSE.

</details>

# Guide-level explanation

When an automatic SCU switch is configured, the middleware detects this when it's performing a daily closing.

The middleware then automatically performs the following during processing of the daily closing:
1. The processing of a normal daily-closing receipt
2. A GetTseInfo call to the target SCU
3. An init-SCU-switch receipt
4. A finish-SCU-switch receipt

The signatures of the daily closing response will contain all signatures done by the daily-closing, init-SCU-switch and finish-SCU-switch receipts in that order.

This behaves like the zero receipt, which also performs multiple TSE transactions during processing (e.g. recovering failed start- and finish-transactions when closing the SSCD-fail mode) and appends all of their signatures to a single response:
The `ftReceiptIdentification` is derived from the daily-closing's own TSE transaction number, the signatures of the other steps are appended, and process-level status and error messages are reported as text signature items (`ftSignatureType` `0x4445000000000002`).

After the daily-closing receipt is processed the target SCU is used by the queue.

## Force behaviour

By default the automatic SCU switch will not be attempted if the source SCU is unreachable.
That way we ensure that no TAR-Files are lost and all steps of the SCU switch process are executed.

If it is known that the source SCU is broken the automatic SCU switch can be configured to be forced.
If configured so, the automatic SCU switch uses the force flag behaviour of the init-SCU-switch receipt.

That means that the daily-closing does not need to succeed for the forced automatic SCU switch process to start, which can lead to lost TAR-Files.

In case of a failed daily-closing the Queue will have entered failed-mode, which needs to be resolved with a zero receipt after the automatic SCU switch is performed. This mirrors the behaviour of the manual SCU switch. (If there are failed finish transactions from the source TSE in the queue database, the zero receipt will fail if it's sent without the remove-transactions-not-on-TSE flag (`0x2000_0000`).)

## Error handling

Each of those steps is recoverable, and after daily closing, the queue is either connected to the source SCU if something went wrong or to the target SCU if everything was successful.

> ***Meta:** We'll further define later how the error messages and returned signature items look.
> They should behave like the zero receipt: status and error messages are reported as text signature items (`ftSignatureType` `0x4445000000000002`) appended to the response.*

If the middleware is stopped mid-switch (e.g. by a crash or external outage) it can recover the process on the next daily-closing receipt. If the init-SCU-switch receipt has already been performed when the automatic SCU switch is done the process skips the init-SCU-switch receipt and directly continues the process at the finish-SCU-switch receipt.

### daily-closing fails

If the daily closing fails because the source SCU is not reachable, the daily closing will be processed as a failed receipt, and the automatic SCU switch process will not be initiated.

### GetTseInfo to target SCU fails

If the GetTseInfo call to the target SCU fails, the switch process is not performed, an error will be logged, and an error message will be returned in the receipt signatures.

The daily closing is processed like a normal daily closing.

### init-SCU-switch fails

If the init-SCU-switch receipt fails (e.g. because the source SCU is not reachable), an error will be logged, but the source SCU will be disconnected from the queue and the switch process will continue.
We have downloaded the TAR-File earlier during the daily closing so we're not losing any data in that case. Only the client is maybe not deregistered.

This uses the same behaviour the manual init-SCU-switch receipt has.

### finish-SCU-switch fails

If the finish-SCU-switch receipt fails (e.g. because the target SCU is not reachable), an error will be logged and the queue will reset the SCU switch process by performing a void init-SCU-switch receipt.

The void init-SCU-switch receipt will reconnect the queue back to the source SCU even if the source SCU was/is broken. This will leave the SCU switch process in a clean state (where it can be reconfigured and reattempted) but may result in the queue not being connected to a working SCU.

## Retry

If the automatic SCU switch does not finish successfully it will always be attempted again with the next daily-closing receipt.

The PosSystem will be notified of the failure in the receipt signatures.

## Configuration

### Queue -> SCU connection dialog

When opening the Queue -> SCU connection dialog and configuring the SCU switch, the portal detects if the SCU switch could be performed automatically and gives the user the option to select the automatic SCU switch.

If the user opts for the automatic SCU switch, it will be performed automatically on the next daily closing after the cashbox has been rebuilt and the queue has received the new configuration.

If the user does not opt for the automatic SCU switch, the current behaviour (manual SCU switch has to be performed) is kept.

### TSE exchange workflow

The TSE exchange workflow in the portal also detects if the SCU switch can be performed automatically and, if so, configures the cashbox for the automatic SCU switch and informs the user that no further action is needed.

If the SCU switch cannot be performed automatically, the current behaviour is used for the TSE exchange workflow.

If a TSE outage deleted the source TSE of some users, this workflow can be automatically used to switch the queue to a new TSE.

> ***Meta:** I'm not exactly sure how the TSE exchange workflow currently works so that section might need corrections.*  

# Reference-level explanation

## Configuration

The SCU switch is configured through the cashbox configuration.  
We'll take advantage of the fact that we know if the SCU switch can be performed automatically when configuring the SCU switch to provide that information to the queue through the cashbox configuration.

The source SCU and target SCU are written into the `init_ftSignaturCreationUnitDE` key in the queue configuration.

The `"Mode"` parameter of each SCU indicates if it's a source SCU (`0x1_0000`) or a target SCU (`0x2_0000`).
Each SCU also contains a `"ModeConfigurationJson"` key where we can add additional information (currently this contains only the respective other SCU ID. So the source SCU with mode `0x1_0000` contains the key `"TargetScuId"` pointing to the respective target SCU and vice versa.).

We add an optional `"PerformSwitchOnDailyClosing"` parameter to the `"ModeConfigurationJson"` of the target SCU, which is false by default.
We also add an optional `"ForceInitSwitchOnDailyClosing"` parameter to the `"ModeConfigurationJson"` of the source SCU, which is false by default.

<details>
<summary>Example Cashbox Configuration</summary>

```json
{
  "ftQueues": [
    {
      "Configuration": {
        "init_ftSignaturCreationUnitDE": [
          {
            "ftSignaturCreationUnitDEId": "80de521c-407f-4f97-bbe6-057acbb5fa40",
            "Url": "[\"grpc://localhost:1401\"]",
            "TimeStamp": 639177283121740740,
            "TseInfoJson": null,
            "Mode": 131072, // 0x2_0000 (target scu)
            "ModeConfigurationJson": "{\"SourceScuId\":\"b100aee6-8c39-45fe-94d5-8c169dcc6e1e\",\"PerformSwitchOnDailyClosing\":true}"
          },
          {
            "ftSignaturCreationUnitDEId": "b100aee6-8c39-45fe-94d5-8c169dcc6e1e",
            "Url": "[\"grpc://localhost:18007\"]",
            "TimeStamp": 639184995866783260,
            "TseInfoJson": null,
            "Mode": 65536, // 0x1_0000 (source scu)
            "ModeConfigurationJson": "{\"TargetScuId\":\"80de521c-407f-4f97-bbe6-057acbb5fa40\",\"ForceInitSwitchOnDailyClosing\":true}"
          }
        ]
      }
    }
  ]
}
```

</details>

## Trigger of the automatic SCU switch

After successful processing of the daily-closing receipt and before returning the ReceiptResponse, the queue checks if the `ModeConfigurationJson` of the current SCU has a `TargetScuId` configured, and if the matching target SCU's `ModeConfigurationJson` has the `PerformSwitchOnDailyClosing` parameter set to `true`.

If all of that is true the automatic SCU switch process is started.

If processing of the daily-closing receipt is unsuccessful because the SCU is out of order but the `ForceInitSwitchOnDailyClosing` parameter in the source SCU's `ModeConfigurationJson` is set to `true`, the SCU switch process will also be started but with the force init-SCU-switch flag behaviour.

## Process

> ***Meta:** I need to further investigate and decide if we want to:*
> - *reuse the existing Receipt Commands for the init- and finish-SCU-switch receipts*
> - *or if we should refactor them so that we can reuse the logic*
> - *or if we can simplify everything and put it directly into the daily closing command*
> *Probably the last option is the best. Because we do everything at the same time we can maybe simplify it quite a bit.*

## Error handling

## Retry

> ***Meta:** Mention that the daily closing will be executed if the SCU switch is detected in progress and the automatic SCU switch is configured so that the crash case can resolve itself.*


<!--
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
-->

# Drawbacks

If it's not communicated clearly (or not read by users), it could cause some confusion.  
Imagine configuring the SCU switch like you usually do and then trying to perform it:
1. You do a daily-closing
2. Unbeknownst to you, the SCU switch is performed
3. You do an init-SCU-switch receipt and get the message that it is not configured correctly.

(Mitigated by [improving the error message](#already-performed-scu-switch))

# Rationale and alternatives

## Implement an all-in-one-SCU-switch receipt

Instead of configuring it to happen automatically and doing it automatically during daily closing, we could also create a new receipt case that does both init- and finish-SCU-switch in one go.

This has the advantage of being explicit but the disadvantage of requiring a specific receipt to be sent.

# Unresolved questions

- ~Do we need another parameter in the `ModeConfigurationJson` to specify that the automatic init-SCU-switch should be done with the force flag?~ Yes. Skipping the TAR-File export from the source TSE that happens during the daily-closing is dangerous and should require explicit opt-in.
- ~If something fails, should the queue void the init-SCU-switch and return to the source SCU, or just force-connect itself to the target SCU even if the target SCU will not work?~ We should void the init-SCU-switch receipt as switching to an SCU that's not working has no benefit.
- ~Should we create an all-in-one-SCU-switch receipt instead?~ No.
- ~Should we require a receipt case flag to be set on the daily closing for the automatic switch to be triggered? (Maybe we could also reuse the update masterdata flag)~ No. Both this and the all-in-one-SCU-switch receipt idea would improve the current manual approach by packing the functionality into one receipt and thus allowing for a less error-prone switch process. But they still require implementation from the PosCreator and interaction from the PosSystem. With checking availability of the target SCU we have a safe success and failure path so performing the switch automatically is no risk. And with the explicit force configuration we allow automatic switching with broken source SCUs.
- Should we return an `ftState` `0x100` for all receipts if the automatic SCU switch is configured but not yet processed?

# Future possibilities

## TSE sanity check

The Queue/SCU should perform a sanity check to verify that the connected TSE is the correct TSE.

The Queue can e.g. verify that the certificate serial number returned by the TSE matches with what the queue has stored in the `ftSignaturCreationUnitDE` table.

## Already performed SCU switch

Improve the error message of the init-SCU-switch receipt in case the SCU switch has already been performed.

If the configured target SCU is already the current SCU we know that the SCU switch has already been performed. In that case we can return a helpful error message.
