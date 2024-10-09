using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2;

public static class ftQueueItemExtensions
{
    public static bool IsReceiptRequestFinished(this ftQueueItem item) => item.ftDoneMoment != null && !string.IsNullOrWhiteSpace(item.response) && !string.IsNullOrWhiteSpace(item.responseHash);

    public static bool IsContentOfQueueItemEqualWithGivenRequest(this ftQueueItem item, ReceiptRequest data)
    {
        var itemRequest = System.Text.Json.JsonSerializer.Deserialize<ReceiptRequest>(item.request);
        if(itemRequest == null)
        {
            return false;
        }
        if (itemRequest.cbChargeItems.Count == data.cbChargeItems.Count && itemRequest.cbPayItems.Count == data.cbPayItems.Count)
        {
            for (var i = 0; i < itemRequest.cbChargeItems.Count; i++)
            {
                if (itemRequest.cbChargeItems[i].Amount != data.cbChargeItems[i].Amount)
                {
                    return false;
                }
                if (itemRequest.cbChargeItems[i].ftChargeItemCase != data.cbChargeItems[i].ftChargeItemCase)
                {
                    return false;
                }
                if (itemRequest.cbChargeItems[i].Moment != data.cbChargeItems[i].Moment)
                {
                    return false;
                }
            }
            for (var i = 0; i < itemRequest.cbPayItems.Count; i++)
            {
                if (itemRequest.cbPayItems[i].Amount != data.cbPayItems[i].Amount)
                {
                    return false;
                }
                if (itemRequest.cbPayItems[i].ftPayItemCase != data.cbPayItems[i].ftPayItemCase)
                {
                    return false;
                }
                if (itemRequest.cbPayItems[i].Moment != data.cbPayItems[i].Moment)
                {
                    return false;
                }
            }
        }
        else
        {
            return false;
        }
        return true;
    }
}