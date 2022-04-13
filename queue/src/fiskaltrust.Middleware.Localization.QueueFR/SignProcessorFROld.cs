using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueFR;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.service.shared
{
//    public class SignProcessorFROld : IMarketSpecificSignProcessor
//    {   
//        private readonly IReadOnlyQueueItemRepository _queueItemRepository;
//        private readonly IConfigurationRepository _configurationRepository;
//        private readonly IJournalFRRepository _journalFRRepository;
//        private readonly ICryptoHelper _cryptoHelper;
//        private readonly bool _sandbox;

//        public SignProcessorFR(IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalFRRepository journalFRRepository, ICryptoHelper cryptoHelper, bool sandbox)
//        {
//            _queueItemRepository = queueItemRepository;
//            _configurationRepository = configurationRepository;
//            _journalFRRepository = journalFRRepository;
//            _cryptoHelper = cryptoHelper;
//            _sandbox = sandbox;
//        }

//        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
//        {
//            var queueFR = await _configurationRepository.GetQueueFRAsync(queueItem.ftQueueId).ConfigureAwait(false);
//            var signaturCreationUnitFR = await _configurationRepository.GetSignaturCreationUnitFRAsync(queueFR.ftSignaturCreationUnitFRId).ConfigureAwait(false);

//            var (success, receiptResonse, actionJournals, journalFR) = Sign(request, queue, queueFR, queueItem, signaturCreationUnitFR);

//            if (success)
//            {
//                await _configurationRepository.InsertOrUpdateQueueFRAsync(queueFR).ConfigureAwait(false);
//                await _journalFRRepository.InsertAsync(journalFR).ConfigureAwait(false);
//                return (receiptResonse, actionJournals);
//            }
//            else
//            {
//                return (null, actionJournals);
//            }
//        }

//        public (bool success, ReceiptResponse receiptResonse, List<ftActionJournal> actionJournals, ftJournalFR journalFR) Sign(ReceiptRequest receiptRequest, ftQueue queue, ftQueueFR queueFr, ftQueueItem queueItem, ftSignaturCreationUnitFR signaturCreationUnitFR)
//        {
//            queueItem.ftWorkMoment = DateTime.UtcNow;
//            var localActionJournal = new List<ftActionJournal>();
//            var receiptResponse = new ReceiptResponse
//            {
//                ftCashBoxID = receiptRequest.ftCashBoxID,
//                ftCashBoxIdentification = queueFr.CashBoxIdentification,
//                ftQueueID = queue.ftQueueId.ToString(),
//                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
//                ftQueueRow = queueItem.ftQueueRow,
//                cbTerminalID = receiptRequest.cbTerminalID,
//                cbReceiptReference = receiptRequest.cbReceiptReference,
//                ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#",
//                ftReceiptMoment = DateTime.UtcNow,
//                ftState = 0x4652000000000000
//            };

//            var dataConsistency = CheckDataConsistency(receiptRequest, receiptResponse, queue, queueFr, queueItem);

//            if (!string.IsNullOrWhiteSpace(dataConsistency.Message))
//            {
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournal, dataConsistency.Message, null);
//                queueItem.ftDoneMoment = DateTime.UtcNow;
//                return (dataConsistency.Success, null, localActionJournal, null);
//            }

//            CheckFailedMode(receiptRequest, receiptResponse, queue, ref queueFr, queueItem, localActionJournal);
//            AddMessageSignatures(ref queueFr, localActionJournal, receiptRequest, receiptResponse);

//            ftJournalFR journalFR = null;
//            queueItem.ftDoneMoment = DateTime.UtcNow;
//            if ((receiptRequest.ftReceiptCase & 0x0000000000020000) != 0)
//            {
//                //TODO handle case if signatures need to be adde3d
//                var totals = receiptRequest.GetTotals();
//                journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//            }
//            else
//            {
//                switch (receiptRequest.ftReceiptCase & 0xFFFF)
//                {
//                    case 0x0000:
//                    case 0x0001:
//                        journalFR = TicketRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0002:
//                        journalFR = PaymentProveRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0003:
//                        journalFR = InvoiceRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0004:
//                        journalFR = ShiftRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0005:
//                        journalFR = DayRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0006:
//                        journalFR = MonthRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0007:
//                        journalFR = YearRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0008:
//                        journalFR = BillRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0009:
//                        //TODO is this really not implemented?
//                        throw new NotImplementedException("DeliveryNoteRequest is not implemented.");
//                    case 0x000A:
//                        journalFR = CashDepositRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x000B:
//                        journalFR = PayoutRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x000C:
//                        journalFR = PaymentTransferRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x000D:
//                        //TODO is this really not implemented?
//                        throw new NotImplementedException("InternalRequest is not implemented.");
//                    case 0x000E:
//                        journalFR = ForeignSaleRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x000F:
//                        journalFR = ZeroRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0010:
//                        journalFR = StartRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0011:
//                        journalFR = StopRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0012:
//                        journalFR = LogRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0013:
//                        journalFR = AuditRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0014:
//                        //TODO is this really not implemented?
//                        throw new NotImplementedException("ProtocolRequest is not implemented.");
//                    case 0x0015:
//                        journalFR = ArchiveRequest(queue, queueFr, signaturCreationUnitFR, queueItem, localActionJournal, receiptRequest, receiptResponse);
//                        break;
//                    case 0x0016:
//                        journalFR = CopyRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse);
//                        break;
//                    default:
//                        break;
//                }
//            }
//            if (_sandbox)
//            {
//                var signatures = new List<SignaturItem>(receiptResponse.ftSignatures)
//                {
//                    new SignaturItem() { Caption = "S A N D B O X", Data = queueFr.ftQueueFRId.ToString(), ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown }
//                };
//                receiptResponse.ftSignatures = signatures.ToArray();
//            }

