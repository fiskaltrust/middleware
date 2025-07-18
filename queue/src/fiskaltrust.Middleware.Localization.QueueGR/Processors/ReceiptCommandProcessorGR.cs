﻿using fiskaltrust.ifPOS.v2;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class ReceiptCommandProcessorGR(IGRSSCD sscd, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            var receiptReferences = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, receiptReferences);
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
        else
        {
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            });
            return new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>());
        }
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request)
    {
        if (request.ReceiptRequest.cbPreviousReceiptReference != null)
        {
            var receiptReference = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, receiptReference);
            return new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>());
        }
        else
        {
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            });
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
    }

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }


    private async Task<List<(ReceiptRequest, ReceiptResponse)>> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        if (request.cbPreviousReceiptReference is null)
        {
            return new List<(ReceiptRequest, ReceiptResponse)>();
        }

        return await request.cbPreviousReceiptReference.MatchAsync(
            async single => [await LoadReceiptReferencesToResponse(request, receiptResponse, single)],
            async group =>
            {
                var references = new List<(ReceiptRequest, ReceiptResponse)>();
                foreach (var reference in group)
                {
                    var item = await LoadReceiptReferencesToResponse(request, receiptResponse, reference);
                    references.Add(item);
                }
                return references;
            }
        );

    }

#pragma warning disable
    private async Task<(ReceiptRequest, ReceiptResponse)> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse, string cbPreviousReceiptReferenceString)
    {
        var queueItems = (await _readOnlyQueueItemRepository.Value).GetByReceiptReferenceAsync(cbPreviousReceiptReferenceString, request.cbTerminalID);
        await foreach (var existingQueueItem in queueItems)
        {
            if (string.IsNullOrEmpty(existingQueueItem.response))
            {
                continue;
            }

            var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(existingQueueItem.request);
            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            if (referencedResponse != null && referencedRequest != null)
            {

                return (referencedRequest, referencedResponse);
            }
            else
            {
                throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{cbPreviousReceiptReferenceString}' sent via the request.");
            }
        }
        throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{cbPreviousReceiptReferenceString}' sent via the request.");
    }
}
