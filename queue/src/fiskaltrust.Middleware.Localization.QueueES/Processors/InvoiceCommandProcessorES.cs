﻿using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class InvoiceCommandProcessorES : IInvoiceCommandProcessor
{
    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
        switch (receiptCase)
        {
            case (int) ReceiptCases.InvoiceUnknown0x1000:
                return await InvoiceUnknown0x1000Async(request);
            case (int) ReceiptCases.InvoiceB2C0x1001:
                return await InvoiceB2C0x1001Async(request);
            case (int) ReceiptCases.InvoiceB2B0x1002:
                return await InvoiceB2B0x1002Async(request);
            case (int) ReceiptCases.InvoiceB2G0x1003:
                return await InvoiceB2G0x1003Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase(request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}