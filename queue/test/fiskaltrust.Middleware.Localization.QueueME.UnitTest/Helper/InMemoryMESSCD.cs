using System;
using System.ServiceModel;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.me;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper
{
    public class InMemoryMESSCD : IMESSCD
    {
        private readonly string _tcrCodeOrFIC;
        private readonly string _iic;
        private readonly string _iicSignature;
        private readonly bool _throwException;

        public InMemoryMESSCD (string tcrCodeOrFIC, string iic, string iicSignature, bool throwException = false)
        {
            _tcrCodeOrFIC = tcrCodeOrFIC;
            _throwException = throwException;
            _iic = iic;
            _iicSignature = iicSignature;
        }

        public Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => throw new NotImplementedException();
        public Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest)
        {
            ThrowExceptinIfWanted();
            return Task.FromResult(new RegisterCashDepositResponse()
            {
                FCDC = "1111"
            });
        }
        public Task RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest)
        {
            ThrowExceptinIfWanted();
            return Task.FromResult(true);
        }

        public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
        {
            ThrowExceptinIfWanted();
            var invoiceDetails = registerInvoiceRequest.InvoiceDetails;
            var rounded = (decimal)Math.Round((double) (invoiceDetails.NetAmount + invoiceDetails.TotalVatAmount));
            invoiceDetails.GrossAmount.Should().Be(rounded);
            return Task.FromResult(new RegisterInvoiceResponse()
            {
                FIC = _tcrCodeOrFIC
            });
        }
        public Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTcrequest)
        {
            ThrowExceptinIfWanted();
            return Task.FromResult(new RegisterTcrResponse()
            {
                TcrCode = _tcrCodeOrFIC
            });
        }

        public Task UnregisterTcrAsync(UnregisterTcrRequest unregisterTCRRequest)
        {
            return Task.CompletedTask;
        }

        public void ThrowExceptinIfWanted()
        {
            if (_throwException)
            {
                throw new EndpointNotFoundException("Tse not reachable!");
            }
        }

        public Task<ComputeIICResponse> ComputeIICAsync(ComputeIICRequest computeIICRequest) => Task.FromResult(new ComputeIICResponse { IIC = _iic, IICSignature = _iicSignature });
    }
}

