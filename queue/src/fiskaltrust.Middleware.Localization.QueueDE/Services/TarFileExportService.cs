using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public class TarFileExportService
    {
        public async Task<(string filePath, bool success)> ProcessTarFileExportAsync(IDESSCD client, Guid queueId, string cashboxIdentification, bool erase, string serviceFolder, int tarFileChunkSize)
        {
            var exportSession = await client.StartExportSessionAsync(new StartExportSessionRequest
            {
                Erase = erase
            }).ConfigureAwait(false);

            var targetDirectory = Path.Combine(serviceFolder, "Exports", queueId.ToString(), "TAR");
            Directory.CreateDirectory(targetDirectory);
            var filePath = Path.Combine(targetDirectory, $"{DateTime.Now:yyyyMMddhhmmssfff}_{cashboxIdentification.RemoveInvalidFilenameChars()}.tar");

            using (var fileStream = File.Create(filePath))
            {
                ExportDataResponse export;
                do
                {
                    export = await client.ExportDataAsync(new ExportDataRequest
                    {
                        TokenId = exportSession.TokenId,
                        MaxChunkSize = tarFileChunkSize
                    }).ConfigureAwait(false);
                    if (!export.TotalTarFileSizeAvailable)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                    }
                    else
                    {
                        var chunk = Convert.FromBase64String(export.TarFileByteChunkBase64);
                        fileStream.Write(chunk, 0, chunk.Length);
                    }
                } while (!export.TarFileEndOfFile);
            }

            using var sha256 = SHA256.Create();
            var sha256CheckSum = Convert.ToBase64String(sha256.ComputeHash(File.ReadAllBytes(filePath)));
            var endSessionRequest = new EndExportSessionRequest
            {
                TokenId = exportSession.TokenId,
                Sha256ChecksumBase64 = sha256CheckSum,
                Erase = erase
            };
            var endExportSessionResult = await client.EndExportSessionAsync(endSessionRequest).ConfigureAwait(false);
            if (!endExportSessionResult.IsValid)
            {
                return (null, false);
            }
            return (filePath, true);
        }
    }
}
