﻿using System.Runtime.Serialization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;

public interface IPTSSCD
{
    public Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string invoiceNo, string? lastHash);

    public Task<PTSSCDInfo> GetInfoAsync();
}


public class ProcessRequest
{
    public required ReceiptRequest ReceiptRequest { get; set; }

    public required ReceiptResponse ReceiptResponse { get; set; }
}

public class ProcessResponse
{
    public required ReceiptResponse ReceiptResponse { get; set; }
}