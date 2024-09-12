using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.v2
{
    public class Queue : IPOS
    {
        private readonly ISignProcessor _signProcessor;
        private readonly IJournalProcessor _journalProcessor;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        public Queue(ISignProcessor signProcessor, IJournalProcessor journalProcessor, MiddlewareConfiguration middlewareConfiguration)
        {
            _signProcessor = signProcessor;
            _journalProcessor = journalProcessor;
            _middlewareConfiguration = middlewareConfiguration;
        }

        public string Echo(string message) => message;

        private delegate string Echo_Delegate(string message);

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new Echo_Delegate(Echo);
            var r = d.BeginInvoke(message, callback, d);
            return r;
        }
        public string EndEcho(IAsyncResult result)
        {
            var d = (Echo_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        private delegate ifPOS.v0.ReceiptResponse Sign_Delegate(ifPOS.v0.ReceiptRequest request);
        public IAsyncResult BeginSign(ifPOS.v0.ReceiptRequest request, AsyncCallback callback, object state)
        {
            var d = new Sign_Delegate(Sign);
            var r = d.BeginInvoke(request, callback, d);
            return r;
        }
        public ifPOS.v0.ReceiptResponse EndSign(IAsyncResult result)
        {
            var d = (Sign_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public async Task<EchoResponse> EchoAsync(EchoRequest message) => await Task.FromResult(new EchoResponse
        {
            Message = message.Message
        }).ConfigureAwait(false);

        public Stream Journal(long ftJournalType, long from, long to)
        {
            var journalRequest = new JournalRequest
            {
                ftJournalType = ftJournalType,
                From = from,
                To = to,
                MaxChunkSize = _middlewareConfiguration.JournalChunkSize
            };
            var result = JournalAsync(journalRequest).ToListAsync().Result;
            return new MemoryStream(result.SelectMany(x => x.Chunk).ToArray());
        }

        private delegate Stream Journal_Delegate(long ftJournalType, long from, long to);
        public IAsyncResult BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state)
        {
            var d = new Journal_Delegate(Journal);
            var r = d.BeginInvoke(ftJournalType, from, to, callback, d);
            return r;
        }

        public Stream EndJournal(IAsyncResult result)
        {
            var d = (Journal_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public ifPOS.v0.ReceiptResponse Sign(ifPOS.v0.ReceiptRequest data) => ReceiptRequestHelper.ConvertToV0(Task.Run(() => _signProcessor.ProcessAsync(ReceiptRequestHelper.ConvertToV1(data)).Result).Result);

        public async Task<ReceiptResponse> SignAsync(ReceiptRequest request) => await _signProcessor.ProcessAsync(request).ConfigureAwait(false);

        public IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request) => _journalProcessor.ProcessAsync(request);
    }
}
