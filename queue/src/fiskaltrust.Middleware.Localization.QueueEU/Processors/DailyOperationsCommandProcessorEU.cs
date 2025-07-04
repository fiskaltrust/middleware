﻿using fiskaltrust.Middleware.Localization.QueueEU.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Storage;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueEU.Processors;

public class DailyOperationsCommandProcessorEU : IDailyOperationsCommandProcessor
{
    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

}