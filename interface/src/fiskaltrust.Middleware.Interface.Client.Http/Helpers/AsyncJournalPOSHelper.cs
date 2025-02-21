using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http.Helpers
{
    public class AsyncJournalPOSHelper : IPOS
    {
        private readonly IPOS _innerPOS;
        public AsyncJournalPOSHelper(IPOS innerPOS)
        {
            _innerPOS = innerPOS;
        }

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state) => _innerPOS.BeginEcho(message, callback, state);

        public IAsyncResult BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state) => _innerPOS.BeginJournal(ftJournalType, from, to, callback, state);

        public IAsyncResult BeginSign(ifPOS.v0.ReceiptRequest data, AsyncCallback callback, object state) => _innerPOS.BeginSign(data, callback, state);

        public string Echo(string message) => _innerPOS.Echo(message);

        public string EndEcho(IAsyncResult result) => _innerPOS.EndEcho(result);

        public Stream EndJournal(IAsyncResult result) => _innerPOS.EndJournal(result);

        public ifPOS.v0.ReceiptResponse EndSign(IAsyncResult result) => _innerPOS.EndSign(result);

        public ifPOS.v0.ReceiptResponse Sign(ifPOS.v0.ReceiptRequest data) => _innerPOS.Sign(data);

        public Task<ifPOS.v1.ReceiptResponse> SignAsync(ifPOS.v1.ReceiptRequest request) => _innerPOS.SignAsync(request);

        public Stream Journal(long ftJournalType, long from, long to) => _innerPOS.Journal(ftJournalType, from, to);

        public IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request)
        {
            var stream = _innerPOS.Journal(request.ftJournalType, request.From, request.To);
            return stream.ToAsyncEnumerable(request.MaxChunkSize);
        }

        public Task<EchoResponse> EchoAsync(EchoRequest message) => _innerPOS.EchoAsync(message);
    }
}
