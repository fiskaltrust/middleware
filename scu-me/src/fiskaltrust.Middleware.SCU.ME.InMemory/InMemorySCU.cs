using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using fiskaltrust.Middleware.SCU.ME.Common.Helpers;

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
            FCDC = Guid.NewGuid().ToString()
        });
    public Task RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest) =>
        Task.FromResult(0);

    public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
    {
        return Task.FromResult(new RegisterInvoiceResponse
        {
            FIC = Guid.NewGuid().ToString(),
            IIC = SigningHelper.CreateIIC(_configuration, registerInvoiceRequest)
        });
    }

    public Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTCRRequest) =>
        Task.FromResult(new RegisterTcrResponse
        {
            TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
        });

    public Task UnregisterTcrAsync(UnregisterTcrRequest registerTCRRequest) => Task.CompletedTask;
}