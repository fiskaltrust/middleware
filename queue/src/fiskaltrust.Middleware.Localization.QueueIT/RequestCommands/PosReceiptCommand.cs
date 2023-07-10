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
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.Middleware.Contracts.Constants;
using System.Globalization;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Interfaces;

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
        private readonly long _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly ICountrySpecificSettings _countryspecificSettings;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly IMiddlewareJournalITRepository _journalITRepository;
        private readonly IITSSCD _client;
        private readonly ISigningDevice _signingDevice;
        private readonly ILogger<DailyClosingReceiptCommand> _logger;

        public PosReceiptCommand(ISigningDevice signingDevice, ILogger<DailyClosingReceiptCommand> logger, IITSSCDProvider itIsscdProvider, SignatureItemFactoryIT signatureItemFactoryIT, IMiddlewareJournalITRepository journalITRepository, IConfigurationRepository configurationRepository, ICountrySpecificSettings countrySpecificSettings)
        {
            _client = itIsscdProvider.Instance;
            _signatureItemFactoryIT = signatureItemFactoryIT;
            _journalITRepository = journalITRepository;
            _countryspecificSettings = countrySpecificSettings;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _configurationRepository = configurationRepository;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var journals = await _journalITRepository.GetAsync().ConfigureAwait(false);
            if (journals.Where(x => x.cbReceiptReference.Equals(request.cbReceiptReference)).Any())
            {
                throw new CbReferenceExistsException(request.cbReceiptReference);
            }

            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);

            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, _countryBaseState);

            if (request.IsMultiUseVoucherSale())
            {
                return await CreateNonFiscalRequestAsync(queueIt, queueItem, receiptResponse, request).ConfigureAwait(false);
            }

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
                if (response.SSCDErrorInfo.Type == SSCDErrorType.Connection && !isBeingResent)
                {
                    return await ProcessFailedReceiptRequest(_signingDevice, _logger, _countryspecificSettings, queue, queueItem, request).ConfigureAwait(false);
                }
                else
                {
                    throw new SSCDErrorException(response.SSCDErrorInfo.Type, response.SSCDErrorInfo.Info);
                }
            }
            else
            {
                receiptResponse.ftReceiptIdentification += $"{response.ReceiptNumber}";
                receiptResponse.ftSignatures = _signatureItemFactoryIT.CreatePosReceiptSignatures(response);
                var journalIT = new ftJournalIT().FromResponse(queueIt, queueItem, new ScuResponse()
                {
                    DataJson = response.ReceiptDataJson,
                    ftReceiptCase = request.ftReceiptCase,
                    ReceiptDateTime = response.ReceiptDateTime,
                    ReceiptNumber = response.ReceiptNumber,
                    ZRepNumber = response.ZRepNumber
                });
                await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
            }

            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                Signatures = receiptResponse.ftSignatures.ToList(),
                ActionJournals = new List<ftActionJournal>()
            };
        }

        private async Task<RequestCommandResponse> CreateNonFiscalRequestAsync(ICountrySpecificQueue queue, ftQueueItem queueItem, ReceiptResponse receiptResponse, ReceiptRequest request)
        {
            var nonFiscalRequest = new NonFiscalRequest
            {
                NonFiscalPrints = new List<NonFiscalPrint>()
            };
            if (request.cbChargeItems != null)
            {
                foreach (var chargeItem in request.cbChargeItems.Where(x => x.IsMultiUseVoucherSale()))
                {
                    AddVoucherNonFiscalPrints(nonFiscalRequest.NonFiscalPrints, chargeItem.Amount, chargeItem.ftChargeItemCaseData);
                }
            }
            if (request.cbPayItems != null)
            {
                foreach (var payItem in request.cbPayItems.Where(x => x.IsVoucherSale()))
                {
                    AddVoucherNonFiscalPrints(nonFiscalRequest.NonFiscalPrints, payItem.Amount, payItem.ftPayItemCaseData);
                }
            }
            var response = await _client.NonFiscalReceiptAsync(nonFiscalRequest);

            if (response.Success)
            {
                receiptResponse.ftSignatures = _signatureItemFactoryIT.CreateVoucherSignatures(nonFiscalRequest);
            }
            var journalIT = new ftJournalIT
            {
                ftJournalITId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                cbReceiptReference = queueItem.cbReceiptReference,
                ftSignaturCreationUnitITId = queue.ftSignaturCreationUnitId.Value,
                JournalType = request.ftReceiptCase & 0xFFFF,
                ReceiptDateTime = queueItem.cbReceiptMoment,
                ReceiptNumber = -1,
                ZRepNumber = -1,
                DataJson = JsonConvert.SerializeObject(nonFiscalRequest),
                TimeStamp = DateTime.UtcNow.Ticks
            };
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);

            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                Signatures = receiptResponse.ftSignatures.ToList(),
                ActionJournals = new List<ftActionJournal>()
            };
        }

        private static void AddVoucherNonFiscalPrints(List<NonFiscalPrint> nonFiscalPrints, decimal amount, string info)
        {
            nonFiscalPrints.Add(new NonFiscalPrint() { Data = "***Voucher***", Font = 2 });
            if (!string.IsNullOrEmpty(info))
            {
                nonFiscalPrints.Add(new NonFiscalPrint() { Data = info, Font = 2 });
            }
            nonFiscalPrints.Add(new NonFiscalPrint()
            {
                Data = Math.Abs(amount).ToString(new NumberFormatInfo
                {
                    NumberDecimalSeparator = ",",
                    NumberGroupSeparator = "",
                    CurrencyDecimalDigits = 2
                }),
                Font = 2
            });
        }

        private static FiscalReceiptInvoice CreateInvoice(ReceiptRequest request)
        {
            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                //Barcode = ChargeItem.ProductBarcode,
                //TODO DisplayText = "Message on customer display",
                Operator = request.cbUser,
                Items = request.cbChargeItems.Where(x => !x.IsPaymentAdjustment()).Select(p => new Item
                {
                    Description = p.Description,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice ?? p.Amount / p.Quantity,
                    Amount = p.Amount,
                    VatGroup = p.GetVatGroup(),
                    AdditionalInformation = p.ftChargeItemCaseData
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.GetPayments()
            };
            return fiscalReceiptRequest;
        }

        private async Task<FiscalReceiptRefund> CreateRefundAsync(ReceiptRequest request)
        {
            var refundDetails = await GetRefundDetailsAsync(request).ConfigureAwait(false);
            var fiscalReceiptRequest = new FiscalReceiptRefund()
            {
                //TODO Barcode = "0123456789" 
                Operator = request.cbUser,
                DisplayText = $"REFUND {refundDetails.ZRepNumber:D4} {refundDetails.ReceiptNumber:D4} {refundDetails.ReceiptDateTime:ddMMyyyy} {refundDetails.Serialnumber}",
                Refunds = request.cbChargeItems?.Select(p => new Refund
                {
                    Description = p.Description,
                    Quantity = Math.Abs(p.Quantity),
                    UnitPrice = p.UnitPrice ?? 0,
                    Amount = Math.Abs(p.Amount),
                    VatGroup = p.GetVatGroup()
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.cbPayItems?.Select(p => new Payment
                {
                    Amount = p.Amount,
                    Description = p.Description,
                    PaymentType = p.GetPaymentType(),
                }).ToList()
            };

            return fiscalReceiptRequest;
        }

        private async Task<RefundDetails> GetRefundDetailsAsync(ReceiptRequest request)
        {
            var journalIt = await _journalITRepository.GetAsync().ConfigureAwait(false);
            var receipt = journalIt.Where(x => x.cbReceiptReference.Equals(request.cbPreviousReceiptReference)).FirstOrDefault() ?? throw new RefundException($"Receipt {request.cbPreviousReceiptReference} was not found!");
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(receipt.ftSignaturCreationUnitITId).ConfigureAwait(false);
            var deviceInfo = JsonConvert.DeserializeObject<DeviceInfo>(scu.InfoJson);
            return new RefundDetails()
            {
                ReceiptNumber = receipt.ReceiptNumber,
                ZRepNumber = receipt.ZRepNumber,
                ReceiptDateTime = receipt.ReceiptDateTime,
                Serialnumber = deviceInfo.SerialNumber
            };
        }

        public override async Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var journalIt = await _journalITRepository.GetByQueueItemId(queueItem.ftQueueItemId).ConfigureAwait(false);
            return journalIt == null;
        }
    }
}
