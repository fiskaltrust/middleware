using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.me;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper
{
    public class InMemoryMESSCD : IMESSCD
    {
        private readonly string _tcrCodeOrFIC;

        public InMemoryMESSCD (string tcrCodeOrFIC)
        {
            _tcrCodeOrFIC = tcrCodeOrFIC;
        }
        public Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest) => throw new NotImplementedException();
        public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
        {
            return Task.FromResult(new RegisterInvoiceResponse()
            {
                FIC = _tcrCodeOrFIC
            });
        }
        public Task<RegisterTCRResponse> RegisterTCRAsync(RegisterTCRRequest registerTCRRequest)
        {
            return Task.FromResult(new RegisterTCRResponse()
            {
                TCRCode = _tcrCodeOrFIC
            });
        }
    }
}
