﻿using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;

public interface IGRSSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

    public Task<GRSSCDInfo> GetInfoAsync();
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