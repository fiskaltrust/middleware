using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.ifPOS.v1.it;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Exceptions;
using System.ServiceModel.Channels;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{

    public struct RefundDetails
    {
        public string Serialnumber { get; set; }
        public long ZRepNumber { get; set; }
        public long ReceiptNumber { get; set; }
        public DateTime ReceiptDateTime { get; set; }
    }

    public class PosReceiptCommand : RequestCommand
    {
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly IJournalITRepository _journalITRepository;
        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IITSSCD _client;

        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public PosReceiptCommand(IITSSCDProvider itIsscdProvider, SignatureItemFactoryIT signatureItemFactoryIT, IJournalITRepository journalITRepository, IReadOnlyConfigurationRepository configurationRepository)
        {
            _client = itIsscdProvider.Instance;
            _signatureItemFactoryIT = signatureItemFactoryIT;
            _journalITRepository = journalITRepository;
            _configurationRepository = configurationRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var journals = await _journalITRepository.GetAsync().ConfigureAwait(false);
            if (journals.Where(x=> x.cbReceiptReference.Equals(request.cbReceiptReference)).Any())
            {
                throw new CbReferenceExistsException( request.cbReceiptReference);
            }

            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId);

            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, CountryBaseState);
            FiscalReceiptResponse response;
            if (request.IsVoid())
            {
                var fiscalReceiptRefund = await CreateRefundAsync(request).ConfigureAwait(false);
                response = await _client.FiscalReceiptRefundAsync(fiscalReceiptRefund).ConfigureAwait(false);
            }
            else
            {
                var fiscalReceiptinvoice = CreateInvoice(request);
                response = await _client.FiscalReceiptInvoiceAsync(fiscalReceiptinvoice).ConfigureAwait(false);
            }
            if (!response.Success)
            {
                throw new Exception(response.ErrorInfo);
            }
            receiptResponse.ftSignatures = _signatureItemFactoryIT.CreatePosReceiptSignatures(response);

            var journalIT = CreateJournalIT(queue, queueIt, request, queueItem, response);
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);


            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
            };
        }

        private ftJournalIT CreateJournalIT(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ftQueueItem queueItem, FiscalReceiptResponse response) =>
            new ftJournalIT
            {
                ftJournalITId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                cbReceiptReference = request.cbReceiptReference,
                ftSignaturCreationUnitITId = queueIt.ftSignaturCreationUnitITId.Value,
                JournalType = request.ftReceiptCase & 0xFFFF,
                ReceiptDateTime = response.ReceiptDateTime,
                ReceiptNumber = response.ReceiptNumber,
                ZRepNumber = response.ZRepNumber,
                DataJson = response.ReceiptDataJson,
                TimeStamp = DateTime.UtcNow.Ticks
            };

        private static FiscalReceiptInvoice CreateInvoice(ReceiptRequest request)
        {
            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                //Barcode = ChargeItem.ProductBarcode,
                //TODO DisplayText = "Message on customer display",
                Items = request.cbChargeItems.Where(x => !x.IsPaymentAdjustment()).Select(p => new Item
                {
                    Description = p.Description,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice ?? p.Amount / p.Quantity,
                    Amount = p.Amount,
                    VatGroup = p.GetVatGroup()
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.cbPayItems?.Select(p => new Payment
                {
                    Amount = p.Amount,
                    Description = p.Description,
                    PaymentType = p.GetPaymentType()
                }).ToList()
            };
            return fiscalReceiptRequest;
        }

        private async Task<FiscalReceiptRefund> CreateRefundAsync(ReceiptRequest request)
        {
            var refundDetails = await GetRefundDetailsAsync(request).ConfigureAwait(false);
            var fiscalReceiptRequest = new FiscalReceiptRefund()
            {
                //TODO Barcode = "0123456789" 
                DisplayText = $"REFUND {refundDetails.ReceiptNumber:D4} {refundDetails.ZRepNumber:D4} {refundDetails.ReceiptDateTime:ddMMyyyy} {refundDetails.Serialnumber}",
                Refunds = request.cbChargeItems?.Select(p => new Refund
                {
                    Description = p.Description,
                    Quantity = Math.Abs(p.Quantity),
                    UnitPrice = p.UnitPrice ?? 0,
                    Amount = Math.Abs(p.Amount),
                    VatGroup = p.GetVatGroup(),
                    OperationType = p.GetRefundOperationType()
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.cbPayItems?.Select(p => new Payment
                {
                    Amount = p.Amount,
                    Description = p.Description,
                    PaymentType = p.GetPaymentType()
                }).ToList()
            };

            return fiscalReceiptRequest;
        }

        private async Task<RefundDetails> GetRefundDetailsAsync(ReceiptRequest request)
        {
            var journalIt = await _journalITRepository.GetAsync().ConfigureAwait(false);
            var receipt = journalIt.Where(x => x.cbReceiptReference.Equals(request.cbPreviousReceiptReference)).FirstOrDefault() ?? throw new RefundException($"Receipt {request.cbPreviousReceiptReference} was not found!");
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(receipt.ftSignaturCreationUnitITId).ConfigureAwait(false);
            var deviceInfo =  JsonConvert.DeserializeObject<DeviceInfo>(scu.InfoJson);
            return new RefundDetails()
            {
                ReceiptNumber = receipt.ReceiptNumber,
                ZRepNumber = receipt.ZRepNumber,
                ReceiptDateTime = receipt.ReceiptDateTime,
                Serialnumber = deviceInfo.SerialNumber
            };
        }
    }
}
