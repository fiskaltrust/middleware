using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public class TarFileExportService
    {
        public async Task<(string filePath, bool success, string checkSum, bool isErased)> ProcessTarFileExportAsync(ILogger logger, IDESSCD client, Guid queueId, string cashboxIdentification, bool erase, string serviceFolder, int tarFileChunkSize)
        {
            logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync [enter].");
            var exportSession = await client.StartExportSessionAsync(new StartExportSessionRequest
            {
                Erase = erase
            }).ConfigureAwait(false);

            var targetDirectory = Path.Combine(serviceFolder, "Exports", queueId.ToString(), "TAR");
            Directory.CreateDirectory(targetDirectory);
            var filePath = Path.Combine(targetDirectory, $"{DateTime.Now:yyyyMMddhhmmssfff}_{cashboxIdentification.RemoveInvalidFilenameChars()}.tar");

            var sha256CheckSum = string.Empty;
            logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync Section ExportDataAsync [enter].");
            using (var fileStream = File.Create(filePath))
            {
                ExportDataResponse export;
                var backoff = TimeSpan.FromMilliseconds(100);
                do
                {
                    export = await client.ExportDataAsync(new ExportDataRequest
                    {
                        TokenId = exportSession.TokenId,
                        MaxChunkSize = tarFileChunkSize
                    }).ConfigureAwait(false);
                    if (!export.TotalTarFileSizeAvailable)
                    {
                        await Task.Delay(backoff).ConfigureAwait(false);
                        backoff = TimeSpan.FromMilliseconds(Math.Min(backoff.TotalMilliseconds * 2, TimeSpan.FromSeconds(30).TotalMilliseconds));
                    }
                    else
                    {
                        logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync Section Convert.FromBase64String [enter].");
                        var chunk = Convert.FromBase64String(export.TarFileByteChunkBase64);
                        fileStream.Write(chunk, 0, chunk.Length);
                        logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync Section Convert.FromBase64String [exit].");
                    }
                } while (!export.TarFileEndOfFile);

                logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync Section Sha256ChecksumBase64 [enter].");
                using var sha256 = SHA256.Create();
                fileStream.Position = 0;
                sha256CheckSum = Convert.ToBase64String(sha256.ComputeHash(fileStream));
                logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync Section Sha256ChecksumBase64 [exit].");
            }
            logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync Section ExportDataAsync [exit].");

            var endSessionRequest = new EndExportSessionRequest
            {
                TokenId = exportSession.TokenId,
                Sha256ChecksumBase64 = sha256CheckSum,
                Erase = erase
            };
            var endExportSessionResult = await client.EndExportSessionAsync(endSessionRequest).ConfigureAwait(false);
            if (!endExportSessionResult.IsValid)
            {
                return (null, false, null, false);
            }

            logger.LogTrace("TarFileExportService.ProcessTarFileExportAsync [exit].");
            return (filePath, true, sha256CheckSum, endExportSessionResult.IsErased);
        }
    }
}
