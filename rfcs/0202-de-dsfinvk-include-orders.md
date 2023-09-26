- Feature Name: `de_dsfinvk_include_orders`
- Start Date: 2023-08-24
- RFC PR: [fiskaltrust/middleware#202](https://github.com/fiskaltrust/middleware/pull/202)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->

# Summary

Including Orders and not revenue related transactions to the Dsfinvk.

# Motivation

A letter from the Financial Authorities Berlin prompted us to include orders to the Dsfinvk-Export

# Guide-level explanation

For the PosCreator they are no implementation changes needed, the new Export will include orders.

## New local queues

New Queues don't need to take anything into account.
They will work out of the box.

## Updating local queues

The new export will be included in the latest middleware version and will be available in the Fiskaltrust portal.


# Reference-level explanation

The changes will take place in the Fiskaltrust module 'fiskaltrust.Exports.DSFinVK'. There, orders and other not revenue related transactions will be added to the export.

## Initialization

No initial process needed. The changes will come into account with the new version of the export.

# Drawbacks

With the current implementation it is not possible to log single start- or update-transaction on Fiskaltrust explicit flow.

# Rationale and alternatives

## Option 1: Include single start- and update- Transactions of Fiskaltrust explicit flow

We will include start- update-transactions without finish transaction to the Dsfinvk-export. We will check if the given data validates against Amadeus Verify.

## Option 2:  Exclude single start- and update- Transactions of Fiskaltrust explicit flow

In the first step we excluded start- and update-transaction of the Fiskaltrust explicit flow.

### Open questions

If we can include start- or update- transactions without a finish transaction when changing the middleware according.




