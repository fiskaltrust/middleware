using System;
using System.Text;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts
{
    public interface ICryptoHelper
    {
        string GenerateBase64Hash(string content);

        string GenerateJwsBase64Hash(string content);

        string GenerateBase64ChainHash(string receiptHash, ftReceiptJournal receiptJournal, ftQueueItem queueItem);

        string CreateJwsToken(string payload, string privateKeyBase64, byte[] encryptionKey);
    }
}