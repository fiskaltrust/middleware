﻿using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public static class ReceiptCaseHelper
{
    public static bool IsV2Receipt(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_F000_0000_0000) == 0x0000_2000_0000_0000;

    public static long GetReceiptCase(this ReceiptRequest request) => request.ftReceiptCase & 0x0000_0000_0000_FFFF;

    public static bool IsDailyClosing(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.DailyClosing0x2011;

    public static bool IsMonthlyClosing(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.MonthlyClosing0x2012;

    public static bool IsYearlyClosing(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.YearlyClosing0x2013;

    public static bool IsReprint(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.Reprint0x3010;

    public static bool IsZeroReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.ZeroReceipt0x2000;

    public static bool IsOutOfOperationReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.OutOfOperationReceipt0x4002;

    public static bool IsInitialOperationReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.InitialOperationReceipt0x4001;

    public static bool IsUsedFailed(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) > 0x0000;

    public static bool IsLateSigning(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) > 0x0000;

    public static bool IsGroupingRequest(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0800_0000) > 0x0000;

    public static bool IsTraining(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0002_0000) > 0x0000;

    public static bool IsVoid(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0x0000;

    public static bool IsXReportZeroReceipt(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0001_0000_0000) > 0x0000;

    public static bool IsVoid(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0000_0000_0001_0000) > 0x0000;

    public static bool IsVoid(this PayItem payItem) => (payItem.ftPayItemCase & 0x0000_0000_0001_0000) > 0x0000;

    public static bool IsHandwritten(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0008_0000) > 0x0000;

    public static bool IsSmallBusinessReceipt(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0010_0000) > 0x0000;

    public static bool IsReceiverBusiness(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0020_0000) > 0x0000;

    public static bool IsReceiverKnown(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0040_0000) > 0x0000;

    public static bool IsSalesInForeignCountry(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0080_0000) > 0x0000;

    public static bool IsRefund(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0100_0000) > 0x0000;

    public static bool IsRefund(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0000_0000_0002_0000) > 0x0000;

    public static bool IsTip(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0000_0000_0000_00F0) == 0x30;

    public static bool IsSingleUseVoucher(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0000_0000_0000_00F0) == 0x40 && !IsMultiUseVoucher(chargeItem);

    public static bool IsMultiUseVoucher(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0x0000_0000_0000_00FF) == 0x48;

    public static bool IsRefund(this PayItem payItem) => (payItem.ftPayItemCase & 0x0000_0000_0002_0000) > 0x0000;

    public static bool IsReceiptRequest(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_8000_0000) > 0x0000;
    public static Customer? GetCustomer(this ReceiptRequest receiptRequest) => GetValueOrNull<Customer>(receiptRequest?.cbCustomer);
    public static ReceiptCaseLotteryData? GetLotteryData(this ReceiptRequest receiptRequest) => GetValueOrNull<ReceiptCaseLotteryData>(receiptRequest?.ftReceiptCaseData);

    private static T? GetValueOrNull<T>(string? data) where T : class
    {
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(data!);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
