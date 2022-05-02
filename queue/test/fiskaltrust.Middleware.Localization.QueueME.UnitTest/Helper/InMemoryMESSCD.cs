using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.me;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper
{
    public class InMemoryMESSCD : IMESSCD
    {
        private readonly string _tcrCodeOrFIC;


        public InMemoryMESSCD (string tcrCodeOrFIC)
        {
            _tcrCodeOrFIC = tcrCodeOrFIC;
        }

        public Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => throw new NotImplementedException();
        public Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest) => throw new NotImplementedException();
        public Task<RegisterCashWithdrawalResponse> RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest) => throw new NotImplementedException();

        public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
        {
            var invoiceDetails = registerInvoiceRequest.InvoiceDetails;
            invoiceDetails.GrossAmount.Should().Be(invoiceDetails.NetAmount + invoiceDetails.TotalVatAmount);


            return Task.FromResult(new RegisterInvoiceResponse()
            {
                FIC = _tcrCodeOrFIC
            });
        }
        public Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTcrequest)
        {
            return Task.FromResult(new RegisterTcrResponse()
            {
                TcrCode = _tcrCodeOrFIC
            });
        }

        public Task UnregisterTcrAsync(RegisterTcrRequest registerTCRRequest) => throw new NotImplementedException();
    }
}
