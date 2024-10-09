using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Extensions;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.serialization.AT.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    public abstract class RequestCommand
    {
        private const string JSWS_PAYLOAD_COUNTER_TRAINING = "VFJB";
        private const string JWS_PAYLOAD_COUNTER_STORNO = "U1RP";
        private const string SSCD_JWS_HEADER = "eyJhbGciOiJFUzI1NiJ9";
        private const string SSCD_JWS_SIGNATURE_FAILED = "U2ljaGVyaGVpdHNlaW5yaWNodHVuZyBhdXNnZWZhbGxlbg";

        private readonly IATSSCDProvider _sscdProvider;
        protected readonly MiddlewareConfiguration _middlewareConfiguration;
        protected readonly QueueATConfiguration _queueATConfiguration;
        protected readonly ILogger<RequestCommand> _logger;

        public RequestCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger )
        {
            _sscdProvider = sscdProvider;
            _middlewareConfiguration = middlewareConfiguration;
            _queueATConfiguration = queueATConfiguration;
            _logger = logger;
        }

        public abstract string ReceiptName { get; }

        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueDE, ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse response);

        public static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem, ftQueueAT queueAT, ftQueue queue)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftCashBoxIdentification = queueAT.CashBoxIdentification,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4154000000000000,
                ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#",
                ftSignatures = Array.Empty<SignaturItem>()
            };
        }

        protected static ftActionJournal CreateActionJournal(ftQueue queue, ftQueueItem queueItem, string message, string dataJson = null)
        {
            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                Message = message,
                DataJson = dataJson
            };
        }

        protected static string CreateLastReceiptSignature(string data)
        {
            using var sha256 = SHA256.Create();
            var input = Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(input);

            return Convert.ToBase64String(hash, 0, 8);
        }

        protected void ThrowIfTraining(ReceiptRequest request)
        {
            if (request.HasTrainingReceiptFlag())
            {
                throw new ArgumentException($"Receipt case 0x{request.ftReceiptCase:X} can not use training mode flag.");
            }
        }

#pragma warning disable IDE0060
        protected void IncrementMessageCount(ref ftQueueAT queueAT)
        {
            // Not needed anymore, as the database is now only updated when a request finishes
            // Previously, this mechanism was used to ensure that notifications were always included, even when the signing process failed during execution.
            // Kept here temporary for future reference.
            
            //queueAT.MessageCount++;
            //if (!queueAT.MessageMoment.HasValue)
            //{
            //    queueAT.MessageMoment = DateTime.UtcNow;
            //}
        }