//            queueItem.ftDoneMoment = DateTime.UtcNow;
//            return (true, receiptResponse, localActionJournal, journalFR);
//        }

//        internal ValidationError CheckDataConsistency(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueue queue, ftQueueFR queueFr, ftQueueItem queueItem)
//        {
//            if (new Guid(receiptRequest.ftCashBoxID) != queue.ftCashBoxId)
//            {
//                return new ValidationError() { Success = false, Message = $"CashBoxId of QueueItem {queueItem.ftQueueItemId} does not match CashBoxId of Queue {queueFr.ftQueueFRId}" };
//            }

//            if ((receiptRequest.ftReceiptCase >> 48) != 0x4652)
//            {
//                return new ValidationError() { Success = false, Message = $"The receipt case [0x{receiptRequest.ftReceiptCase:X}] does not match the expected country id 0x4652XXXXXXXXXXXX" };
//            }

//            var ciSum = 0m;
//            var piSum = 0m;

//            if (receiptRequest.cbChargeItems != null)
//            {
//                ciSum = receiptRequest.cbChargeItems.Sum(ci => ci.Amount);
//                var wrongChargeItems = receiptRequest.cbChargeItems.Where(ci => (ci.ftChargeItemCase >> 48) != 0x4652);
//                if (wrongChargeItems.Any())
//                {
//                    return new ValidationError() { Success = false, Message = $"The charge item case [0x{wrongChargeItems.First().ftChargeItemCase:X}] does not match the expected country id 0x4652XXXXXXXXXXXX" };
//                }
//            }
//            if (receiptRequest.cbPayItems != null)
//            {
//                piSum = receiptRequest.cbPayItems.Sum(ci => ci.Amount);
//                var wrongChargeItems = receiptRequest.cbPayItems.Where(ci => (ci.ftPayItemCase >> 48) != 0x4652);
//                if (wrongChargeItems.Any())
//                {
//                    return new ValidationError() { Success = false, Message = $"The pay item case [0x{wrongChargeItems.First().ftPayItemCase:X}] does not match the expected country id 0x4652XXXXXXXXXXXX" };
//                }
//            }

//            if (ciSum != piSum)
//            {
//                return new ValidationError() { Success = false, Message = $"The sum of the amounts of the charge items ({ciSum}) is not equal to the sum of the amounts of the pay items ({piSum})" };
//            }

//            switch (receiptRequest.ftReceiptCase)
//            {
//                case 0x4652000000000002: //Payment Prove
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Payment Prove receipt must not have charge items" };
//                    }
//                };
//                break;
//                case 0x4652000000000004: //Shift receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Shift receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Shift receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000005: //Daily receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Daily receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Daily receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000006: //Monthly receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Monthly receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Monthly receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000007: //Yearly receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Yearly receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Yearly receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000008: //Bill receipt
//                {
//                    if (receiptRequest.cbPayItems == null || receiptRequest.cbPayItems.Length != 1 || receiptRequest.cbPayItems.Where(pi => pi.ftPayItemCase == 0x4652000000000011).Count() != 1)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Bill receipt must have one pay item with the ftPayItemCase = 0x4652000000000011" };
//                    }
//                };
//                break;
//                case 0x465200000000000A: //Cash Deposit
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Cash Deposit receipt must not have charge items" };
//                    }
//                };
//                break;
//                case 0x465200000000000B: //Payout
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Payout receipt must not have charge items" };
//                    }
//                };
//                break;
//                case 0x465200000000000C: //Payment Transfer
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Payment Transfer receipt must not have charge items" };
//                    }
//                };
//                break;
//                case 0x465200000000000E: //Foreign Sale receipt
//                {
//                    if (receiptRequest.cbPayItems == null || receiptRequest.cbPayItems.Length != 1 || receiptRequest.cbPayItems.Where(pi => pi.ftPayItemCase == 0x4652000000000011).Count() != 1)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Foreign Sale receipt must have one pay item with the ftPayItemCase = 0x4652000000000011" };
//                    }
//                };
//                break;
//                case 0x465200000000000F: //Zero receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Zero receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Zero receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000010: //Start receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Start receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Start receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000011: //Stop receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Stop receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Stop receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000013: //Audit receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Audit receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Audit receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000015: //Archive receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Archive receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Archive receipt must not have pay items" };
//                    }
//                };
//                break;
//                case 0x4652000000000016: //Copy receipt
//                {
//                    if (receiptRequest.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Copy receipt must not have charge items" };
//                    }

//                    if (receiptRequest.cbPayItems != null && receiptRequest.cbPayItems.Length > 0)
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Copy receipt must not have pay items" };
//                    }

