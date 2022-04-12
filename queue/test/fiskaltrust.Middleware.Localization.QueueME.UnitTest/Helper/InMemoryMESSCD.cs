using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.me;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper
{
    public class InMemoryMESSCD : IMESSCD
    {
        private readonly string _tcrCode;

        public InMemoryMESSCD (string tcrCode)
        {
            _tcrCode = tcrCode;
        }

        public RegisterCashDepositResponse RegisterCashDeposit(RegisterCashDepositRequest registerCashDepositRequest) => throw new NotImplementedException();
        public RegisterInvoiceResponse RegisterInvoice(RegisterInvoiceRequest registerInvoiceRequest) => throw new NotImplementedException();
        public RegisterTCRResponse RegisterTCR(RegisterTCRRequest registerTCRRequest)
        {
            return new RegisterTCRResponse()
            {
                TCRCode = _tcrCode
            };
        }
    }
}