#pragma warning restore IDE0060

        protected async Task<(string receiptIdentification, string ftStateData, bool isBackupScuUsed, List<SignaturItem> signatureItems, ftJournalAT journalAT)> SignReceiptAsync(ftQueueAT queueAT, ReceiptRequest receiptRequest, string ftReceiptIdentification, DateTime ftReceiptMoment, Guid ftQueueItemId, bool isZeroReceipt = false)
        {
            var signatures = new List<SignaturItem>();
            string ftStateData = null;
            var isBackupScuUsed = false;

            var rc_Ausfall_Nacherfassung = (receiptRequest.ftReceiptCase & 0x010000) == 0x010000;
            var rc_Training = (receiptRequest.ftReceiptCase & 0x020000) == 0x020000;
            var rc_Belegstorno = (receiptRequest.ftReceiptCase & 0x040000) == 0x040000;
            var rc_Handschrift_Nacherfassung = (receiptRequest.ftReceiptCase & 0x080000) == 0x080000;
            var rc_Kleinunternehmer = (receiptRequest.ftReceiptCase & 0x100000) == 0x100000;
            var rc_B2B = (receiptRequest.ftReceiptCase & 0x200000) == 0x200000;
            var rc_UStG_Rechnung = (receiptRequest.ftReceiptCase & 0x400000) == 0x400000;

            var decision = new AddSignatureDecision
            {
                Number = -1,
                Exception = string.Empty,
                Signing = false,
                Counting = false,
                ZeroReceipt = isZeroReceipt
            };

            decimal? satz_Normal = null;
            decimal? satz_Erm1 = null;
            decimal? satz_Erm2 = null;
            decimal? satz_Besonders = null;
            decimal? satz_Null = null;

            decimal? satz_Null_keinUmsatz = null;
            decimal? satz_Normal_keinUmsatz = null;
            decimal? satz_Erm1_keinUmsatz = null;
            decimal? satz_Erm2_keinUmsatz = null;
            decimal? satz_Besonders_keinUmsatz = null;

            decimal? satz_Null_Verbindlichkeiten = null;



            long[] decisionBit5_Values = { 0, 1, 2, 3, 4, 5, 6 };
            var decisionBit5 = decisionBit5_Values.Contains(0xFFFF & receiptRequest.ftReceiptCase);
            var decisionBit4 = rc_Ausfall_Nacherfassung | rc_Handschrift_Nacherfassung;
            var decisionBit3 = rc_Training;

            if (receiptRequest?.cbChargeItems != null && receiptRequest.cbChargeItems.Length > 0)
            {
                foreach (var chargeItem in receiptRequest.cbChargeItems.Where(ci => (ci.ftChargeItemCase & 0xFFFF) == 0))
                {
                    chargeItem.ftChargeItemCase += chargeItem.VATRate switch
                    {
                        10.0m => 0x1,
                        13.0m => 0x2,
                        20.0m => 0x3,
                        0.0m => 0x5,
                        _ => (long) 0x4,
                    };
                }
            }

            long[] satz_Normal_Values = { 0, 3, 10, 15, 20, 25, 30 };
            var satz_Normal_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Normal_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Normal_Query?.Count() > 0)
            {
                satz_Normal = satz_Normal_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Erm1_Values = { 1, 8, 13, 18, 23, 28 };
            var satz_Erm1_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Erm1_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Erm1_Query?.Count() > 0)
            {
                satz_Erm1 = satz_Erm1_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Erm2_Values = { 2, 9, 14, 19, 24, 29 };
            var satz_Erm2_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Erm2_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Erm2_Query?.Count() > 0)
            {
                satz_Erm2 = satz_Erm2_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Besonders_Values = { 4, 11, 16, 21, 26, 31 };
            var satz_Besonders_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Besonders_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Besonders_Query?.Count() > 0)
            {
                satz_Besonders = satz_Besonders_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Null_Values = { 5, 6, 12, 17, 22, 27, 32 };
            var satz_Null_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Null_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Null_Query?.Count() > 0)
            {
                satz_Null = satz_Null_Query.Sum(ci => ci.Amount);
            }

            //Kein Umsatz (=Verbindlichkeit ohne RKSV-Pflicht) kann auf verschieden Steuersätze aufgeteilt werden und wird nach der Entscheidung zu den einzelnen Beträgen hinzugezählt.
            long[] satz_Null_keinUmsatz_Values = { 7, 33, 35 };
            var satz_Null_keinUmsatz_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Null_keinUmsatz_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Null_keinUmsatz_Query?.Count() > 0)
            {
                satz_Null_keinUmsatz = satz_Null_keinUmsatz_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Normal_keinUmsatz_Values = { 38 };
            var satz_Normal_keinUmsatz_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Normal_keinUmsatz_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Normal_keinUmsatz_Query?.Count() > 0)
            {
                satz_Normal_keinUmsatz = satz_Normal_keinUmsatz_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Erm1_keinUmsatz_Values = { 36 };
            var satz_Erm1_keinUmsatz_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Erm1_keinUmsatz_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Erm1_keinUmsatz_Query?.Count() > 0)
            {
                satz_Erm1_keinUmsatz = satz_Erm1_keinUmsatz_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Erm2_keinUmsatz_Values = { 37 };
            var satz_Erm2_keinUmsatz_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Erm2_keinUmsatz_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Erm2_keinUmsatz_Query?.Count() > 0)
            {
                satz_Erm2_keinUmsatz = satz_Erm2_keinUmsatz_Query.Sum(ci => ci.Amount);
            }

            long[] satz_Besonders_keinUmsatz_Values = { 39 };
            var satz_Besonders_keinUmsatz_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Besonders_keinUmsatz_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Besonders_keinUmsatz_Query?.Count() > 0)
            {
                satz_Besonders_keinUmsatz = satz_Besonders_keinUmsatz_Query.Sum(ci => ci.Amount);
            }

            //Verbindlichkeit mit RKSV-Pflicht #34 wird wie ein Zahlungsmittel mit RKSV-Pflicht gehandhabt, daher setzt es in der Entscheidungstabelle Bit0 (RKSV-Barzahlung) auf "JA"
            //Um auf jeden Fall eine Signierung herbeizuführen wird auch das Bit2 (Umsatz) auf "JA" gesetzt
            long[] satz_Null_Verbindlichkeiten_Values = { 34 };
            var satz_Null_Verbindlichkeiten_Query = receiptRequest?.cbChargeItems?.Where(ci => satz_Null_Verbindlichkeiten_Values.Contains(ci.ftChargeItemCase & 0xFFFF));
            if (satz_Null_Verbindlichkeiten_Query?.Count() > 0)
            {
                satz_Null_Verbindlichkeiten = satz_Null_Verbindlichkeiten_Query.Sum(ci => ci.Amount);
            }

            var decisionBit2 = satz_Normal.HasValue | satz_Erm1.HasValue | satz_Erm2.HasValue | satz_Besonders.HasValue | satz_Null.HasValue | satz_Null_Verbindlichkeiten.HasValue;

            var decisionBit1 = satz_Null_keinUmsatz.HasValue | satz_Normal_keinUmsatz.HasValue | satz_Erm1_keinUmsatz.HasValue | satz_Erm2_keinUmsatz.HasValue | satz_Besonders_keinUmsatz.HasValue;

            long[] DecisionBit0_Values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var decisionBit0 = receiptRequest?.cbPayItems?.Count(pi => DecisionBit0_Values.Contains(0xFFFF & pi.ftPayItemCase)) > 0 | satz_Null_Verbindlichkeiten.HasValue;


            var betrag_Normal = satz_Normal.GetValueOrDefault() + satz_Normal_keinUmsatz.GetValueOrDefault();
            var betrag_Erm1 = satz_Erm1.GetValueOrDefault() + satz_Erm1_keinUmsatz.GetValueOrDefault();
            var betrag_Erm2 = satz_Erm2.GetValueOrDefault() + satz_Erm2_keinUmsatz.GetValueOrDefault();
            var betrag_Null = satz_Null.GetValueOrDefault() + satz_Null_keinUmsatz.GetValueOrDefault() + satz_Null_Verbindlichkeiten.GetValueOrDefault();
            var betrag_Besonders = satz_Besonders.GetValueOrDefault() + satz_Besonders_keinUmsatz.GetValueOrDefault();


            var requiresCounting = false;
            var requiresSigning = false;

            if (isZeroReceipt || queueAT.SignAll)
            {
                requiresSigning = true;
                //Add everything to Counter except in Training-Mode
                if (!rc_Training)
                {
                    requiresCounting = true;
                }

                if (_middlewareConfiguration.IsSandbox && queueAT.SignAll)
                {
                    signatures.Add(new SignaturItem() { Caption = "Sign-All-Mode", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                }

                if (_middlewareConfiguration.IsSandbox && isZeroReceipt)
                {
                    signatures.Add(new SignaturItem() { Caption = "Zero-Receipt", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                }
            }
            else
            {

                var decisionNumerical = 0;
                if (decisionBit0)
                {
                    decisionNumerical += 1;
                }

                if (decisionBit1)
                {
                    decisionNumerical += 2;
                }

                if (decisionBit2)
                {
                    decisionNumerical += 4;
                }

                if (decisionBit3)
                {
                    decisionNumerical += 8;
                }

                if (decisionBit4)
                {
                    decisionNumerical += 16;
                }

                if (decisionBit5)
                {
                    decisionNumerical += 32;
                }

                // Decision Matrix Process
                int[] signingValues = { 8, 9, 10, 11, 12, 13, 14, 15, 24, 25, 26, 27, 28, 29, 30, 31, 37, 39, 40, 41, 42, 43, 44, 45, 46, 47, 53, 55, 56, 57, 58, 59, 60, 61, 62, 63 };
                int[] countingValues = { 37, 39, 53, 55 };

                if (signingValues.Contains(decisionNumerical))
                {
                    requiresSigning = true;
                }

                if (countingValues.Contains(decisionNumerical))
                {
                    requiresCounting = true;
                }

                if (_middlewareConfiguration.IsSandbox)
                {
                    signatures.Add(new SignaturItem() { Caption = $"Decision {decisionNumerical}", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                }

                decision.Number = decisionNumerical;


                if (rc_Belegstorno)
                {
                    //reverse turnover for exception decision
                    foreach (var item in receiptRequest.cbChargeItems)
                    {
                        item.Amount = (-1m) * item.Amount;
                        if (item.VATAmount.HasValue)
                        {
                            item.VATAmount = (-1m) * item.VATAmount;
                        }
                    }
                    foreach (var item in receiptRequest.cbPayItems)
                    {
                        item.Amount = (-1m) * item.Amount;
                    }
                }

                //Exception Matrix Process
                //A1 ??? Mehrwertsteuertausch
                if (receiptRequest.cbPayItems == null || receiptRequest.cbPayItems.Count() == 0)
                {
                    decision.Exception += "A1";

                    if (receiptRequest.cbChargeItems != null & !receiptRequest.IsProtocolReceipt())
                    {
                        var Query = receiptRequest.cbChargeItems?.Where(ci =>
                             satz_Normal_Values.Contains(ci.ftChargeItemCase & 0xFFFF) ||
                             satz_Erm1_Values.Contains(ci.ftChargeItemCase & 0xFFFF) ||
                             satz_Erm2_Values.Contains(ci.ftChargeItemCase & 0xFFFF) ||
                             satz_Besonders_Values.Contains(ci.ftChargeItemCase & 0xFFFF) ||
                              satz_Null_Values.Contains(ci.ftChargeItemCase & 0xFFFF)
                            );
                        if (Query.Sum(ci => ci.Amount) != 0m || (Query.Where(ci => ci.VATAmount.HasValue).Sum(ci => ci.VATAmount) + Query.Where(ci => !ci.VATAmount.HasValue).Sum(ci => ci.VATRate / 100m * ci.Amount)) != 0m)
                        {
                            requiresSigning = true;
                            if (!rc_Training)
                            {
                                requiresCounting = true;
                            }

                            if (_middlewareConfiguration.IsSandbox)
                            {
                                signatures.Add(new SignaturItem() { Caption = $"Exception A1", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                            }
                        }
                    }
                }
                //A2 Zahlung einer Ausgangsrechnung -> RKSV-Barzahlung und Negativer Debitor (ftPayItemCase==11)
                if (decisionBit0 &&
                    receiptRequest.cbPayItems?.Count(pi => ((0xFFFF & pi.ftPayItemCase) == 11) &&
                    (pi.Amount < 0m)) > 0)
                {
                    decision.Exception += "A2";

                    requiresSigning = true;
                    if (!rc_Training)
                    {
                        requiresCounting = true;
                    }

                    if (_middlewareConfiguration.IsSandbox)
                    {
                        signatures.Add(new SignaturItem() { Caption = $"Exception A2", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                    }
                }

                //A3 Zahlung einer Ausgangsrechnung -> RKSV-Barzahlung und RKSV-Pflichtige Verbindlichkeit bei 
                if (decisionBit0 &&
                    receiptRequest.cbChargeItems?.Count(ci => ((0xFFFF & ci.ftChargeItemCase) == 34) && (ci.Amount > 0m)) > 0 &&
                    ((0xFFFF & receiptRequest.ftReceiptCase) == 8 || (0xFFFF & receiptRequest.ftReceiptCase) == 10))
                {
                    decision.Exception += "A3";

                    requiresSigning = true;
                    if (!rc_Training)
                    {
                        requiresCounting = true;
                    }

                    if (_middlewareConfiguration.IsSandbox)
                    {
                        signatures.Add(new SignaturItem() { Caption = $"Exception A3", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                    }
                }

                //A4 Zahlung einer Ausgangsrechnung -> 
                if (decisionBit0 &&
                    (0xFFFF & receiptRequest.ftReceiptCase) == 8 &&
                    receiptRequest?.cbPayItems?.Count(pi => DecisionBit0_Values.Contains(0xFFFF & pi.ftPayItemCase) && pi.Amount > 0) > 0)
                {
                    decision.Exception += "A4";

                    requiresSigning = true;
                    if (!rc_Training)
                    {
                        requiresCounting = true;
                    }

                    if (_middlewareConfiguration.IsSandbox)
                    {
                        signatures.Add(new SignaturItem() { Caption = $"Exception A4", Data = $"Sign: {requiresSigning} Counter:{requiresCounting}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                    }
                }


                if (rc_Belegstorno)
                {
                    //reverse turnover after exception decision
                    foreach (var item in receiptRequest.cbChargeItems)
                    {
                        item.Amount = (-1m) * item.Amount;
                        if (item.VATAmount.HasValue)
                        {
                            item.VATAmount = (-1m) * item.VATAmount;
                        }
                    }
                    foreach (var item in receiptRequest.cbPayItems)
                    {
                        item.Amount = (-1m) * item.Amount;
                    }

                    decision.Exception += "S";
                }
            }

            if (_middlewareConfiguration.IsSandbox)
            {
                decision.Signing = requiresSigning;
                decision.Counting = requiresCounting;
                ftStateData = JsonConvert.SerializeObject(decision);
            }

            var totalizer = 0m;
            if (requiresCounting)
            {
                totalizer += betrag_Normal + betrag_Erm1 + betrag_Erm2 + betrag_Besonders + betrag_Null;
                totalizer = Math.Round(totalizer, 2);

                if (_middlewareConfiguration.IsSandbox)
                {
                    signatures.Add(new SignaturItem() { Caption = $"Counter Add", Data = $"{totalizer}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_Unknown });
                }
            }

            if (isZeroReceipt || requiresSigning)
            {
                var cashNumerator = queueAT.ftCashNumerator + 1;
                var receiptIdentification = $"{ftReceiptIdentification}{cashNumerator}";

                var cashTotalizerDecimal = queueAT.ftCashTotalizer + totalizer;
                var encryptedTotalizer = Convert.ToBase64String(TotalizerEncryptionHelper.EncryptTotalizer(queueAT.CashBoxIdentification, receiptIdentification, queueAT.EncryptionKeyBase64, cashTotalizerDecimal));

                if (rc_Training)
                {
                    encryptedTotalizer = JSWS_PAYLOAD_COUNTER_TRAINING;
                }
                else if (rc_Belegstorno)
                {
                    encryptedTotalizer = JWS_PAYLOAD_COUNTER_STORNO;
                }

                var journalAT = new ftJournalAT
                {
                    ftJournalATId = Guid.NewGuid(),
                    ftQueueId = queueAT.ftQueueATId,
                    Number = cashNumerator
                };

                if (isZeroReceipt || queueAT.SSCDFailCount == 0)
                {
                    if(!(await _sscdProvider.GetAllInstances()).Any())
                    {
                        throw new Exception("No SCU is connected to this Queue.");
                    }

                    if (isZeroReceipt)
                    {
                        _sscdProvider.SwitchToFirstScu();
                    }

                    var scus = await _sscdProvider.GetAllInstances();
                    var retry = 0;

                    do
                    {
                        var (scu, sscd, startIndex) = await _sscdProvider.GetCurrentlyActiveInstanceAsync();
                        var currentIndex = startIndex;
                        do
                        {
                            // Skip SCU if using a backup SCU on the first retry, and the previously used SCU was not a backup one
                            // TODO Clarify why this is required
                            if (retry == 0 && scu.IsBackup() && scus.Any(x => !x.IsBackup()) && !scus[startIndex].IsBackup())
                            {
                                currentIndex = _sscdProvider.SwitchToNextScu();
                                continue;
                            }

                            isBackupScuUsed = scu.IsBackup();
                                

                            try
                            {
                                var zda = (await sscd.ZdaAsync()).ZDA;
                                var certResponse = await sscd.CertificateAsync();
                                var cert = new X509CertificateParser().ReadCertificate(certResponse.Certificate);
                                var certificateSerialNumber = cert.SerialNumber.ToString(16);

                                var sb2 = new StringBuilder();
                                sb2.Append("_R1-");
                                sb2.Append(zda);
                                sb2.Append("_");
                                sb2.Append(queueAT.CashBoxIdentification);
                                sb2.Append("_");
                                sb2.Append(receiptIdentification);
                                sb2.Append("_");
                                sb2.AppendFormat("{0:yyyy-MM-ddTHH:mm:ss}_", ftReceiptMoment.ToLocalTime());
                                sb2.AppendFormat(System.Globalization.CultureInfo.CreateSpecificCulture("de-AT"), "{0:0.00}_{1:0.00}_{2:0.00}_{3:0.00}_{4:0.00}_", betrag_Normal, betrag_Erm1, betrag_Erm2, betrag_Null, betrag_Besonders);
                                sb2.Append(encryptedTotalizer);
                                sb2.Append("_");
                                sb2.Append(certificateSerialNumber);
                                sb2.Append("_");
                                sb2.Append(queueAT.LastSignatureHash);

                                var jwsPayload = sb2.ToString();


                                journalAT.JWSHeaderBase64url = SSCD_JWS_HEADER;
                                journalAT.JWSPayloadBase64url = ConversionHelper.ToBase64UrlString(Encoding.UTF8.GetBytes(jwsPayload));
                                var jwsDataToSign = Encoding.UTF8.GetBytes($"{journalAT.JWSHeaderBase64url}.{journalAT.JWSPayloadBase64url}");


                                var jwsSignature = await sscd.SignAsync(new SignRequest() {Data = jwsDataToSign });

                                journalAT.JWSSignatureBase64url = ConversionHelper.ToBase64UrlString(jwsSignature.SignedData);
                                journalAT.ftSignaturCreationUnitId = scu.ftSignaturCreationUnitATId;

                                queueAT.ftCashNumerator = cashNumerator;
                                queueAT.ftCashTotalizer += Math.Round(totalizer, 2);
                                queueAT.LastSignatureHash = CreateLastReceiptSignature($"{journalAT.JWSHeaderBase64url}.{journalAT.JWSPayloadBase64url}.{journalAT.JWSSignatureBase64url}");
                                queueAT.LastSignatureZDA = zda;
                                queueAT.LastSignatureCertificateSerialNumber = certificateSerialNumber;


                                signatures.Add(new SignaturItem() { Caption = "www.fiskaltrust.at", Data = $"{jwsPayload}_{Convert.ToBase64String(jwsSignature.SignedData)}", ftSignatureFormat = (long) SignaturItem.Formats.QR_Code, ftSignatureType = (long) SignaturItem.Types.AT_RKSV });

                                return (receiptIdentification, ftStateData, isBackupScuUsed, signatures, journalAT);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An error occured while trying to sign a receipt with the SCU {ScuId}.", scu.ftSignaturCreationUnitATId);
                            }

                            currentIndex = _sscdProvider.SwitchToNextScu();
                        } while (currentIndex != startIndex);
                    } while (retry++ < _queueATConfiguration.ScuMaxRetries);
                }


                if (queueAT.SSCDFailCount == 0)
                {
                    queueAT.SSCDFailMoment = DateTime.UtcNow;
                    queueAT.SSCDFailQueueItemId = ftQueueItemId;
                }
                queueAT.SSCDFailCount++;

                var sb = new StringBuilder();
                sb.Append("_R1-");
                sb.Append(queueAT.LastSignatureZDA);
                sb.Append("_");
                sb.Append(queueAT.CashBoxIdentification);
                sb.Append("_");
                sb.Append(receiptIdentification);
                sb.Append("_");
                sb.AppendFormat("{0:yyyy-MM-ddTHH:mm:ss}_", ftReceiptMoment.ToLocalTime());
                sb.AppendFormat(System.Globalization.CultureInfo.CreateSpecificCulture("de-AT"), "{0:0.00}_{1:0.00}_{2:0.00}_{3:0.00}_{4:0.00}_", betrag_Normal, betrag_Erm1, betrag_Erm2, betrag_Null, betrag_Besonders);
                sb.Append(encryptedTotalizer);
                sb.Append("_");
                sb.Append(queueAT.LastSignatureCertificateSerialNumber);
                sb.Append("_");
                sb.Append(queueAT.LastSignatureHash);

                var failedReceiptJwsPayload = sb.ToString();

                journalAT.JWSHeaderBase64url = SSCD_JWS_HEADER;
                journalAT.JWSPayloadBase64url = ConversionHelper.ToBase64UrlString(Encoding.UTF8.GetBytes(failedReceiptJwsPayload));
                journalAT.JWSSignatureBase64url = SSCD_JWS_SIGNATURE_FAILED;
                journalAT.ftSignaturCreationUnitId = Guid.Empty;

                queueAT.ftCashNumerator = cashNumerator;
                queueAT.ftCashTotalizer += Math.Round(totalizer, 2);
                queueAT.LastSignatureHash = CreateLastReceiptSignature($"{journalAT.JWSHeaderBase64url}.{journalAT.JWSPayloadBase64url}.{journalAT.JWSSignatureBase64url}");

                signatures.Add(new SignaturItem() { Caption = $"Sicherheitseinrichtung ausgefallen", Data = $"{failedReceiptJwsPayload}_{Convert.ToBase64String(ConversionHelper.FromBase64UrlString(SSCD_JWS_SIGNATURE_FAILED))}", ftSignatureFormat = (long) SignaturItem.Formats.QR_Code, ftSignatureType = (long) SignaturItem.Types.AT_RKSV });

                return (receiptIdentification, ftStateData, isBackupScuUsed, signatures, journalAT);
            }
            else
            {
                return (ftReceiptIdentification, ftStateData, isBackupScuUsed, signatures, null);
            }
        }

        
        // TODO: Refactor and put directly into the places where we are generating the action journals.
        // This was taken from the previous service code for simplicity reasons for now.
        protected IEnumerable<SignaturItem> CreateNotificationSignatures(List<ftActionJournal> actionJournals)
        {
            foreach (var item in actionJournals.Where(aj => aj.Priority < 0))
            {
                if (item.Type.StartsWith($"0x{SignaturItem.Types.AT_FinanzOnline:x}-"))
                {
                    var type = item.Type.Split('-')[1];
                    if (type == nameof(FonActivateQueue))
                    {
                        var ajData = JsonConvert.DeserializeObject<FonActivateQueue>(item.DataJson);
                        var qrData = new FonActivateQueue
                        {
                            CashBoxId = ajData.CashBoxId,
                            QueueId = ajData.QueueId,
                            Moment = ajData.Moment,
                            DEPValue = string.Empty,
                            IsStartReceipt = ajData.IsStartReceipt,
                            Version = ajData.Version,
                            CashBoxIdentification = ajData.CashBoxIdentification
                        };
                        yield return new SignaturItem
                        {
                            Caption = item.Message,
                            Data = JsonConvert.SerializeObject(new { ActionJournalId = item.ftActionJournalId, Type = item.Type, Data = qrData }),
                            ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                            ftSignatureType = (long) SignaturItem.Types.AT_FinanzOnline
                        };
                    }
                    else if (type == nameof(FonDeactivateQueue))
                    {
                        var ajData = JsonConvert.DeserializeObject<FonDeactivateQueue>(item.DataJson);
                        var qrData = new FonDeactivateQueue
                        {
                            CashBoxId = ajData.CashBoxId,
                            QueueId = ajData.QueueId,
                            Moment = ajData.Moment,
                            DEPValue = string.Empty,
                            IsStopReceipt = ajData.IsStopReceipt,
                            Version = ajData.Version,
                            CashBoxIdentification = ajData.CashBoxIdentification
                        };
                        yield return new SignaturItem
                        {
                            Caption = item.Message,
                            Data = JsonConvert.SerializeObject(new { ActionJournalId = item.ftActionJournalId, Type = item.Type, Data = qrData }),
                            ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                            ftSignatureType = (long) SignaturItem.Types.AT_FinanzOnline
                        };
                    }
                    else if (type == nameof(FonActivateSCU))
                    {
                        var ajData = JsonConvert.DeserializeObject<FonActivateSCU>(item.DataJson);
                        var qrData = new FonActivateSCU
                        {
                            CashBoxId = ajData.CashBoxId,
                            SCUId = ajData.SCUId,
                            Moment = ajData.Moment,
                            PackageName = ajData.PackageName,
                            VDA = ajData.VDA,
                            SerialNumber = ajData.SerialNumber,
                            Version = ajData.Version
                        };
                        yield return new SignaturItem
                        {
                            Caption = item.Message,
                            Data = JsonConvert.SerializeObject(new { ActionJournalId = item.ftActionJournalId, Type = item.Type, Data = qrData }),
                            ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                            ftSignatureType = (long) SignaturItem.Types.AT_FinanzOnline
                        };
                    }
                    else if (type == nameof(FonDeactivateSCU))
                    {
                        var ajData = JsonConvert.DeserializeObject<FonDeactivateSCU>(item.DataJson);
                        var qrData = new FonDeactivateSCU
                        {
                            CashBoxId = ajData.CashBoxId,
                            SCUId = ajData.SCUId,
                            Moment = ajData.Moment,
                            PackageName = ajData.PackageName,
                            VDA = ajData.VDA,
                            SerialNumber = ajData.SerialNumber,
                            Version = ajData.Version
                        };
                        yield return new SignaturItem
                        {
                            Caption = item.Message,
                            Data = JsonConvert.SerializeObject(new { ActionJournalId = item.ftActionJournalId, Type = item.Type, Data = qrData }),
                            ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                            ftSignatureType = (long) SignaturItem.Types.AT_FinanzOnline
                        };
                    }
                    else if (type == nameof(FonVerifySignature))
                    {
                        var ajData = JsonConvert.DeserializeObject<FonVerifySignature>(item.DataJson);
                        var qrData = new FonVerifySignature
                        {
                            CashBoxId = ajData.CashBoxId,
                            CashBoxIdentification = ajData.CashBoxIdentification,
                            QueueId = ajData.QueueId,
                            SCUId = ajData.SCUId,
                            DEPValue = ajData.DEPValue
                        };
                        yield return new SignaturItem
                        {
                            Caption = item.Message,
                            Data = JsonConvert.SerializeObject(new { ActionJournalId = item.ftActionJournalId, Type = item.Type, Data = qrData }),
                            ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                            ftSignatureType = (long) SignaturItem.Types.AT_FinanzOnline
                        };
                    }
                    else
                    {
                        yield return new SignaturItem
                        {
                            Caption = item.Message,
                            Data = item.DataJson,
                            ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                            ftSignatureType = (long) SignaturItem.Types.AT_Unknown
                        };
                    }
                }
                else
                {
                    yield return new SignaturItem
                    {
                        Caption = item.Message,
                        Data = item.DataJson,
                        ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                        ftSignatureType = (long) SignaturItem.Types.AT_Unknown
                    };
                }
            }
        }
    }
}