//                    if (string.IsNullOrWhiteSpace(receiptRequest.cbPreviousReceiptReference))
//                    {
//                        return new ValidationError() { Success = false, Message = $"The Copy receipt must provide the POS System receipt reference of the receipt whose the copy has been asked" };
//                    }
//                    //if (!parentStorage.QueueItemTableByReceiptReference(ReceiptRequest.cbPreviousReceiptReference).Any()) return new DataConsistencyResult() { Success = false, Message = $"The Copy receipt refers to an unknown receipt reference" };
//                };
//                break;
//            }

//            if (!queue.StartMoment.HasValue && receiptRequest.ftReceiptCase != 0x4652000000000010)
//            {
//                receiptResponse.ftState |= 0x1;
//                return new ValidationError() { Success = true, Message = $"Queue {queueFr.ftQueueFRId} is out of order, it has not been activated!" };
//            }

//            if (queue.StartMoment.HasValue && queue.StopMoment.HasValue)
//            {
//                receiptResponse.ftState |= 0x1;
//                return new ValidationError() { Success = true, Message = $"Queue {queueFr.ftQueueFRId} is out of order, it is permanent de-activated!" };
//            }

//            return new ValidationError() { Success = true };
//        }

//        internal void CheckFailedMode(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueue queue, ref ftQueueFR queueFr, ftQueueItem queueItem, List<ftActionJournal> localActionJournals)
//        {
//            ftActionJournal aj;

//            if ((receiptRequest.ftReceiptCase & 0x0000000000010000) > 0)
//            {
//                if (!queueFr.UsedFailedMomentMin.HasValue)
//                {
//                    queueFr.UsedFailedMomentMin = receiptRequest.cbReceiptMoment;
//                    queueFr.UsedFailedMomentMax = receiptRequest.cbReceiptMoment;
//                    queueFr.UsedFailedQueueItemId = queueItem.ftQueueItemId;

//                    aj = new ftActionJournal
//                    {
//                        ftActionJournalId = Guid.NewGuid(),
//                        ftQueueId = queue.ftQueueId,
//                        ftQueueItemId = queueItem.ftQueueItemId,
//                        Moment = DateTime.UtcNow,
//                        Message = $"QueueItem {queueItem.ftQueueItemId} enabled mode \"UsedFailed\" of Queue {queueFr.ftQueueFRId}"
//                    };
//                    localActionJournals.Add(aj);

//                }
//                queueFr.UsedFailedCount++;

//                if (receiptRequest.cbReceiptMoment < queueFr.UsedFailedMomentMin)
//                {
//                    queueFr.UsedFailedMomentMin = receiptRequest.cbReceiptMoment;
//                }

//                if (receiptRequest.cbReceiptMoment > queueFr.UsedFailedMomentMax)
//                {
//                    queueFr.UsedFailedMomentMax = receiptRequest.cbReceiptMoment;
//                }
//            }

//            if (queueFr.UsedFailedCount > 0)
//            {
//                receiptResponse.ftState |= 0x8;
//            }
//        }

//        internal void ResetFailedMode(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueue queue, ref ftQueueFR queueFr, ftQueueItem queueItem, List<ftActionJournal> localActionJournals)
//        {
//            if (queueFr.UsedFailedCount > 0)
//            {
//                //try to recover from usedfailed mode
//                var localSignatures = new List<SignaturItem>(receiptResponse.ftSignatures);

//                var fromReceipt = "#";
//                try
//                {
//                    var fromQueueItem = _queueItemRepository.GetAsync(queueFr.UsedFailedQueueItemId.Value).Result;
//                    var fromResponse = JsonConvert.DeserializeObject<ReceiptResponse>(fromQueueItem.response);
//                    fromReceipt = fromResponse.ftReceiptIdentification;
//                }
//                catch (Exception x)
//                {
//                    if ((receiptRequest.ftReceiptCase & 0x0000000000020000) == 0)
//                    {
//                        AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"QueueItem {queueItem.ftQueueItemId} error on resolving receiptidentificateion of queueitem {queueFr.UsedFailedQueueItemId} where used-failed beginns: {x.Message}", null);
//                    }
//                }
//                var toReceipt = receiptResponse.ftReceiptIdentification;
//                localSignatures.Add(new SignaturItem() { Caption = "Failure registered", Data = $"from {fromReceipt} to {toReceipt} ", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.Information });
//                receiptResponse.ftSignatures = localSignatures.ToArray();

//                //if it is not a training receit, the counter will be updated
//                if ((receiptRequest.ftReceiptCase & 0x0000000000020000) == 0)
//                {
//                    AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"QueueItem {queueItem.ftQueueItemId} recovered Queue {queueFr.ftQueueFRId} from used-failed mode. closing chain of failed receipts from {fromReceipt} to {toReceipt}.", null);

//                    //reset used-fail mode
//                    queueFr.UsedFailedCount = 0;
//                    queueFr.UsedFailedMomentMin = null;
//                    queueFr.UsedFailedMomentMax = null;
//                    queueFr.UsedFailedQueueItemId = null;
//                }

//                //remove used-fail state from response-state
//                if ((receiptResponse.ftState & 0x0008) != 0)
//                {
//                    //remove used-failed state
//                    receiptResponse.ftState -= 0x0008;
//                }
//            }
//        }

