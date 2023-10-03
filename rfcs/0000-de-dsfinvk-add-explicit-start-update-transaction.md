- Feature Name: `de_dsfinvk_explicit_start_update_transaction`
- Start Date: 2023-10
- RFC PR: [fiskaltrust/middleware#000]()
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->

# Summary

Including Start- and Update-Transaction of Explicit-Flow to the DSFinvk.

# Motivation

A letter from the Financial Authorities Berlin prompted us to include orders to the DSFinvk-Export, which is done. But Start- and Update-Transaction of Explicit-Flow are missing.
In Explicit-Flow they are handled with different Bon-Id´s as the Finish-Transaction. The Finish-Transaction is logged as it includes all the necessary data.

# Guide-level explanation

For the PosCreator they are no implementation changes needed, the new Export will include explicit Start- and Update-Transaction.

## New local queues

New Queues don't need to take anything into account.
They will work out of the box.

## Updating local queues

The new export will be included in the latest middleware version and will be available in the Fiskaltrust portal.


# Reference-level explanation

The changes will take place in the Fiskaltrust module 'fiskaltrust.Exports.DSFinVK' and the 'fiskaltrust.Middleware.Localization.QueueDE'. In the German middleware missing signatures will be saved to the database. The export will include the missing transactions.

## Initialization

No initial process needed. The changes will come into account with the new version of the export.

# Drawbacks

As explicit transactions are split into multiple Bon-Ids it is not clear we will get valid data for the DSFinvk.

# Rationale and alternatives

## Option 1: Include single start- and update- Transactions of Fiskaltrust explicit flow

We will include start- update-transactions without finish transaction to the DSFinvk-export. We will check if the given data validates against Amadeus Verify.

## Option 2:  Exclude single start- and update- Transactions of Fiskaltrust Explicit-Flow

In the first step we excluded start- and update-transaction of the Fiskaltrust Explicit-Flow. If Options 1 does not succeed, we will leave it this way.

### Open questions

If we can include start- or update- transactions without a finish transaction when changing the middleware accordingly.




