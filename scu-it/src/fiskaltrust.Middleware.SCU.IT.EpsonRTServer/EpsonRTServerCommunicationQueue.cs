using System;
using System.Collections.Generic;
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
        private readonly bool _diskCacheAvailable;

        private bool _requestCancellation;
        private bool _processingReceipts;
        private readonly Dictionary<string, int> _rejectionCounts = new();

        public EpsonRTServerCommunicationQueue(Guid id, IEpsonRTServerClient client, ILogger<EpsonRTServerCommunicationQueue> logger, EpsonRTServerConfiguration configuration)
        {
            _id = id;
            _client = client;
            _logger = logger;
            _configuration = configuration;

            // The durable location is only ever the explicitly configured folder. There is deliberately no
            // fallback (e.g. to SpecialFolder.Personal): a stateless/cloud host that never configures a
            // folder must never buffer fiscal documents to ephemeral (or unexpectedly cloud-synced) disk.
            var cacheFolder = configuration.ServiceFolder;
            _documentsPath = Path.Combine(string.IsNullOrEmpty(cacheFolder) ? "." : cacheFolder!, "epsonrtservercache", id.ToString());
            if (!string.IsNullOrEmpty(configuration.CacheDirectory))
            {
                _documentsPath = configuration.CacheDirectory!;
            }

            _diskCacheAvailable = !string.IsNullOrEmpty(configuration.CacheDirectory) || !string.IsNullOrEmpty(configuration.ServiceFolder);
            if (!_diskCacheAvailable)
            {
                // No persistent folder configured: the on-disk offline buffer cannot survive a restart, so
                // documents are always sent synchronously and the background drain is not started.
                if (!_configuration.SendReceiptsSync)
                {
                    _logger.LogWarning("SendReceiptsSync=false requested but no persistent ServiceFolder/CacheDirectory is configured; enforcing synchronous signing to avoid losing fiscal documents on restart.");
                }
                return;
            }

            if (!Directory.Exists(_documentsPath))
            {
                Directory.CreateDirectory(_documentsPath);
            }
            _ = Task.Run(ProcessReceiptsInBackground);
        }

        /// <summary>
        /// Sends the document synchronously (returning the server response so the caller can react to
        /// rejections, e.g. blockchain errors) or caches it on disk for the background queue (returns null).
        /// </summary>
        public async Task<Models.RtServerResponse?> EnqueueDocument(string tillId, string createReceiptXml, long zRepNumber, long docNumber)
        {
            if (_configuration.SendReceiptsSync || !_diskCacheAvailable)
            {
                return await _client.CreateReceiptAsync(createReceiptXml).ConfigureAwait(false);
            }

            var tillFolder = Path.Combine(_documentsPath, tillId);
            if (!Directory.Exists(tillFolder))
            {
                Directory.CreateDirectory(tillFolder);
            }
            var fileName = $"{DateTime.UtcNow.Ticks}__{zRepNumber:D4}-{docNumber:D4}{DocumentSuffix}";
            File.WriteAllText(Path.Combine(tillFolder, fileName), createReceiptXml);
            return null;
        }

        public async Task ProcessReceiptsInBackground()
        {
            if (!_diskCacheAvailable)
            {
                // Belt-and-suspenders: there is no disk cache to drain, and _documentsPath was never created,
                // so never touch it even if this ever gets invoked despite the guard in ProcessAllReceipts.
                _processingReceipts = false;
                return;
            }

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
                            if (!await SendCachedDocumentAsync(document).ConfigureAwait(false))
                            {
                                // Rejected but not yet parked: stop this till's sequence to preserve the
                                // blockchain order; retried on the next cycle.
                                break;
                            }
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
            if (!_diskCacheAvailable)
            {
                // No disk cache means nothing to drain; skip so the `finally` below never restarts the
                // background drain against a cache folder that was never created (it would loop forever
                // throwing DirectoryNotFoundException every ~2s).
                return;
            }

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
                    if (!await SendCachedDocumentAsync(document).ConfigureAwait(false))
                    {
                        throw new EpsonRTServerCommunicationException($"The RT Server rejected the cached document '{Path.GetFileName(document)}'; daily closing cannot proceed until the cache is drained.", -1);
                    }
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

        /// <summary>
        /// Sends one cached document. Returns true when the file was consumed (accepted by the server, or
        /// parked in the "failed" subfolder after too many rejections), false when it was rejected and should
        /// be retried later. Network exceptions propagate to the caller (the device is simply unreachable).
        /// </summary>
        private async Task<bool> SendCachedDocumentAsync(string documentPath)
        {
            Models.RtServerResponse? response = null;
            var rejected = false;
            try
            {
                response = await _client.CreateReceiptAsync(File.ReadAllText(documentPath)).ConfigureAwait(false);
                // "Accepted with error in log" codes are NOT rejections: the document is fiscally registered,
                // so it must be consumed (not parked). Only genuine "Receipt not accepted" / unknown negatives
                // count as rejected.
                rejected = response != null && !response.Success && response.CodeAsInt < 0
                    && !EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(response.CodeAsInt);
                if (response != null && !rejected && response.CodeAsInt != 0)
                {
                    _logger.LogWarning("Cached document {document} was accepted by the RT Server with warning code {code} ({description}).",
                        Path.GetFileName(documentPath), response.Code, EpsonRTServerErrorCodes.Describe(response.CodeAsInt));
                }
            }
            catch (EpsonRTServerCommunicationException ex)
            {
                rejected = !EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(ex.ResponseCode);
                if (rejected)
                {
                    _logger.LogError(ex, "The RT Server rejected the cached document {document}.", Path.GetFileName(documentPath));
                }
                else
                {
                    _logger.LogWarning(ex, "Cached document {document} was accepted by the RT Server with warning code {code} ({description}).",
                        Path.GetFileName(documentPath), ex.ResponseCode, EpsonRTServerErrorCodes.Describe(ex.ResponseCode));
                }
            }

            if (!rejected)
            {
                File.Delete(documentPath);
                _rejectionCounts.Remove(documentPath);
                return true;
            }

            _rejectionCounts[documentPath] = (_rejectionCounts.TryGetValue(documentPath, out var count) ? count : 0) + 1;
            _logger.LogWarning("Cached document {document} was rejected by the RT Server (code {code}, attempt {attempt}/{max}).",
                Path.GetFileName(documentPath), response?.Code ?? "n/a", _rejectionCounts[documentPath], _configuration.MaxDocumentSendRetries);

            if (_rejectionCounts[documentPath] >= _configuration.MaxDocumentSendRetries)
            {
                // Poison document: park it so it stops blocking the queue. It stays on disk for manual analysis.
                var failedFolder = Path.Combine(Path.GetDirectoryName(documentPath)!, "failed");
                if (!Directory.Exists(failedFolder))
                {
                    Directory.CreateDirectory(failedFolder);
                }
                File.Move(documentPath, Path.Combine(failedFolder, Path.GetFileName(documentPath)));
                _rejectionCounts.Remove(documentPath);
                _logger.LogError("Cached document {document} was rejected {max} times and has been moved to '{failedFolder}'. It will NOT be transmitted automatically.",
                    Path.GetFileName(documentPath), _configuration.MaxDocumentSendRetries, failedFolder);
                return true;
            }
            return false;
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
