using fiskaltrust.Middleware.Interface.Client.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Soap.Helpers
{
    public class AsyncPOSHelper : ifPOS.v1.IPOS
    {
        private readonly ifPOS.v1.IPOS _innerPOS;
        public AsyncPOSHelper(ifPOS.v1.IPOS innerPOS)
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

        public Stream Journal(long ftJournalType, long from, long to) => _innerPOS.Journal(ftJournalType, from, to);

        public Task<ifPOS.v1.ReceiptResponse> SignAsync(ifPOS.v1.ReceiptRequest request) => Task.Run(() =>
        {
            var response = _innerPOS.Sign(request.Into());
            return response.Into();
        });

        public IAsyncEnumerable<ifPOS.v1.JournalResponse> JournalAsync(ifPOS.v1.JournalRequest request)
        {
            var stream = _innerPOS.Journal(request.ftJournalType, request.From, request.To);
            return stream.ToAsyncEnumerable(request.MaxChunkSize);
        }

        public Task<ifPOS.v1.EchoResponse> EchoAsync(ifPOS.v1.EchoRequest message) => Task.Run(() => _innerPOS.EchoAsync(message));
    }
}
