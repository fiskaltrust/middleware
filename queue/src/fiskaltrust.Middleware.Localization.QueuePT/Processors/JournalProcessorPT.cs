﻿using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class JournalProcessorPT : IJournalProcessor
{
    private readonly IStorageProvider _storageProvider;

    public JournalProcessorPT(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public (ContentType contentType, IAsyncEnumerable<byte[]> result) ProcessAsync(JournalRequest request)
        => (new ContentType(MediaTypeNames.Application.Xml) { CharSet = Encoding.UTF8.WebName }, ProcessSAFTAsync(request));

    public async IAsyncEnumerable<byte[]> ProcessSAFTAsync(JournalRequest request)
    {
        var masterData = new AccountMasterData
        {
            AccountId = Guid.NewGuid(),
            AccountName = "FISKALTRUST CONSULTING GMBH - SUCURSAL EM",
            Street = "AV DA REPUBLICA N 35 4 ANDAR",
            Zip = "1050-189",
            City = "Lisboa",
            Country = "PT",
            TaxId = "980833310"
        };

        List<ftQueueItem> queueItems;
        if (request.From > 0)
        {
            queueItems = (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetEntriesOnOrAfterTimeStampAsync(request.From).ToBlockingEnumerable().ToList();
        }
        else
        {
            queueItems = (await (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetAsync()).ToList();
        }
        var data = SAFTMapping.SerializeAuditFile(masterData, queueItems, (int) request.To);
        yield return Encoding.UTF8.GetBytes(data);
    }
}
