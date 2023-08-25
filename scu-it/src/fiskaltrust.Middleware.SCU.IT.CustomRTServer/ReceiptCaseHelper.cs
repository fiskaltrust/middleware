﻿using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public static class ReceiptCaseHelper
{
    public static bool IsLegacyReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0xF000) == 0x0000;

    public static long GetReceiptCase(this ReceiptRequest request) => request.ftReceiptCase & 0x0000_0000_0000_FFFF;

    public static bool IsDailyClosing(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.DailyClosing0x2011;

    public static bool IsZeroReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.ZeroReceipt0x200;

    public static bool IsOutOfOperationReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.OutOfOperationReceipt0x4002;

    public static bool IsInitialOperationReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x0000_0000_0000_FFFF) == (long) ITReceiptCases.InitialOperationReceipt0x4001;

    public static bool IsUsedFailed(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) > 0x0000;

    public static bool IsLateSigning(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) > 0x0000;

    public static bool IsTraining(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0002_0000) > 0x0000;

    public static bool IsVoid(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0x0000;

    public static bool IsHandwritten(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0008_0000) > 0x0000;

    public static bool IsSmallBusinessReceipt(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0010_0000) > 0x0000;

    public static bool IsReceiverBusiness(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0020_0000) > 0x0000;

    public static bool IsReceiverKnown(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0040_0000) > 0x0000;

    public static bool IsSalesInForeignCountry(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0080_0000) > 0x0000;

    public static bool IsRefund(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0100_0000) > 0x0000;

    public static bool IsReceiptRequest(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_8000_0000) > 0x0000;
}