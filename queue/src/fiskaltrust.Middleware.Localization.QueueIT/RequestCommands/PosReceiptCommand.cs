using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.ifPOS.v1.it;
using Newtonsoft.Json;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class PosReceiptCommand : RequestCommandIT
    {
        public PosReceiptCommand(IServiceProvider services) { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IITSSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIt)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, CountryBaseState);

            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                Barcode = "0123456789",
                DisplayText = "Message on customer display",
                Items = request.cbChargeItems?.Select(p => new Item
                {
                    Description = p.Description,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice ?? 0,
                    VatGroup = p.GetVatRate()
                }).ToList(),
                PaymentAdjustments = new List<PaymentAdjustment>()
                {
                    new PaymentAdjustment()
                    {
                        Description = "Discount applied to the subtotal",
                        Amount = -300.12m
                    }
                },
                Payments = new List<Payment>()
                {
                    new Payment(){ Description = "Payment in cash", Amount = 0, PaymentType = PaymentType.Cash, Index = 1}
                }

            };
            await client.FiscalReceiptInvoiceAsync(fiscalReceiptRequest).ConfigureAwait(false);

            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
            };
        }
    }
}
