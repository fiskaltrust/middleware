using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.Helpers
{
    public static class RequestValidation
    {
        public static IEnumerable<ValidationError> ValidateQueueState(ReceiptRequest request, ftQueue queue, ftQueueFR queueFR)
        {
            if (!queue.StartMoment.HasValue && request.ftReceiptCase != 0x4652000000000010)
            {                
                yield return new ValidationError { Message = $"Queue {queueFR.ftQueueFRId} is out of order, it has not been activated!" };
            }

            if (queue.StartMoment.HasValue && queue.StopMoment.HasValue)
            {
                yield return new ValidationError { Message = $"Queue {queueFR.ftQueueFRId} is out of order, it is permanent de-activated!" };
            }
        }

        public static IEnumerable<ValidationError> ValidateReceiptItems(ReceiptRequest request)
        {
            var chargeItemSum = 0m;
            var payItemSum = 0m;

            if (request.cbChargeItems != null)
            {
                chargeItemSum = request.cbChargeItems.Sum(ci => ci.Amount);
                var wrongChargeItems = request.cbChargeItems.Where(ci => (ci.ftChargeItemCase >> 48) != 0x4652);
                if (wrongChargeItems.Count() > 0)
                {
                    yield return new ValidationError { Message = $"The charge item cases [0x{string.Join(", ", wrongChargeItems.Select(x => x.ftChargeItemCase.ToString("X")))}] do not match the expected country id 0x4652XXXXXXXXXXXX" };
                }
            }
            if (request.cbPayItems != null)
            {
                payItemSum = request.cbPayItems.Sum(ci => ci.Amount);
                var wrongPayItems = request.cbPayItems.Where(ci => (ci.ftPayItemCase >> 48) != 0x4652);
                if (wrongPayItems.Count() > 0)
                {
                    yield return new ValidationError { Message = $"The pay item case [0x{string.Join(", ", wrongPayItems.Select(x => x.ftPayItemCase.ToString("X")))}] does not match the expected country id 0x4652XXXXXXXXXXXX" };
                }
            }

            if (chargeItemSum != payItemSum)
            {
                yield return new ValidationError { Message = $"The sum of the amounts of the charge items ({chargeItemSum}) is not equal to the sum of the amounts of the pay items ({payItemSum})" };
            }
        }
    }
}
