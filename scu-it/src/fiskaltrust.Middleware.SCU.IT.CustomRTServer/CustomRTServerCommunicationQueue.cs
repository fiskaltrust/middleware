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

public class CustomRTServerCommunicationQueue : IDisposable
{
    private readonly Dictionary<string, List<CommercialDocument>> _receiptQueue = new Dictionary<string, List<CommercialDocument>>();
    private readonly Guid _id;
    private readonly CustomRTServerClient _client;
    private readonly ILogger<CustomRTServerCommunicationQueue> _logger;
    private readonly CustomRTServerConfiguration _customRTServerConfiguration;

    private string _scuCacheFolder;
    private string _documentsPath;

    private bool _requestCancellation = false;
    private bool _processingReceipts = false;

    public CustomRTServerCommunicationQueue(Guid id, CustomRTServerClient client, ILogger<CustomRTServerCommunicationQueue> logger, CustomRTServerConfiguration customRTServerConfiguration)
    {
        _id = id;
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

    private const string DocumentSuffix = "_commercialdocument.json";
    private const string LotteryDocumentSuffix = "_commercialdocumentlottery.json";
    private const string DocumentSearchPattern = "*_commercialdocument*.json";

    private static bool IsLotteryFile(string path) => path.EndsWith(LotteryDocumentSuffix, StringComparison.OrdinalIgnoreCase);

    private Task SendDocumentAsync(string cashuuid, CommercialDocument commercialDocument, bool isLottery)
        => isLottery
            ? _client.InsertFiscalDocumentLotteryAsync(cashuuid, commercialDocument)
            : _client.InsertFiscalDocumentAsync(cashuuid, commercialDocument);

    public async Task EnqueueDocument(string cashuuid, CommercialDocument commercialDocument, long zRepNumber, long docNumber, bool isLottery = false)
    {
        if (_customRTServerConfiguration.SendReceiptsSync)
        {
            await SendDocumentAsync(cashuuid, commercialDocument, isLottery);
        }
        else
        {
            if (!Directory.Exists(Path.Combine(_documentsPath, cashuuid)))
            {
                Directory.CreateDirectory(Path.Combine(_documentsPath, cashuuid));
            }
            var suffix = isLottery ? LotteryDocumentSuffix : DocumentSuffix;
            File.WriteAllText(Path.Combine(_documentsPath, cashuuid, $"{DateTime.UtcNow.Ticks}__{zRepNumber.ToString().PadLeft(4, '0')}-{docNumber.ToString().PadLeft(4, '0')}{suffix}"), JsonConvert.SerializeObject(commercialDocument));
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
                    foreach (var document in Directory.GetFiles(directory, DocumentSearchPattern).OrderBy(x => x))
                    {
                        if (_requestCancellation)
                        {
                            _processingReceipts = false;
                            return;
                        }

                        await SendDocumentAsync(cashuuid, JsonConvert.DeserializeObject<CommercialDocument>(File.ReadAllText(document)), IsLotteryFile(document));
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
            count += Directory.GetFiles(directory, DocumentSearchPattern).Length;
        }
        return count;
    }

    public long GetCountOfDocumentsForInCache(string cashuuid)
    {
        if (!Directory.Exists(Path.Combine(_documentsPath, cashuuid)))
        {
            return 0;
        }
        return Directory.GetFiles(Path.Combine(_documentsPath, cashuuid), DocumentSearchPattern)?.Length ?? 0;
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

            foreach (var document in Directory.GetFiles(Path.Combine(_documentsPath, cashuuid), DocumentSearchPattern).OrderBy(x => x))
            {
                await SendDocumentAsync(cashuuid, JsonConvert.DeserializeObject<CommercialDocument>(File.ReadAllText(document)), IsLotteryFile(document));
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

    public void Dispose()
    {
        _requestCancellation = true;
        _logger.LogInformation("Stopping to process receipts in background for scu {scuid}. {amountofdocuments} documents left.", _id, GetCountOfDocumentsForInCache());
    }
}