using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;

namespace fiskaltrust.Middleware.SCU.ME.InMemory;

#nullable enable
public class InMemorySCU : IMESSCD
{
    private readonly ScuMEConfiguration _configuration;
    private readonly Faker _faker;
    public InMemorySCU(ScuMEConfiguration configuration)
    {
        _configuration = configuration;
        _faker = new Faker();
    }

    public Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => Task.FromResult(new ScuMeEchoResponse { Message = request.Message });

    public Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest) =>
        Task.FromResult(new RegisterCashDepositResponse
        {
            FCDC = _faker.Random.Guid().ToString()
        });
    public Task<RegisterCashWithdrawalResponse> RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest) =>
        Task.FromResult(new RegisterCashWithdrawalResponse());

    public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
    {
        var iicInput = string.Join("|", new List<object>
        {
            _configuration.TIN,
            registerInvoiceRequest.Moment,
            registerInvoiceRequest.InvoiceDetails.YearlyOrdinalNumber,
            registerInvoiceRequest.BusinessUnitCode,
            registerInvoiceRequest.TcrCode,
            registerInvoiceRequest.SoftwareCode,
            registerInvoiceRequest.InvoiceDetails.GrossAmount
        }.Select(o => o.ToString()));

        var iicSignature = _configuration.Certificate.GetRSAPrivateKey().SignData(Encoding.ASCII.GetBytes(iicInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var iic = ((HashAlgorithm) CryptoConfig.CreateFromName("MD5")).ComputeHash(iicSignature);

        return Task.FromResult(new RegisterInvoiceResponse
        {
            FIC = _faker.Random.Guid().ToString(),
            IIC = BitConverter.ToString(iic).Replace("-", string.Empty)
        });
    }

    public Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTCRRequest) =>
        Task.FromResult(new RegisterTcrResponse
        {
            TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
        });

    public Task UnregisterTcrAsync(RegisterTcrRequest registerTCRRequest) => Task.CompletedTask;
}