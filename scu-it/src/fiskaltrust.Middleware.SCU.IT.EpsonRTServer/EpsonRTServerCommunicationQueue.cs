using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    /// <summary>
    /// Caches createReceipt commands on disk and transmits them to the RT Server in blockchain order.
    /// Mirrors the Custom RT Server queue; note that the Epson RT Server validates the CCDC chain in sequence,
    /// so document ordering (encoded in the file name) must be preserved.
    /// </summary>
    public class EpsonRTServerCommunicationQueue : IDisposable
    {
        private const string DocumentSuffix = "_createreceipt.xml";
        private const string DocumentSearchPattern = "*_createreceipt.xml";

        private readonly Guid _id;
        private readonly IEpsonRTServerClient _client;
        private readonly ILogger<EpsonRTServerCommunicationQueue> _logger;
        private readonly EpsonRTServerConfiguration _configuration;
        private readonly string _documentsPath;

        private bool _requestCancellation;
        private bool _processingReceipts;

        public EpsonRTServerCommunicationQueue(Guid id, IEpsonRTServerClient client, ILogger<EpsonRTServerCommunicationQueue> logger, EpsonRTServerConfiguration configuration)
        {
            _id = id;
            _client = client;
            _logger = logger;
            _configuration = configuration;

            var cacheFolder = configuration.ServiceFolder;
            if (string.IsNullOrEmpty(cacheFolder))
            {
                cacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }

            _documentsPath = Path.Combine(cacheFolder!, "epsonrtservercache", id.ToString());
            if (!string.IsNullOrEmpty(configuration.CacheDirectory))
            {
                _documentsPath = configuration.CacheDirectory!;
            }
            if (!Directory.Exists(_documentsPath))
            {
                Directory.CreateDirectory(_documentsPath);
            }

            _ = Task.Run(ProcessReceiptsInBackground);
        }

        public async Task EnqueueDocument(string tillId, string createReceiptXml, long zRepNumber, long docNumber)
        {
            if (_configuration.SendReceiptsSync)
            {
                await _client.CreateReceiptAsync(createReceiptXml).ConfigureAwait(false);
                return;
            }

            var tillFolder = Path.Combine(_documentsPath, tillId);
            if (!Directory.Exists(tillFolder))
            {
                Directory.CreateDirectory(tillFolder);
            }
            var fileName = $"{DateTime.UtcNow.Ticks}__{zRepNumber:D4}-{docNumber:D4}{DocumentSuffix}";
            File.WriteAllText(Path.Combine(tillFolder, fileName), createReceiptXml);
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
                        foreach (var document in Directory.GetFiles(directory, DocumentSearchPattern).OrderBy(x => x))
                        {
                            if (_requestCancellation)
                            {
                                _processingReceipts = false;
                                return;
                            }
                            await _client.CreateReceiptAsync(File.ReadAllText(document)).ConfigureAwait(false);
                            File.Delete(document);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to transmit cached receipt to the Epson RT Server.");
                }
                finally
                {
                    await Task.Delay(2000).ConfigureAwait(false);
                }
            }
        }

        public async Task ProcessAllReceipts(string tillId)
        {
            _requestCancellation = true;
            while (_processingReceipts)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
            try
            {
                var tillFolder = Path.Combine(_documentsPath, tillId);
                if (!Directory.Exists(tillFolder))
                {
                    return;
                }
                foreach (var document in Directory.GetFiles(tillFolder, DocumentSearchPattern).OrderBy(x => x))
                {
                    await _client.CreateReceiptAsync(File.ReadAllText(document)).ConfigureAwait(false);
                    File.Delete(document);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transmit cached receipt to the Epson RT Server.");
                throw;
            }
            finally
            {
                _requestCancellation = false;
                _ = Task.Run(ProcessReceiptsInBackground);
            }
        }

        public long GetCountOfDocumentsForInCache()
        {
            if (!Directory.Exists(_documentsPath))
            {
                return 0;
            }
            return Directory.GetDirectories(_documentsPath).Sum(directory => Directory.GetFiles(directory, DocumentSearchPattern).Length);
        }

        public long GetCountOfDocumentsForInCache(string tillId)
        {
            var tillFolder = Path.Combine(_documentsPath, tillId);
            if (!Directory.Exists(tillFolder))
            {
                return 0;
            }
            return Directory.GetFiles(tillFolder, DocumentSearchPattern).Length;
        }

        public void Dispose()
        {
            _requestCancellation = true;
            _logger.LogInformation("Stopping to process receipts in background for scu {scuid}. {amountofdocuments} documents left.", _id, GetCountOfDocumentsForInCache());
        }
    }
}
