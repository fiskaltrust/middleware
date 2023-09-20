using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;
#pragma warning disable

public class CustomRTServerCommunicationQueue
{
    private readonly Dictionary<string, List<CommercialDocument>> _receiptQueue = new Dictionary<string, List<CommercialDocument>>();
    private readonly CustomRTServerClient _client;
    private readonly ILogger<CustomRTServerCommunicationQueue> _logger;
    private readonly CustomRTServerConfiguration _customRTServerConfiguration;

    private string _scuCacheFolder;
    private string _documentsPath;

    private bool _requestCancellation = false;
    private bool _processingReceipts = false;

    public CustomRTServerCommunicationQueue(Guid id, CustomRTServerClient client, ILogger<CustomRTServerCommunicationQueue> logger, CustomRTServerConfiguration customRTServerConfiguration)
    {
        _client = client;
        _logger = logger;
        _customRTServerConfiguration = customRTServerConfiguration;

        if (!string.IsNullOrEmpty(customRTServerConfiguration.ServiceFolder))
        {
            _scuCacheFolder = customRTServerConfiguration.ServiceFolder!;
        }
        if (string.IsNullOrEmpty(_scuCacheFolder))
        {
            _scuCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }

        _documentsPath = Path.Combine(_scuCacheFolder, "customrtservercache", id.ToString());
        if (!string.IsNullOrEmpty(customRTServerConfiguration.CacheDirectory))
        {
            _documentsPath = customRTServerConfiguration.CacheDirectory;
        }
        if (!Directory.Exists(_documentsPath))
        {
            Directory.CreateDirectory(_documentsPath);
        }

        _ = Task.Run(ProcessReceiptsInBackground);
    }

    public async Task EnqueueDocument(string cashuuid, CommercialDocument commercialDocument, long zRepNumber, long docNumber)
    {
        if (_customRTServerConfiguration.SendReceiptsSync)
        {
            await _client.InsertFiscalDocumentAsync(cashuuid, commercialDocument);

        }
        else
        {
            if (!Directory.Exists(Path.Combine(_documentsPath, cashuuid)))
            {
                Directory.CreateDirectory(Path.Combine(_documentsPath, cashuuid));
            }
            File.WriteAllText(Path.Combine(_documentsPath, cashuuid, $"{DateTime.UtcNow.Ticks}__{zRepNumber.ToString().PadLeft(4, '0')}-{docNumber.ToString().PadLeft(4, '0')}_commercialdocument.json"), JsonConvert.SerializeObject(commercialDocument));
        }
    }


    public async Task ProcessReceiptsInBackground()
    {
        while (true)
        {
            _processingReceipts = true;
            if (_requestCancellation)
            {
                _processingReceipts = false;
                return;
            }

            try
            {
                foreach (var directory in Directory.GetDirectories(_documentsPath))
                {
                    var cashuuid = Path.GetFileName(directory);
                    foreach (var document in Directory.GetFiles(directory, "*_commercialdocument.json").OrderBy(x => x))
                    {
                        if (_requestCancellation)
                        {
                            _processingReceipts = false;
                            return;
                        }

                        var fileName = Path.GetFileNameWithoutExtension(document);
                        await _client.InsertFiscalDocumentAsync(cashuuid, JsonConvert.DeserializeObject<CommercialDocument>(File.ReadAllText(document)));
                        File.Delete(document);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed tro transmit receipt...");
            }
            finally
            {
                await Task.Delay(2000);
            }
        }
    }

    public long GetCountOfDocumentsForInCache()
    {
        var count = 0;
        foreach (var directory in Directory.GetDirectories(_documentsPath))
        {
            count += Directory.GetFiles(directory, $"*_commercialdocument.json").Length;
        }
        return count;
    }

    public long GetCountOfDocumentsForInCache(string cashuuid)
    {
        if (!Directory.Exists(Path.Combine(_documentsPath, cashuuid)))
        {
            return 0;
        }
        return Directory.GetFiles(Path.Combine(_documentsPath, cashuuid), $"*_commercialdocument.json").Length;
    }

    public async Task ProcessAllReceipts(string cashuuid)
    {
        _requestCancellation = true;
        while (_processingReceipts)
        {
            await Task.Delay(10);
        }
        try
        {
            if (!Directory.Exists(Path.Combine(_documentsPath, cashuuid)))
            {
                return;
            }

            foreach (var document in Directory.GetFiles(Path.Combine(_documentsPath, cashuuid), $"*_commercialdocument.json").OrderBy(x => x))
            {
                await _client.InsertFiscalDocumentAsync(cashuuid, JsonConvert.DeserializeObject<CommercialDocument>(File.ReadAllText(document)));
                File.Delete(document);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed tro transmit receipt...");
            throw;
        }
        finally
        {
            _requestCancellation = false;
            Task.Run(() => ProcessReceiptsInBackground());
        }
    }
}