using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Queue.Helpers
{
    public class CryptoHelper : ICryptoHelper
    {
        public string CreateJwsToken(string payload, string privateKeyBase64, byte[] encryptionKey) => throw new NotImplementedException();

        public string GenerateBase64ChainHash(string previousReceiptHash, ftReceiptJournal receiptJournal, ftQueueItem queueItem)
        {
            using (var sha256 = SHA256.Create())
            {
                var input = new List<byte>();

                if (!string.IsNullOrWhiteSpace(previousReceiptHash))
                {
                    input.AddRange(Convert.FromBase64String(previousReceiptHash));
                }
                input.AddRange(receiptJournal.ftReceiptJournalId.ToByteArray());
                input.AddRange(BitConverter.GetBytes(receiptJournal.ftReceiptMoment.Ticks));
                input.AddRange(BitConverter.GetBytes(receiptJournal.ftReceiptNumber));
                input.AddRange(Convert.FromBase64String(queueItem.requestHash));
                input.AddRange(Convert.FromBase64String(queueItem.responseHash));

                var hash = sha256.ComputeHash(input.ToArray());
                return Convert.ToBase64String(hash);
            }
        }

        public string GenerateBase64Hash(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return Convert.ToBase64String(hash);
            }
        }

        public string GenerateJwsBase64Hash(string content) => throw new NotImplementedException();
    }
}
