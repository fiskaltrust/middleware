using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Derives the receipt-level <c>ftTypeOfService</c> from the type-of-service nibble of every
/// <c>ftChargeItemCase</c>: <c>B</c> when only goods were sold, <c>S</c> when only services,
/// <c>M</c> when both. Items that are neither (vouchers, tips, grants, receivables, cash transfers,
/// own consumption, unknown) do not influence the result, and a receipt made only of those - or
/// carrying no charge items at all - gets no value.
/// </summary>
public static class FRTypeOfServiceCalculator
{
    public const string Goods = "B";
    public const string Services = "S";
    public const string Mix = "M";

    public static string? From(ReceiptRequest request)
    {
        var chargeItems = request.cbChargeItems ?? new List<ChargeItem>();
        var hasGoods = chargeItems.Any(x => IsGoods(x.ftChargeItemCase));
        var hasServices = chargeItems.Any(x => IsService(x.ftChargeItemCase));

        return (hasGoods, hasServices) switch
        {
            (true, true) => Mix,
            (true, false) => Goods,
            (false, true) => Services,
            _ => null,
        };
    }

    /// <summary>Stamps the value onto the <c>FR</c> block of the response's <c>ftStateData</c>, keeping what is already there.</summary>
    public static void Apply(ReceiptRequest request, ReceiptResponse response)
    {
        var typeOfService = From(request);
        if (typeOfService is null)
        {
            return;
        }

        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.ftTypeOfService = typeOfService;
        response.ftStateData = stateData;
    }

    public static bool IsGoods(ChargeItemCase chargeItemCase) => chargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.Delivery;

    public static bool IsService(ChargeItemCase chargeItemCase) => chargeItemCase.TypeOfService() is ChargeItemCaseTypeOfService.OtherService or ChargeItemCaseTypeOfService.CatalogService;
}
