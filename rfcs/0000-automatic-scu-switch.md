- Feature Name: `automatic_scu_switch`
- Start Date: 2026-07-17
- RFC PR: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/pull/0000)
- Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000)
- Markets: `DE` + `v2`

# Summary

In this RFC we introduce the possibility to configure an SCU switch to be performed automatically by the queue during daily closing.

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

The current SCU switch process is designed that way because for hardware TSEs you need to physically switch the TSE devices out in the middle of the process.

When switching to a cloud TSE, that manual step is not needed, which means that in some cases we can automatically perform the SCU switch.  
With the process this RFC describes, we'll be able to make it possible to automatically switch to supported SCUs during daily closing.

The automatic SCU switch is generally possible if the target SCU is a cloud TSE.

This automation enables us to:
- Perform SCU switches where the only user interaction happens in the portal
- Automatically switch queues over to a new working SCU if a cloud TSE outage leads to deleted TSEs

# Guide-level explanation

When an automatic SCU switch is configured, the middleware detects this when it's performing a daily closing.

The middleware then implicitly performs the following during processing of the daily closing:
1. A daily-closing receipt
2. A GetTseInfo call to the target SCU
3. An init-SCU-switch receipt (with the force flag behaviour)
4. A finish-SCU-switch receipt

The signatures of the daily closing response will contain all signatures done by the daily-closing, init-SCU-switch and finish-SCU-switch receipts.

After that the target SCU is used by the queue.

## Error handling

Each of those steps is recoverable, and after daily closing, the queue is either connected to the source SCU if something went wrong or to the target SCU if everything was successful.

> ***Meta:** We'll further define later how the error messages and returned signature items look.*

### daily-closing fails

If the daily closing fails because the source SCU is not reachable, the daily closing will be processed as a failed receipt, but the automatic SCU switch process will continue.

### GetTseInfo to target SCU fails

If the GetTseInfo call to the target SCU fails, the switch process is not performed, an error will be logged, and an error message will be returned in the receipt signatures.

The daily closing is just processed like a normal daily closing.

### init-SCU-switch fails

If the init-SCU-switch receipt fails (e.g. because the source SCU is not reachable), an error will be logged, but the source SCU will be disconnected from the queue and the switch process will continue.

This is the same behaviour as the init-SCU-switch receipt with a force flag has.

### finish-SCU-switch fails

If the finish-SCU-switch receipt fails (e.g. because the target SCU is not reachable), an error will be logged and the queue will reset the SCU switch process by performing a void-init-SCU-switch receipt.

The void-init-SCU-switch receipt will reconnect the source SCU to the queue.

## Configuration

### Queue -> SCU connection dialog

When opening the Queue -> SCU connection dialog and configuring the SCU switch, the portal detects if the SCU switch could be performed automatically and gives the user the option to select the automatic SCU switch.

If the user opts for the automatic SCU switch, it will be performed automatically on the next daily closing after the cashbox has been rebuilt and the queue has received the new configuration.

If the user does not opt for the automatic SCU switch, the current behaviour (manual SCU switch has to be performed) is kept.

### TSE exchange workflow

The TSE exchange workflow in the portal also detects if the SCU switch can be performed automatically and, if so, configures the cashbox for the automatic SCU switch and informs the user that no further action is needed.

If the SCU switch cannot be performed automatically, the current behaviour is used for the TSE exchange workflow.

If a TSE outage deleted the source TSE of some users this workflow can be automatically used to switch the queue to a new TSE.

> ***Meta:** I'm not exactly sure how the TSE exchange workflow currently works so that section might need corrections*  
> *\- @volllly*

# Reference-level explanation

## Configuration

The SCU switch is configured through the cashbox configuration.  
We'll take advantage of the fact that we know if the SCU switch can be performed automatically when configuring the SCU switch to provide that information to the queue through the cashbox configuration.

The source SCU and target SCU are written into the `init_ftSignaturCreationUnitDE` key in the queue configuration.

The `"Mode"` parameter of each SCU indicates if it's a source SCU (`0x10000`) or a target SCU (`0x20000`).
Each SCU also contains a `"ModeConfigurationJson"` key where we can add additional information (currently this contains only the respective other SCU ID).

We add an optional `"PerformSwitchOnDailyClosing"` parameter to the `"ModeConfigurationJson"` of both source and target SCUs, which is false by default.

<details>
<summary>

**Example Cashbox Configuration:**

</summary>

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
            "Mode": 131072,
            "ModeConfigurationJson": "{\"SourceScuId\":\"b100aee6-8c39-45fe-94d5-8c169dcc6e1e\",\"PerformSwitchOnDailyClosing\":true}"
          },
          {
            "ftSignaturCreationUnitDEId": "b100aee6-8c39-45fe-94d5-8c169dcc6e1e",
            "Url": "[\"grpc://localhost:18007\"]",
            "TimeStamp": 639184995866783260,
            "TseInfoJson": null,
            "Mode": 65536,
            "ModeConfigurationJson": "{\"TargetScuId\":\"80de521c-407f-4f97-bbe6-057acbb5fa40\",\"PerformSwitchOnDailyClosing\":true}"
          }
        ],
      }
    }
  ]
}
```

</details>

## Trigger of the automatic SCU switch

After (successful or unsuccessful) processing of the daily-closing receipt and before returning the ReceiptResponse, the queue checks if the `ModeConfigurationJson` of the current SCU has `PerformSwitchOnDailyClosing` set to `true`,
if it has a `TargetScuId` configured, and if the matching target SCU's `ModeConfigurationJson` also has `PerformSwitchOnDailyClosing` set to `true`.

If all of that is true the automatic SCU switch process is started.

## Process

> ***Meta:** I need to further investigate and decide if we want to:*
> - *reuse the existing Receipt Commands for the init- and finish-SCU-switch receipts*
> - *or if we should refactor them so that we can reuse the logic*
> - *or if we can simplify everything and put it directly into the daily closing command*
> *Probably the last option is the best. Because we do everything at the same time we can maybe simplify it quite a bit.*

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

It also packs quite a lot of things that can fail into one receipt.

# Rationale and alternatives

## Implement an all-in-one-SCU-switch receipt

Instead of configuring it to happen automatically and doing it automatically during daily closing, we could also create a new receipt case that does both init- and finish-SCU-switch in one go.

This has the advantage of being explicit but the disadvantage of requiring a specific receipt to be sent.

# Unresolved questions

- Do we need another parameter in the `ModeConfigurationJson` to specify that the automatic init-SCU-switch should be done with the force flag?
- If something fails, should the queue void the init-SCU-switch and return to the source SCU, or just force-connect itself to the target SCU even if the target SCU will not work?
