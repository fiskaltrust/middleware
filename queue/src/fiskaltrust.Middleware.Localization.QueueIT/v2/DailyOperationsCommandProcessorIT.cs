using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class DailyOperationsCommandProcessorIT
    {
        private readonly IJournalITRepository _journalITRepository;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IITSSCDProvider _itSSCDProvider;

        public DailyOperationsCommandProcessorIT(IITSSCDProvider itSSCDProvider, IJournalITRepository journalITRepository, IConfigurationRepository configurationRepository)
        {
            _itSSCDProvider = itSSCDProvider;
            _journalITRepository = journalITRepository;
            _configurationRepository = configurationRepository;
        }

        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = (request.ReceiptRequest.ftReceiptCase & 0xFFFF);
            if (receiptCase == (int) ReceiptCases.ZeroReceipt0x2000)
                return await ZeroReceipt0x2000Async(request);

            if (receiptCase == (int) ReceiptCases.OneReceipt0x2001)
                return await OneReceipt0x2001Async(request);

            if (receiptCase == (int) ReceiptCases.ShiftClosing0x2010)
                return await ShiftClosing0x2010Async(request);

            if (receiptCase == (int) ReceiptCases.DailyClosing0x2011)
                return await DailyClosing0x2011Async(request);

            if (receiptCase == (int) ReceiptCases.MonthlyClosing0x2012)
                return await MonthlyClosing0x2012Async(request);

            if (receiptCase == (int) ReceiptCases.YearlyClosing0x2013)
                return await YearlyClosing0x2013Async(request);

            request.ReceiptResponse.SetReceiptResponseError($"The given ReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request)
        {
            var (queue, queueIT, receiptRequest, receiptResponse, queueItem) = request;
            if (queueIT.SSCDFailCount != 0)
            {
                queueIT.SSCDFailCount = 0;
                queueIT.SSCDFailMoment = null;
                queueIT.SSCDFailQueueItemId = null;
                await _configurationRepository.InsertOrUpdateQueueITAsync(queueIT).ConfigureAwait(false);
            }

            var establishConnection = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            });
            if (establishConnection.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(establishConnection.ReceiptResponse, new List<ftActionJournal>());
            }

            if (establishConnection.ReceiptResponse.ftState == 0x4954_2001_0000_0000)
            {
                return new ProcessCommandResponse(establishConnection.ReceiptResponse, new List<ftActionJournal>());
            }
            return new ProcessCommandResponse(establishConnection.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

        public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            var actionJournalEntry = ftActionJournalFactory.CreateDailyClosingActionJournal(queue, queueItem, receiptRequest);
            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            });
            if (result.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }

            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
            receiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";
            receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;

            var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIt, new ScuResponse()
            {
                ftReceiptCase = receiptRequest.ftReceiptCase,
                ZRepNumber = long.Parse(zNumber)
            });
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);

            return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>
                {
                    actionJournalEntry
                });
        }

        public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            var actionJournalEntry = ftActionJournalFactory.CreateMonthlyClosingActionJournal(queue, queueItem, receiptRequest);
            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            });
            if (result.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }

            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
            receiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";
            receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;

            var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIt, new ScuResponse()
            {
                ftReceiptCase = receiptRequest.ftReceiptCase,
                ZRepNumber = long.Parse(zNumber)
            });
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);

            return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>
                {
                    actionJournalEntry
                });
        }

        public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;

            var actionJournalEntry = ftActionJournalFactory.CreateYearlyClosingClosingActionJournal(queue, queueItem, receiptRequest);
            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            });
            if (result.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }

            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
            receiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";
            receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;
            var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIt, new ScuResponse()
            {
                ftReceiptCase = receiptRequest.ftReceiptCase,
                ZRepNumber = long.Parse(zNumber)
            });
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
            return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>
                {
                    actionJournalEntry
                });
        }
    }
}