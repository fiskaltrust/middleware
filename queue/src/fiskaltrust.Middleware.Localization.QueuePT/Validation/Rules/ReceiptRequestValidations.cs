using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public class ReceiptRequestValidations
{
    /// <summary>
    /// Validates that the sum of charge items matches the sum of pay items.
    /// Returns a single ValidationResult if balance validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptBalance(ReceiptRequest request)
    {
        if(request.ftReceiptCase.IsCase((ReceiptCase) 0x0007) || request.ftReceiptCase.IsCase((ReceiptCase) 0x0006))
        {
            // Delivery notes are not required to be balanced
            yield break;
        }

        var chargeItemsSum = 0m;
        var payItemsSum = 0m;

        if (request.cbChargeItems != null && request.cbChargeItems.Count > 0)
        {
            chargeItemsSum = request.cbChargeItems.Sum(ci => ci.Amount);
        }

        if (request.cbPayItems != null && request.cbPayItems.Count > 0)
        {
            payItemsSum = request.cbPayItems.Sum(pi => pi.Amount);
        }

        const decimal roundingTolerance = 0.01m;
        var difference = Math.Abs(chargeItemsSum - payItemsSum);

        if (difference > roundingTolerance)
        {
            var rule = PortugalValidationRules.ReceiptNotBalanced;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_ReceiptNotBalanced(chargeItemsSum, payItemsSum, difference),
                rule.Code
            )
            .WithContext("ChargeItemsSum", chargeItemsSum)
            .WithContext("PayItemsSum", payItemsSum)
            .WithContext("Difference", difference));
        }
    }

    /// <summary>
    /// Validates that OtherService charge items do not exceed 100€ net amount.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateOtherServiceNetAmountLimit(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var otherServiceNetAmount = request.cbChargeItems
            .Where(chargeItem => chargeItem.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.OtherService)
            .Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount());

        if (otherServiceNetAmount > 100m)
        {
            var rule = PortugalValidationRules.OtherServiceNetAmountExceedsLimit;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_OtherServiceNetAmountExceedsLimit,
                rule.Code,
                rule.Field
            ).WithContext("OtherServiceNetAmount", otherServiceNetAmount)
             .WithContext("Limit", 100m));
        }
    }

    /// <summary>
    /// Validates that refunds have a cbPreviousReceiptReference.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateRefundHasPreviousReference(ReceiptRequest request)
    {
        if (request.cbPreviousReceiptReference is null)
        {
            var rule = PortugalValidationRules.RefundMissingPreviousReceiptReference;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_RefundMissingPreviousReceiptReference,
                rule.Code,
                rule.Field
            ));
        }
    }

    /// <summary>
    /// Validates that receipt moment is not more than 10 minutes different from the current server time,
    /// and that it is never in the future.
    /// This ensures receipts are created with accurate timestamps (except for handwritten receipts which may be backdated).
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(ReceiptRequest request, object series, bool isHandwritten)
    {
        var serverTime = DateTime.UtcNow;
        var receiptMomentUtc = request.cbReceiptMoment.ToUniversalTime();

        // Check if receipt moment is in the future - this is always invalid
        if (receiptMomentUtc > serverTime)
        {
            var rule = PortugalValidationRules.CbReceiptMomentInFuture;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CbReceiptMomentInFuture(request.cbReceiptMoment, serverTime),
                rule.Code,
                rule.Field
            )
            .WithContext("ServerTime", serverTime)
            .WithContext("CbReceiptMoment", request.cbReceiptMoment));

            // If receipt is in the future, don't check time deviation
            yield break;
        }

        // Skip time deviation validation for handwritten receipts which may be backdated
        if (isHandwritten)
        {
            yield break;
        }

        var timeDifference = Math.Abs((receiptMomentUtc - serverTime).TotalMinutes);

        const double maxAllowedDifferenceMinutes = 10.0;

        if (timeDifference > maxAllowedDifferenceMinutes)
        {
            var rule = PortugalValidationRules.CbReceiptMomentDeviationExceeded;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CbReceiptMomentDeviationExceeded(request.cbReceiptMoment, serverTime, timeDifference),
                rule.Code,
                rule.Field
            )
            .WithContext("ServerTime", serverTime)
            .WithContext("CbReceiptMoment", request.cbReceiptMoment)
            .WithContext("DifferenceInMinutes", timeDifference)
            .WithContext("MaxAllowedDifferenceMinutes", maxAllowedDifferenceMinutes));
        }
    }

    /// <summary>
    /// Validates that the time difference between cbReceiptMoment and ftReceiptMoment does not exceed 2 minutes.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptMomentTimeDifference(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        if(request.cbReceiptMoment.Kind != DateTimeKind.Utc)
        {
           var rule = PortugalValidationRules.CbReceiptMomentNotUtc;
           yield return ValidationResult.Failed(new ValidationError(
                "cbReceiptMoment must be in UTC format for time difference validation.",
                rule.Code,
                rule.Field
            ));
        }

        if(request.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            // Skip validation for handwritten receipts which may have backdated ftReceiptMoment
            yield break;
        }


        var timeDifference = (receiptResponse.ftReceiptMoment - request.cbReceiptMoment).Duration();
        
        if (timeDifference > TimeSpan.FromMinutes(1))
        {
            var rule = PortugalValidationRules.ReceiptMomentTimeDifferenceExceeded;
            yield return ValidationResult.Failed(new ValidationError(
                $"The time difference between cbReceiptMoment ({request.cbReceiptMoment:yyyy-MM-dd HH:mm:ss}) and ftReceiptMoment ({receiptResponse.ftReceiptMoment:yyyy-MM-dd HH:mm:ss}) is {timeDifference.TotalMinutes:F2} minutes, which exceeds the maximum allowed difference of 2 minutes.",
                rule.Code,
                rule.Field
            ));
        }
    }

    /// <summary>
    /// Validates that item positions, if provided, start at 1 and are strictly increasing without gaps.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidatePositions(ReceiptRequest request)
    {
        foreach (var result in ValidatePositionsCore(request.cbChargeItems, "ChargeItems", PortugalValidationFields.ChargeItems))
        {
            yield return result;
        }

        foreach (var result in ValidatePositionsCore(request.cbPayItems, "PayItems", PortugalValidationFields.PayItems))
        {
            yield return result;
        }
    }

    private static IEnumerable<ValidationResult> ValidatePositionsCore<T>(IList<T>? items, string itemType, string fieldName) where T : class
    {
        if (items is null || items.Count == 0)
        {
            yield break;
        }

        var positions = items.Select(GetPosition).ToList();
        if (!positions.Any(p => p > 0))
        {
            yield break;
        }

        var expected = 1L;
        foreach (var position in positions)
        {
            if (position != expected)
            {
                var rule = PortugalValidationRules.InvalidPositions;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_InvalidPositions(itemType),
                    rule.Code,
                    fieldName
                )
                .WithContext("ExpectedPosition", expected)
                .WithContext("ActualPosition", position));
                yield break;
            }
            expected++;
        }
    }

    private static decimal GetPosition<T>(T item) where T : class
    {
        return item switch
        {
            ChargeItem chargeItem => chargeItem.Position,
            PayItem payItem => payItem.Position,
            _ => 0
        };
    }

    /// <summary>
    /// Validates that a handwritten receipt with the same series and number has not already been linked.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static async IAsyncEnumerable<ValidationResult> ValidateHandwrittenReceiptSeriesNumberNotAlreadyLinkedAsync(
        ReceiptRequest request,
        AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        if (!request.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            yield break;
        }

        if (!request.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data) ||
            data?.PT?.Series is null ||
            !data.PT.Number.HasValue)
        {
            yield break;
        }

        var series = data.PT.Series;
        var number = data.PT.Number.Value;

        var queueItemRepository = await readOnlyQueueItemRepository.Value;
        var foundDuplicate = false;

        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request) || string.IsNullOrEmpty(queueItem.response))
            {
                continue;
            }

            if (CheckForDuplicateHandwrittenReceipt(queueItem, series, number))
            {
                foundDuplicate = true;
                break;
            }
        }

        if (foundDuplicate)
        {
            var rule = PortugalValidationRules.HandwrittenReceiptSeriesNumberAlreadyLinked;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_HandwrittenReceiptSeriesNumberAlreadyLinked(series, number),
                rule.Code,
                rule.Field
            )
            .WithContext("Series", series)
            .WithContext("Number", number));
        }
    }

    private static bool CheckForDuplicateHandwrittenReceipt(storage.V0.ftQueueItem queueItem, string series, long number)
    {
        try
        {
            var existingRequest = System.Text.Json.JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
            var existingResponse = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);

            if (existingRequest == null || existingResponse == null)
            {
                return false;
            }

            if (!existingResponse.ftState.IsState(State.Success))
            {
                return false;
            }

            if (!existingRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
            {
                return false;
            }

            if (!existingRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var existingData) ||
                existingData?.PT?.Series is null ||
                !existingData.PT.Number.HasValue)
            {
                return false;
            }

            return existingData.PT.Series == series && existingData.PT.Number.Value == number;
        }
        catch
        {
            return false;
        }
    }
}
