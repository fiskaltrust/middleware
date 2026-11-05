using System;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions
{
    public static class ChargeItemCaseExtensions
    {
        private static readonly DateTime _reducedTimeframeBegin = new DateTime(2020, 7, 1, 0, 0, 0);
        private static readonly DateTime _reducedTimeframeEnd = new DateTime(2021, 1, 1, 0, 0, 0);
        private static readonly DateTime _extendedReducedTimeframeEnd = new DateTime(2021, 7, 1, 0, 0, 0);

        public static int GetVATKey(this long chargeItemCase, ILogger logger, decimal sentVatRate, DateTime moment)
        {
            var key = (chargeItemCase & 0xFFFF) switch
            {
                0x0000 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0001 => DSFinVKConstants.VatKeys.General,
                0x0002 => DSFinVKConstants.VatKeys.Reduced,
                0x0003 => DSFinVKConstants.VatKeys.AverageRate,
                0x0004 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0005 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0006 => DSFinVKConstants.VatKeys.Excempt,
                0x0007 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0011 => DSFinVKConstants.VatKeys.General,
                0x0012 => DSFinVKConstants.VatKeys.Reduced,
                0x0013 => DSFinVKConstants.VatKeys.AverageRate,
                0x0014 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0015 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0016 => DSFinVKConstants.VatKeys.Excempt,
                0x0017 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0019 => DSFinVKConstants.VatKeys.General,
                0x001A => DSFinVKConstants.VatKeys.Reduced,
                0x001B => DSFinVKConstants.VatKeys.AverageRate,
                0x001C => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x001D => DSFinVKConstants.VatKeys.NotTaxable,
                0x001E => DSFinVKConstants.VatKeys.Excempt,
                0x001F => DSFinVKConstants.VatKeys.NotDetectable,
                0x0021 => DSFinVKConstants.VatKeys.General,
                0x0022 => DSFinVKConstants.VatKeys.Reduced,
                0x0023 => DSFinVKConstants.VatKeys.AverageRate,
                0x0024 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0025 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0026 => DSFinVKConstants.VatKeys.Excempt,
                0x0027 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0029 => DSFinVKConstants.VatKeys.General,
                0x002A => DSFinVKConstants.VatKeys.Reduced,
                0x002B => DSFinVKConstants.VatKeys.AverageRate,
                0x002C => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x002D => DSFinVKConstants.VatKeys.NotTaxable,
                0x002E => DSFinVKConstants.VatKeys.Excempt,
                0x002F => DSFinVKConstants.VatKeys.NotDetectable,
                0x0031 => DSFinVKConstants.VatKeys.General,
                0x0032 => DSFinVKConstants.VatKeys.Reduced,
                0x0033 => DSFinVKConstants.VatKeys.AverageRate,
                0x0034 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0035 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0036 => DSFinVKConstants.VatKeys.Excempt,
                0x0037 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0039 => DSFinVKConstants.VatKeys.General,
                0x003A => DSFinVKConstants.VatKeys.Reduced,
                0x003B => DSFinVKConstants.VatKeys.AverageRate,
                0x003C => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x003D => DSFinVKConstants.VatKeys.NotTaxable,
                0x003E => DSFinVKConstants.VatKeys.Excempt,
                0x003F => DSFinVKConstants.VatKeys.NotDetectable,
                0x0041 => DSFinVKConstants.VatKeys.General,
                0x0042 => DSFinVKConstants.VatKeys.Reduced,
                0x0043 => DSFinVKConstants.VatKeys.AverageRate,
                0x0044 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0045 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0046 => DSFinVKConstants.VatKeys.Excempt,
                0x0047 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0049 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0051 => DSFinVKConstants.VatKeys.General,
                0x0052 => DSFinVKConstants.VatKeys.Reduced,
                0x0053 => DSFinVKConstants.VatKeys.AverageRate,
                0x0054 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0055 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0056 => DSFinVKConstants.VatKeys.Excempt,
                0x0057 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0059 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0060 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0061 => DSFinVKConstants.VatKeys.General,
                0x0062 => DSFinVKConstants.VatKeys.Reduced,
                0x0063 => DSFinVKConstants.VatKeys.AverageRate,
                0x0064 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0065 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0066 => DSFinVKConstants.VatKeys.Excempt,
                0x0067 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0068 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0069 => DSFinVKConstants.VatKeys.General,
                0x006A => DSFinVKConstants.VatKeys.Reduced,
                0x006B => DSFinVKConstants.VatKeys.AverageRate,
                0x006C => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x006D => DSFinVKConstants.VatKeys.NotTaxable,
                0x006E => DSFinVKConstants.VatKeys.Excempt,
                0x006F => DSFinVKConstants.VatKeys.NotDetectable,
                0x0071 => DSFinVKConstants.VatKeys.General,
                0x0072 => DSFinVKConstants.VatKeys.Reduced,
                0x0073 => DSFinVKConstants.VatKeys.AverageRate,
                0x0074 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0075 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0076 => DSFinVKConstants.VatKeys.Excempt,
                0x0077 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0079 => DSFinVKConstants.VatKeys.General,
                0x007A => DSFinVKConstants.VatKeys.Reduced,
                0x007B => DSFinVKConstants.VatKeys.AverageRate,
                0x007C => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x007D => DSFinVKConstants.VatKeys.NotTaxable,
                0x007E => DSFinVKConstants.VatKeys.Excempt,
                0x007F => DSFinVKConstants.VatKeys.NotDetectable,
                0x0081 => DSFinVKConstants.VatKeys.General,
                0x0082 => DSFinVKConstants.VatKeys.Reduced,
                0x0083 => DSFinVKConstants.VatKeys.AverageRate,
                0x0084 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x0085 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0086 => DSFinVKConstants.VatKeys.Excempt,
                0x0087 => DSFinVKConstants.VatKeys.NotDetectable,
                0x0089 => DSFinVKConstants.VatKeys.General,
                0x008A => DSFinVKConstants.VatKeys.Reduced,
                0x008B => DSFinVKConstants.VatKeys.AverageRate,
                0x008C => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x008D => DSFinVKConstants.VatKeys.NotTaxable,
                0x008E => DSFinVKConstants.VatKeys.Excempt,
                0x008F => DSFinVKConstants.VatKeys.NotDetectable,
                0x0090 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0091 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0092 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0093 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0094 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0095 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0096 => DSFinVKConstants.VatKeys.NotTaxable,
                0x0097 => DSFinVKConstants.VatKeys.NotTaxable,
                0x00B1 => DSFinVKConstants.VatKeys.General,
                0x00B2 => DSFinVKConstants.VatKeys.Reduced,
                0x00B3 => DSFinVKConstants.VatKeys.AverageRate,
                0x00B4 => DSFinVKConstants.VatKeys.AverageRateReduced,
                0x00B5 => DSFinVKConstants.VatKeys.NotTaxable,
                0x00B6 => DSFinVKConstants.VatKeys.Excempt,
                0x00B7 => DSFinVKConstants.VatKeys.NotDetectable,
                0x00A1 => DSFinVKConstants.VatKeys.NotTaxable,
                0x00A2 => DSFinVKConstants.VatKeys.NotTaxable,
                _ => GetVatNotDetectable(logger, chargeItemCase)
            };

            if (key == DSFinVKConstants.VatKeys.NotDetectable)
            {
                key = ComputeUnknownVatKey(sentVatRate);
            }
            else if (key == DSFinVKConstants.VatKeys.General)
            {
                key = ComputeGeneralVatKey(sentVatRate, moment);
            }
            else if (key == DSFinVKConstants.VatKeys.Reduced)
            {
                key = ComputeReducedVatKey(sentVatRate, moment);
            }

            return key;
        }

        public static decimal GetVATRate(this long chargeItemCase, ILogger logger, decimal sentVatRate, DateTime moment)
        {
            var vatKey = chargeItemCase.GetVATKey(logger, sentVatRate, moment);

            // The rate of these keys is defined by the DSFinV-K, a deviating sent rate must not add a second rate for the same key to Stamm_USt.
            var fixedRate = GetFixedVatRate(vatKey, moment);
            if (fixedRate.HasValue)
            {
                return fixedRate.Value;
            }

            if (sentVatRate != 0.0M)
            {
                return sentVatRate;
            }

            return vatKey switch
            {
                DSFinVKConstants.VatKeys.General => 19.00m,
                DSFinVKConstants.VatKeys.Reduced => 7.00m,
                DSFinVKConstants.VatKeys.AverageRate => 10.70m,
                DSFinVKConstants.VatKeys.AverageRateReduced => 5.50m,
                DSFinVKConstants.VatKeys.NotTaxable => 0m,
                DSFinVKConstants.VatKeys.Excempt => 0m,
                DSFinVKConstants.VatKeys.NotDetectable => 0m,
                DSFinVKConstants.VatKeys.GeneralHistoric => 19.00m,
                DSFinVKConstants.VatKeys.ReducedHistoric => 7.00m,
                DSFinVKConstants.VatKeys.GeneralHistoricCovid => 16.00m,
                DSFinVKConstants.VatKeys.ReducedHistoricCovid => 5.00m,

                _ => throw new Exception($"VAT key not supported."),
            };
        }

        // General and reduced rate are only fixed outside of the lowered timeframe (07-12/2020), where 16% and 5% were the current rates.
        // Average rates (§ 24 UStG) changed over time, so the sent rate is kept for them.
        private static decimal? GetFixedVatRate(int vatKey, DateTime moment) => vatKey switch
        {
            DSFinVKConstants.VatKeys.General when !moment.Between(_reducedTimeframeBegin, _reducedTimeframeEnd) => 19.00m,
            DSFinVKConstants.VatKeys.Reduced when !moment.Between(_reducedTimeframeBegin, _reducedTimeframeEnd) => 7.00m,
            DSFinVKConstants.VatKeys.NotTaxable => 0m,
            DSFinVKConstants.VatKeys.Excempt => 0m,
            DSFinVKConstants.VatKeys.NotDetectable => 0m,
            _ => null
        };

        private static int ComputeGeneralVatKey(decimal sentVatRate, DateTime moment)
        {
            if (sentVatRate == 0.00m)
            {
                return DSFinVKConstants.VatKeys.General;
            }
            if (sentVatRate == 16.00m && moment >= _reducedTimeframeEnd)
            {
                // Reduced, but after deadline => historic general rate (reduced)
                return DSFinVKConstants.VatKeys.GeneralHistoricCovid;
            }
            if (sentVatRate == 7.00m && moment >= _extendedReducedTimeframeEnd)
            {
                // Hospitality/Food. Reduced, but after deadline => historic general rate (reduced)
                return DSFinVKConstants.VatKeys.GeneralHistoricCovid;
            }
            if (sentVatRate == 19.00m && moment.Between(_reducedTimeframeBegin, _reducedTimeframeEnd))
            {
                // Not reduced, but in lowered timeframe => historic general rate (normal)
                return DSFinVKConstants.VatKeys.GeneralHistoric;
            }

            return DSFinVKConstants.VatKeys.General;
        }

        private static int ComputeReducedVatKey(decimal? sentVatRate, DateTime moment)
        {
            if (!sentVatRate.HasValue || sentVatRate == 0.00m)
            {
                return DSFinVKConstants.VatKeys.Reduced;
            }
            if (sentVatRate == 5.00m && moment >= _reducedTimeframeEnd)
            {
                // Reduced, but after deadline => historic reduced rate (reduced)
                return DSFinVKConstants.VatKeys.ReducedHistoricCovid;
            }
            if (sentVatRate == 7.00m && moment.Between(_reducedTimeframeBegin, _reducedTimeframeEnd))
            {
                // Not reduced, but in lowered timeframe => historic reduced rate (normal)
                return DSFinVKConstants.VatKeys.ReducedHistoric;
            }

            return DSFinVKConstants.VatKeys.Reduced;
        }

        private static int ComputeUnknownVatKey(decimal sentVatRate) => sentVatRate != 0.0M ? Convert.ToInt32((sentVatRate * 100) + 1000) : DSFinVKConstants.VatKeys.NotDetectable;

        private static int GetVatNotDetectable(ILogger logger, long chargeItemCase)
        {
            logger.LogWarning($"ftChargeItemCase {chargeItemCase:x} is not supported. Default NotDetectable (0,00% - UmsatzsteuerNichtErmittelbar) used.");
            return DSFinVKConstants.VatKeys.NotDetectable;
        }
    }
}