//        internal void AddActionJournal(ftQueue queue, ref ftQueueFR queueFr, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, string message, string dataJson, int priority = 0)
//        {
//            var aj = new ftActionJournal
//            {
//                ftActionJournalId = Guid.NewGuid(),
//                ftQueueId = queue.ftQueueId,
//                ftQueueItemId = queueItem.ftQueueItemId,
//                Moment = DateTime.UtcNow,
//                Message = message,
//                DataJson = dataJson,
//                Priority = priority
//            };
//            localActionJournals.Add(aj);
//            if (priority < 0)
//            {
//                queueFr.MessageCount++;
//                if (!queueFr.MessageMoment.HasValue)
//                {
//                    //queueFr.MessageMoment = new DateTime(parentStorage.ActionJournalTimeStamp(queueFr.ftQueueFRId));
//                    // TODO: Set messagemoment
//                }
//            }
//        }

//        internal string GetArchivePayload(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, string lastHash)
//#pragma warning restore CA1801 // Review unused parameters
//        {
//            // Somehow use this stuff to not break build
//            Console.WriteLine(receiptRequest);
//            Console.WriteLine(receiptResponse);
//            Console.WriteLine(queue);
//            Console.WriteLine(queueFr);
//            Console.WriteLine(signaturCreationUnitFR);
//            Console.WriteLine(lastHash);

//            //var from = queueFr.GLastDayMoment.HasValue ? queueFr.GLastDayMoment.Value.Ticks : 0;
//            //var actionJournals = parentStorage.ActionJournalTableByTimeStamp(from, parentStorage.ActionJournalTimeStamp(queueFr.ftQueueFRId));
//            //var lastActionJournal = actionJournals.Where(a => a.TimeStamp == actionJournals.Select(a2 => a2.TimeStamp).Max()).FirstOrDefault();
//            //var journalFRs = parentStorage.JournalFRTableByTimeStamp(from, parentStorage.JournalFRTimeStamp(queueFr.ftQueueFRId));
//            //var lastJournalFR = journalFRs.Where(a => a.TimeStamp == journalFRs.Select(a2 => a2.TimeStamp).Max()).FirstOrDefault();
//            //var receiptJournals = parentStorage.ReceiptJournalTableByTimeStamp(from, parentStorage.ReceiptJournalTimeStamp(queueFr.ftQueueFRId));
//            //var lastReceiptJournal = receiptJournals.Where(a => a.TimeStamp == receiptJournals.Select(a2 => a2.TimeStamp).Max()).FirstOrDefault();


//            //Guid? firstArchiveReceiptQueueItemId = null;
//            //DateTime? firstArchiveReceiptMoment = null;
//            //Guid? lastArchiveReceiptQueueItemId = null;
//            //DateTime? lastArchiveReceiptMoment = null;
//            //ftQueueItem lastAllowedQueueItem = null;
//            //ArchivePayload previousArchivePayload = null;

//            ////here it is searched the last archived receipt within the previous archive receipts, they could be empty in case of multiple archive requests in row 
//            //do
//            //{
//            //    var previousArchiveQueueItemId = previousArchivePayload == null ? queueFr.ALastQueueItemId : previousArchivePayload.PreviousArchiveQueueItemId;
//            //    if (!previousArchiveQueueItemId.HasValue)
//            //    {
//            //        previousArchivePayload = null;
//            //        break;
//            //    }
//            //    var previousArchive = _queueItemRepository.GetAsync(previousArchiveQueueItemId.Value).Result;
//            //    var previousArchiveResponse = JsonConvert.DeserializeObject<ReceiptResponse>(previousArchive.response);
//            //    var jwt = previousArchiveResponse.ftSignatures.Where(s => s.ftSignatureType == 0x4652000000000001).First().Data;
//            //    previousArchivePayload = JsonConvert.DeserializeObject<ArchivePayload>(Encoding.UTF8.GetString(ifPOS.Utilities.FromBase64urlString(jwt.Split('.')[1])));
//            //} while (!previousArchivePayload.LastContainedReceiptQueueItemId.HasValue);

//            //if (previousArchivePayload != null && previousArchivePayload.LastContainedReceiptQueueItemId.HasValue)
//            //{
//            //    var firstArchiveItem = parentStorage.QueueItemTableByTimeStamp(_queueItemRepository.GetAsync(previousArchivePayload.LastContainedReceiptQueueItemId.Value).Result.TimeStamp + 1, null, 1).FirstOrDefault();
//            //    firstArchiveReceiptQueueItemId = firstArchiveItem.ftQueueItemId;

//            //    if (firstArchiveReceiptQueueItemId.Value.ToString() == receiptResponse.ftQueueItemID)
//            //    {
//            //        firstArchiveReceiptMoment = receiptResponse.ftReceiptMoment;
//            //        lastAllowedQueueItem = firstArchiveItem;
//            //    }
//            //    else if (firstArchiveItem.response != null)
//            //    {
//            //        var firstArchiveReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(firstArchiveItem.response);
//            //        firstArchiveReceiptMoment = firstArchiveReceiptResponse.ftReceiptMoment;
//            //    }

