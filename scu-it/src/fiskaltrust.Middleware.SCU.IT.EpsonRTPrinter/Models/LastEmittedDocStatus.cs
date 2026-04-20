using System;
using System.Globalization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;

public class LastEmittedDocStatus
{
    public long TotalDocAmountCents { get; set; }
    public long TotalVatAmountCents { get; set; }
    public DateTime DocumentDateTime { get; set; }
    public long ZNumber { get; set; }
    public long DocNumber { get; set; }
    public string? PrinterSN { get; set; }
    public bool IsFiscalDocument { get; set; }

    // Response includes OP (2 bytes) at the start, then fields per Communication Protocol v8.1, pag. 144
    // OP:                2 bytes [0-1]
    // TOTAL DOC AMOUNT:  9 bytes [2-10]
    // TOTAL VAT AMOUNT:  9 bytes [11-19]
    // DATE (DDMMYY):     6 bytes [20-25]
    // TIME (HHMMSS):     6 bytes [26-31]
    // Z NUM:             4 bytes [32-35]
    // DOC NUM:           4 bytes [36-39]
    // PRINTER SN:       11 bytes [40-50]
    // INST LOTT:         1 byte  [51]
    // LOTT CODE:         8 bytes [52-59]
    // FISC/NON FISC:     1 byte  [60]
    // UUID:             40 bytes [61-100]  (E-Receipt only)
    // PDF NAME:         60 bytes [101-160] (E-Receipt only)
    // SPARE:            32 bytes [161-192]
    public static LastEmittedDocStatus? Parse(string? responseData)
    {
        if (string.IsNullOrEmpty(responseData) || responseData.Length < 61)
        {
            return null;
        }

        try
        {
            var docDate = DateTime.ParseExact(responseData.Substring(20, 6) + responseData.Substring(26, 6), "ddMMyyHHmmss", CultureInfo.InvariantCulture);

            return new LastEmittedDocStatus
            {
                TotalDocAmountCents = long.TryParse(responseData.Substring(2, 9).Trim(), out var amount) ? amount : 0,
                TotalVatAmountCents = long.TryParse(responseData.Substring(11, 9).Trim(), out var vat) ? vat : 0,
                DocumentDateTime = docDate,
                ZNumber = long.TryParse(responseData.Substring(32, 4).Trim(), out var zNum) ? zNum : 0,
                DocNumber = long.TryParse(responseData.Substring(36, 4).Trim(), out var docNum) ? docNum : 0,
                PrinterSN = responseData.Substring(40, 11).Trim(),
                IsFiscalDocument = responseData[60] == '1'
            };
        }
        catch
        {
            return null;
        }
    }
}
