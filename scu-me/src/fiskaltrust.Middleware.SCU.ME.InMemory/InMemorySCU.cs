using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using fiskaltrust.ifPOS.v2.me;

namespace fiskaltrust.Middleware.SCU.ME.InMemory;

public class InMemorySCU : IMESSCD
{
    public Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest) => throw new NotImplementedException();

    public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest) => throw new NotImplementedException();

    public Task<RegisterTCRResponse> RegisterTCRAsync(RegisterTCRRequest registerTCRRequest) => throw new NotImplementedException();
}