//            //    if (lastAllowedQueueItem == null)
//            //    {
//            //        lastAllowedQueueItem = parentStorage.QueueItemTableByTimeStamp(firstArchiveItem.TimeStamp + 1).Where(qi => qi.ftQueueMoment < firstArchiveReceiptMoment.Value.AddYears(1).Date).OrderByDescending(qi => qi.TimeStamp).FirstOrDefault();
//            //    }
//            //}
//            //else if (queue.StartMoment.HasValue)
//            //{
//            //    lastAllowedQueueItem = parentStorage.QueueItemTableByTimeStamp().Where(qi => qi.ftQueueMoment < queue.StartMoment.Value.AddYears(1).Date).OrderByDescending(qi => qi.TimeStamp).FirstOrDefault();
//            //}

//            ////it can be null in the following cases:
//            ////  - the queue is not used for one year or more
//            ////  - the Archive receipt has been requested twice (or more) in row
//            ////  - the queue is not yet started (the execution should not reach this method in this case)
//            ////  - the queue has no receipts
//            //if (lastAllowedQueueItem != null)
//            //{
//            //    lastArchiveReceiptQueueItemId = lastAllowedQueueItem.ftQueueItemId;
//            //    if (lastArchiveReceiptQueueItemId.Value.ToString() == receiptResponse.ftQueueItemID)
//            //    {
//            //        lastArchiveReceiptMoment = receiptResponse.ftReceiptMoment;
//            //    }
//            //    else if (lastAllowedQueueItem.response != null)
//            //    {
//            //        var lastArchiveReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(lastAllowedQueueItem.response);
//            //        lastArchiveReceiptMoment = lastArchiveReceiptResponse.ftReceiptMoment;
//            //    }
//            //}
//            //else
//            //{
//            //    lastArchiveReceiptQueueItemId = firstArchiveReceiptQueueItemId;
//            //    lastArchiveReceiptMoment = firstArchiveReceiptMoment;
//            //}

//            //var payload = new ArchivePayload()
//            //{
//            //    QueueId = Guid.Parse(receiptResponse.ftQueueID),
//            //    CashBoxIdentification = receiptResponse.ftCashBoxIdentification,
//            //    Siret = signaturCreationUnitFR.Siret,
//            //    ReceiptId = receiptResponse.ftReceiptIdentification,
//            //    ReceiptMoment = receiptResponse.ftReceiptMoment,
//            //    ReceiptCase = receiptRequest.ftReceiptCase,
//            //    QueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
//            //    DTotalizer = queueFr.GDayTotalizer,
//            //    DCINormal = queueFr.GDayCITotalNormal,
//            //    DCIReduced1 = queueFr.GDayCITotalReduced1,
//            //    DCIReduced2 = queueFr.GDayCITotalReduced2,
//            //    DCIReducedS = queueFr.GDayCITotalReducedS,
//            //    DCIZero = queueFr.GDayCITotalZero,
//            //    DCIUnknown = queueFr.GDayCITotalUnknown,
//            //    DPICash = queueFr.GDayPITotalCash,
//            //    DPINonCash = queueFr.GDayPITotalNonCash,
//            //    DPIInternal = queueFr.GDayPITotalInternal,
//            //    DPIUnknown = queueFr.GDayPITotalUnknown,
//            //    MTotalizer = queueFr.GMonthTotalizer,
//            //    MCINormal = queueFr.GMonthCITotalNormal,
//            //    MCIReduced1 = queueFr.GMonthCITotalReduced1,
//            //    MCIReduced2 = queueFr.GMonthCITotalReduced2,
//            //    MCIReducedS = queueFr.GMonthCITotalReducedS,
//            //    MCIZero = queueFr.GMonthCITotalZero,
//            //    MCIUnknown = queueFr.GMonthCITotalUnknown,
//            //    MPICash = queueFr.GMonthPITotalCash,
//            //    MPINonCash = queueFr.GMonthPITotalNonCash,
//            //    MPIInternal = queueFr.GMonthPITotalInternal,
//            //    MPIUnknown = queueFr.GMonthPITotalUnknown,
//            //    YTotalizer = queueFr.GYearTotalizer,
//            //    YCINormal = queueFr.GYearCITotalNormal,
//            //    YCIReduced1 = queueFr.GYearCITotalReduced1,
//            //    YCIReduced2 = queueFr.GYearCITotalReduced2,
//            //    YCIReducedS = queueFr.GYearCITotalReducedS,
//            //    YCIZero = queueFr.GYearCITotalZero,
//            //    YCIUnknown = queueFr.GYearCITotalUnknown,
//            //    YPICash = queueFr.GYearPITotalCash,
//            //    YPINonCash = queueFr.GYearPITotalNonCash,
//            //    YPIInternal = queueFr.GYearPITotalInternal,
//            //    YPIUnknown = queueFr.GYearPITotalUnknown,
//            //    ATotalizer = queueFr.ATotalizer,
//            //    ACINormal = queueFr.ACITotalNormal,
//            //    ACIReduced1 = queueFr.ACITotalReduced1,
//            //    ACIReduced2 = queueFr.ACITotalReduced2,
//            //    ACIReducedS = queueFr.ACITotalReducedS,
//            //    ACIZero = queueFr.ACITotalZero,
//            //    ACIUnknown = queueFr.ACITotalUnknown,
//            //    APICash = queueFr.APITotalCash,
//            //    APINonCash = queueFr.APITotalNonCash,
//            //    APIInternal = queueFr.APITotalInternal,
//            //    APIUnknown = queueFr.APITotalUnknown,
//            //    LastActionJournalId = lastActionJournal == null ? Guid.Empty : lastActionJournal.ftActionJournalId,
//            //    LastJournalFRId = lastJournalFR == null ? Guid.Empty : lastJournalFR.ftJournalFRId,
//            //    LastReceiptJournalId = lastReceiptJournal == null ? Guid.Empty : lastReceiptJournal.ftReceiptJournalId,
//            //    PreviousArchiveQueueItemId = queueFr.ALastQueueItemId, //every archive payload is chained to the previous archive request also by the queue item id 
//            //    FirstContainedReceiptMoment = firstArchiveReceiptMoment,
//            //    FirstContainedReceiptQueueItemId = firstArchiveReceiptQueueItemId,
//            //    LastContainedReceiptMoment = lastArchiveReceiptMoment,
//            //    LastContainedReceiptQueueItemId = lastArchiveReceiptQueueItemId,
//            //    LastHash = lastHash,
//            //    CertificateSerialNumber = signaturCreationUnitFR.CertificateSerialNumber
//            //};

