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
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class PosReceiptCommand : RequestCommandIT
    {
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        public PosReceiptCommand(IServiceProvider services)
        {
            _signatureItemFactoryIT = services.GetRequiredService<SignatureItemFactoryIT>();
        }
        public override async Task<RequestCommandResponse> ExecuteAsync(IITSSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIt)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, CountryBaseState);

            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                //TODO Barcode = "0123456789" 
                //TODO DisplayText = "Message on customer display",
                Items = request.cbChargeItems?.Select(p => new Item
                {
                    Description = p.Description,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice ?? 0,
                    VatGroup = p.GetVatRate()
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.cbPayItems?.Select(p => new Payment
                {
                    Amount= p.Amount,
                    Description = p.Description,
                    PaymentType = p.GetPaymentType()                  
                }).ToList()            
            };
            var response = await client.FiscalReceiptInvoiceAsync(fiscalReceiptRequest).ConfigureAwait(false);
            receiptResponse.ftSignatures = _signatureItemFactoryIT.CreatePosReceiptSignatures(response);

            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
            };
        }
    }
}
