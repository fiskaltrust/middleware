using System;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions
{
    public static class ChargeItemCaseDataSubItemExtensions
    {
        public static decimal CalculateVAT(this ChargeItemCaseDataSubItem subItem, ILogger logger, decimal sentVatRate, DateTime moment) => TaxCalculator.CalculateVAT(subItem.Amount, subItem.ftSubChargeItemCase.GetVATRate(logger, sentVatRate, moment));
    }
}