//            //return JsonConvert.SerializeObject(payload);

//            throw new NotImplementedException();
//        }

//        internal ftJournalFR TicketRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"T{++queueFr.TNumerator}";
//            queueFr.AddReceiptTotalsToTicketTotals(totals);
//            queueFr.AddReceiptTotalsToGrandTotals(totals);
//            queueFr.AddReceiptTotalsToArchiveTotals(totals);
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.TLastHash);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.TLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "T";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR PaymentProveRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"P{++queueFr.PNumerator}";
//            queueFr.AddReceiptTotalsToPaymentProveTotals(totals);
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.PLastHash);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.PLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "P";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR InvoiceRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"I{++queueFr.INumerator}";
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.ILastHash);
//            queueFr.AddReceiptTotalsToInvoiceTotals(totals);
//            queueFr.AddReceiptTotalsToGrandTotals(totals);
//            queueFr.AddReceiptTotalsToArchiveTotals(totals);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.ILastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "I";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR CashDepositRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"P{++queueFr.PNumerator}";
//            //TODO - which are the differences betweep a Payment Prove and a Payout?
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.PLastHash);
//            queueFr.AddReceiptTotalsToPaymentProveTotals(totals);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.PLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "P";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR BillRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"B{++queueFr.BNumerator}";
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.BLastHash);
//            queueFr.AddReceiptTotalsToBillTotals(totals);
//            //AddReceiptTotalsToGrandTotals(ReceiptRequest, ReceiptResponse, totals, queueFr, signatures);
//            //AddReceiptTotalsToArchiveTotals(ReceiptRequest, ReceiptResponse, totals, queueFr, signatures);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.BLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "B";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR PayoutRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"P{++queueFr.PNumerator}";
//            //TODO - which are the differences betweep a Payment Prove and a Payout?
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.PLastHash);
//            queueFr.AddReceiptTotalsToPaymentProveTotals(totals);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.PLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "P";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR PaymentTransferRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"P{++queueFr.PNumerator}";
//            //TODO - which are the differences betweep a Payment Prove and a Payout?
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.PLastHash);
//            queueFr.AddReceiptTotalsToPaymentProveTotals(totals);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.PLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "P";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR ForeignSaleRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            //TODO - which are the differences betweep a Bill and a Foreign Sale?
//            receiptResponse.ftReceiptIdentification += $"B{++queueFr.BNumerator}";
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.BLastHash);
//            queueFr.AddReceiptTotalsToBillTotals(totals);
//            //AddReceiptTotalsToGrandTotals(ReceiptRequest, ReceiptResponse, totals, QueueFR, signatures);
//            //AddReceiptTotalsToArchiveTotals(ReceiptRequest, ReceiptResponse, totals, QueueFR, signatures);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.BLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "B";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR LogRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"L{++queueFr.LNumerator}";
//            //TODO: what should be the payload
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.LLastHash);
//            //AddActionJournal(Queue, QueueItem, LocalActionJournals, $"Log requested", payload);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.LLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "L";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR CopyRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            receiptResponse.ftReceiptIdentification += $"C{++queueFr.CNumerator}";
//            var payload = PayloadFactory.GetCopyPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, queueFr.CLastHash);
//            //AddActionJournal(Queue, QueueItem, LocalActionJournals, $"Copy requested of the receipt [{ReceiptRequest.ftReceiptCaseData}]", payload);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.CLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "C";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR AuditRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var totals = receiptRequest.GetTotals();
//            receiptResponse.ftReceiptIdentification += $"L{++queueFr.LNumerator}";
//            //TODO: what should be the payload
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.LLastHash);
//            //AddActionJournal(Queue, QueueItem, LocalActionJournals, $"Audit requested", payload);
      
//            (var lastHash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.LLastHash = lastHash;
//            if (journalFR != null)
//            {
//                journalFR.ReceiptType = "L";
//            }

//            return journalFR;
//        }

