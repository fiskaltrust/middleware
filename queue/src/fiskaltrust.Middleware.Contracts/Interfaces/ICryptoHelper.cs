using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface ICryptoHelper
    {
        string GenerateBase64Hash(string content);

        string GenerateBase64ChainHash(string receiptHash, ftReceiptJournal receiptJournal, ftQueueItem queueItem);

        (string hashBase64, string jwsData) CreateJwsToken(string payload, string privateKeyBase64, byte[] encryptionKey);
    }
}