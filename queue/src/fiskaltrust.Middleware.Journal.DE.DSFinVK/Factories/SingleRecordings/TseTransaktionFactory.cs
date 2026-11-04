using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings
{
    public class TseTransaktionFactory
    {
        private readonly DSFinVKParameters _parameters;

        public TseTransaktionFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public IEnumerable<TseTransaktion> Create(ReceiptEntry receiptEntry, long zNumber, DateTime dailyClosingMoment)
        {
            var request = receiptEntry.ReceiptRequest;
            var response = receiptEntry.ReceiptResponse;

            if (response.ftSignatures == null || !response.ftSignatures.Any())
            {
                yield break;
            }

            var signatureBlocks = SplitSignatureItemsFromMultipleTransactions(request, response);

            signatureBlocks = signatureBlocks.Where(x => x.Any(si => si.ftSignatureType == (long) SignatureTypesDE.TransactionNumber));
            foreach (var signatures in signatureBlocks)
            {
                yield return Create(signatures, response.ftReceiptIdentification, zNumber, dailyClosingMoment);
            }
        }

        private IEnumerable<List<SignaturItem>> SplitSignatureItemsFromMultipleTransactions(ReceiptRequest request, ReceiptResponse response)
        {
            // We're adding additional signatures to the response in some cases, e.g. when we're closing all open transactions during a daily-closing receipt.
            // These need to be split.
            IEnumerable<List<SignaturItem>> signatureBlocks;
            if (request.IsDailyClosingReceipt())
            {
                // Daily closing receipts can contain finish-transaction blocks (finish-payload, finish-sig, process-type) after the "regular" SignatureItems
                signatureBlocks = SplitSignatureBlocks(response.ftSignatures, SignatureTypesDE.PublicKey);
                if (signatureBlocks.Count() == 2)
                {
                    signatureBlocks = new List<List<SignaturItem>> { signatureBlocks.First() }
                        .Concat(SplitSignatureBlocks(signatureBlocks.Last(), SignatureTypesDE.ProcessType));
                }
            }
            else if (request.IsZeroReceipt())
            {
                // Zero receipts can contain failed finish-transaction blocks and failed start-transaction blocks after the "regular" SignatureItems
                signatureBlocks = SplitSignatureBlocks(response.ftSignatures, SignatureTypesDE.PublicKey);

                // Last block might contain start-transactions, which we ignore here. They'll be processed in the daily closing receipt.
                if (signatureBlocks.Last().Count(x => x.ftSignatureType == (long) SignatureTypesDE.ProcessType) > 1)
                {
                    var signatureBlockWithoutStarts = SplitSignatureBlocks(signatureBlocks.Last().ToArray(), SignatureTypesDE.PublicKey).First();
                    signatureBlocks = signatureBlocks.Take(signatureBlocks.Count() - 1).Concat(new List<List<SignaturItem>> { signatureBlockWithoutStarts });
                }
            }
            else
            {
                signatureBlocks = SplitSignatureBlocks(response.ftSignatures, SignatureTypesDE.PublicKey);
            }

            return signatureBlocks;
        }

        private TseTransaktion Create(IEnumerable<SignaturItem> signatures, string ftReceiptIdentification, long zNumber, DateTime dailyClosingMoment)
        {
            var publicKey = signatures.FirstOrDefault(x => x.ftSignatureType == (long) SignatureTypesDE.PublicKey)?.Data;
            var hasSignature = signatures.Any(x => x.ftSignatureType == (long) SignatureTypesDE.Signature);

            if (!hasSignature)
            {
                return new TseTransaktion
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    BON_ID = ftReceiptIdentification,
                    TSE_ID = TseIdProvider.GetIdByPublicKey(publicKey),
                    TSE_TANR = long.Parse(signatures.Single(x => x.ftSignatureType == (long) SignatureTypesDE.TransactionNumber).Data),
                    TSE_TA_FEHLER = signatures.First().Data
                };
            }

            return new TseTransaktion
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                BON_ID = ftReceiptIdentification,
                TSE_ID = TseIdProvider.GetIdByPublicKey(publicKey),
                TSE_TANR = long.Parse(signatures.Single(x => x.ftSignatureType == (long) SignatureTypesDE.TransactionNumber).Data),
                TSE_TA_START = DateTime.ParseExact(signatures.Single(x => x.ftSignatureType == (long) SignatureTypesDE.StartTime).Data, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                TSE_TA_ENDE = DateTime.ParseExact(signatures.Single(x => x.ftSignatureType == (long) SignatureTypesDE.LogTime).Data, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                TSE_TA_VORGANGSART = signatures.First(x => x.ftSignatureType == (long) SignatureTypesDE.ProcessType).Data,
                TSE_TA_SIGZ = long.Parse(signatures.Single(x => x.ftSignatureType == (long) SignatureTypesDE.SignatureCounter).Data),
                TSE_TA_SIG = signatures.Single(x => x.ftSignatureType == (long) SignatureTypesDE.Signature).Data,
                TSE_VORGANGSDATEN = signatures.SingleOrDefault(x => x.ftSignatureType == (long) SignatureTypesDE.ProcessData)?.Data ?? ""
            };
        }

        private IEnumerable<List<SignaturItem>> SplitSignatureBlocks(IEnumerable<SignaturItem> ftSignatures, SignatureTypesDE splitAt)
        {
            var result = new List<List<SignaturItem>> { new List<SignaturItem>() };
            foreach (var signature in ftSignatures)
            {
                result.Last().Add(signature);

                if (signature.ftSignatureType == (long) splitAt)
                {
                    result.Add(new List<SignaturItem>());
                }
            }

            return result.Where(x => x.Any());
        }
    }
}