//        internal ftJournalFR ShiftRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddShiftSignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                AddShiftSignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"Shift closure", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR DayRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"Daily closure", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR MonthRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddMonthlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddMonthlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"Monthly closure", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR YearRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddMonthlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddYearlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddMonthlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddYearlySignature(queueFr, signaturCreationUnitFR,queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"Yearly closure", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR ZeroRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                ResetFailedMode(receiptRequest, receiptResponse, queue, ref queueFr, queueItem, localActionJournals);
//                return TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"Zero receipt", payload);
//                ResetFailedMode(receiptRequest, receiptResponse, queue, ref queueFr, queueItem, localActionJournals);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR StartRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                queue.StartMoment = DateTime.UtcNow;
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"QueueItem {queueItem.ftQueueItemId} start enable of Queue {queueFr.ftQueueFRId}", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR StopRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddMonthlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddYearlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"G{++queueFr.GNumerator}";
//                var payload = PayloadFactory.GetGrandTotalPayload(receiptRequest, receiptResponse, queueFr, signaturCreationUnitFR, queueFr.GLastHash);
//                queue.StopMoment = DateTime.UtcNow;
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddMonthlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddYearlySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"QueueItem {queueItem.ftQueueItemId} try to disable Queue {queueFr.ftQueueFRId}", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.GLastHash = hash;
//                journalFR.ReceiptType = "G";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR ArchiveRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (receiptRequest.HasTrainingReceiptFlag())
//            {
//                var totals = receiptRequest.GetTotals();
//                var journalFR = TrainingRequest(queue, queueFr, signaturCreationUnitFR, receiptRequest, receiptResponse, totals);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddArchiveSignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                return journalFR;
//            }
//            else
//            {
//                receiptResponse.ftReceiptIdentification += $"A{++queueFr.ANumerator}";
//                var payload = GetArchivePayload(receiptRequest, receiptResponse, queue, queueFr, signaturCreationUnitFR, queueFr.ALastHash);
//                AddDailySignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddArchiveSignature(queueFr, signaturCreationUnitFR, queueItem, receiptRequest, receiptResponse);
//                AddActionJournal(queue, ref queueFr, queueItem, localActionJournals, $"Archive requested", payload);
          
//                (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//                queueFr.ALastMoment = queueItem.ftQueueMoment;
//                queueFr.ALastQueueItemId = queueItem.ftQueueItemId;
//                queueFr.ALastHash = hash;
//                journalFR.ReceiptType = "A";
//                return journalFR;
//            }
//        }

//        internal ftJournalFR TrainingRequest(ftQueue queue, ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, Totals totals)
//        {
//            receiptResponse.ftReceiptIdentification += $"X{++queueFr.XNumerator}";
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, totals, queueFr.XLastHash);
//            if (totals.Totalizer.HasValue)
//            {
//                queueFr.XTotalizer += totals.Totalizer.Value;
//            }

//            var footers = new List<string>(receiptResponse.ftReceiptFooter ?? Array.Empty<string>())
//            {
//                "T R A I N I N G"
//            };
//            receiptResponse.ftReceiptFooter = footers.ToArray();
      
//            (var hash, var journalFR) = AddTotalsSignatureFR(receiptResponse, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
//            queueFr.XLastHash = hash;
//            journalFR.ReceiptType = "X";
//            return journalFR;
//        }

//        internal string AddTotalsSignatureFRWithoutSign(ReceiptResponse receiptResponse, string payload, string description, SignaturItem.Formats format, SignaturItem.Types type)
//        {
//            var signatures = new List<SignaturItem>(receiptResponse.ftSignatures);
//            var signaturItem = new SignaturItem()
//            {
//                Caption = description,
//                Data = payload,
//                ftSignatureFormat = (long) format,
//                ftSignatureType = (long) type
//            };
//            signatures.Add(signaturItem);
//            receiptResponse.ftSignatures = signatures.ToArray();
//            return signaturItem.Data;
//        }

//        internal (string hash, ftJournalFR journalFR) AddTotalsSignatureFR(ReceiptResponse receiptResponse, ftQueue queue, ftSignaturCreationUnitFR signaturCreationUnitFR, string payload, string description, SignaturItem.Formats format, SignaturItem.Types type)
//        {
//            var signatures = new List<SignaturItem>(receiptResponse.ftSignatures);
//            var jwsData = _cryptoHelper.CreateJwsToken(payload, signaturCreationUnitFR.PrivateKey, signaturCreationUnitFR.ftSignaturCreationUnitFRId.ToByteArray());
//            var signaturItem = new SignaturItem()
//            {
//                Caption = description,
//                Data = jwsData,
//                ftSignatureFormat = (long) format,
//                ftSignatureType = (long) type
//            };
//            signatures.Add(signaturItem);
//            var result = _cryptoHelper.GenerateJwsBase64Hash(payload);
//            var journalFR = new ftJournalFR()
//            {
//                ftJournalFRId = Guid.NewGuid(),
//                ftQueueId = Guid.Parse(receiptResponse.ftQueueID),
//                ftQueueItemId = Guid.Parse(receiptResponse.ftQueueItemID),
//                JWT = signaturItem.Data,
//                Number = queue.ftReceiptNumerator + 1
//            };
//            receiptResponse.ftSignatures = signatures.ToArray();
//            return (result, journalFR);
//        }

//        internal void AddArchiveSignature(ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, queueFr.GetArchiveTotals(), queueFr.ALastHash);
//            AddTotalsSignatureFRWithoutSign(receiptResponse, payload, "Archive Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000006);
//            if (!receiptRequest.HasTrainingReceiptFlag())
//            {
//                queueFr.ResetArchiveTotalizers(queueItem);
//            }
//        }

//        internal void AddDailySignature(ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, queueFr.GetDayTotals(), queueFr.GLastHash);
//            AddTotalsSignatureFRWithoutSign(receiptResponse, payload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);
//            if (!receiptRequest.HasTrainingReceiptFlag())
//            {
//                queueFr.ResetDailyTotalizers(queueItem);
//            }
//        }

//        internal void AddMonthlySignature(ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, queueFr.GetMonthTotals(), queueFr.GLastHash);
//            AddTotalsSignatureFRWithoutSign(receiptResponse, payload, "Month Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000004);
//            if (!receiptRequest.HasTrainingReceiptFlag())
//            {
//                queueFr.ResetMonthlyTotalizers(queueItem);
//            }
//        }

//        internal void AddYearlySignature(ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, queueFr.GetYearTotals(), queueFr.GLastHash);
//            AddTotalsSignatureFRWithoutSign(receiptResponse, payload, "Year Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000005);
//            if (!receiptRequest.HasTrainingReceiptFlag())
//            {
//                queueFr.ResetYearTotalizers(queueItem);
//            }
//        }

//        internal void AddShiftSignature(ftQueueFR queueFr, ftSignaturCreationUnitFR signaturCreationUnitFR, ftQueueItem queueItem, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            var payload = PayloadFactory.GetTicketPayload(receiptRequest, receiptResponse, signaturCreationUnitFR, queueFr.GetShiftTotals(), queueFr.GLastHash);
//            AddTotalsSignatureFRWithoutSign(receiptResponse, payload, "Shift Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000002);
//            if (!receiptRequest.HasTrainingReceiptFlag())
//            {
//                queueFr.ResetShiftTotalizer(queueItem);
//            }
//        }

//        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
//        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
//        internal void AddMessageSignatures(ref ftQueueFR queueFr, List<ftActionJournal> localActionJournals, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
//        {
//            if (queueFr.MessageCount > 0)
//            {
//                var localSignatures = new List<SignaturItem>(receiptResponse.ftSignatures);
//                if ((receiptRequest.ftReceiptCase & 0xFFFF) == 0x000F)
//                {
//                    ////return pending messages, add signatures to receiptresponse
//                    //var NowMoment = new DateTime(parentStorage.ActionJournalTimeStamp(queueFr.ftQueueFRId));
//                    ////find messages since messagemoment timestamp
//                    ////long MessageMomentTimeStamp = DateTime.UtcNow.Subtract(new TimeSpan(72, 0, 0)).Ticks;
//                    //long MessageMomentTimeStamp = 0;
//                    //if (queueFr.MessageMoment.HasValue)
//                    //{
//                    //    //should be always set when messagecount is set. when not all messages are shown?!
//                    //    MessageMomentTimeStamp = queueFr.MessageMoment.Value.Ticks;
//                    //}

//                    //foreach (var item in parentStorage.ActionJournalTableByTimeStamp(MessageMomentTimeStamp).Where(aj => aj.Priority < 0).ToArray().Concat(localActionJournals.Where(j => j.Priority < 0).ToArray()))
//                    //{
//                    //    var signaturItem = new SignaturItem()
//                    //    {
//                    //        Caption = item.Message,
//                    //        Data = item.DataJson
//                    //    };

//                    //    signaturItem.ftSignatureFormat = (long) SignaturItem.Formats.AZTEC;
//                    //    signaturItem.ftSignatureType = (long) SignaturItem.Types.AT_Unknown;

//                    //    localSignatures.Add(signaturItem);
//                    //}

//                    ////if it is not a training receit, the counter will be updated
//                    //if ((receiptRequest.ftReceiptCase & 0x0000000000020000) == 0)
//                    //{
//                    //    queueFr.MessageCount = parentStorage.ActionJournalTableByTimeStamp(NowMoment.Ticks).Count(aj => aj.Priority < 0) + localActionJournals.Count(j => j.TimeStamp >= NowMoment.Ticks && j.Priority < 0);
//                    //    if (queueFr.MessageCount == 0)
//                    //    {
//                    //        queueFr.MessageMoment = null;
//                    //    }
//                    //    else
//                    //    {
//                    //        queueFr.MessageMoment = NowMoment;
//                    //    }
//                    //}
//                    throw new NotImplementedException();
//                }
//                else
//                {
//                    receiptResponse.ftState |= 0x40;

//                    localSignatures.Add(new SignaturItem()
//                    {
//                        Caption = "fiskaltrust-Message pending",
//                        Data = "Create a Zero receipt",
//                        ftSignatureFormat = (long) SignaturItem.Formats.Text,
//                        ftSignatureType = (long) SignaturItem.Types.Information
//                    });
//                }

//                receiptResponse.ftSignatures = localSignatures.ToArray();
//            }
//        }
//    }
}
