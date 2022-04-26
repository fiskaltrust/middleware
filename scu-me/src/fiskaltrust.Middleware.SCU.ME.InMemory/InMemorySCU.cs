using System;
using System.Threading.Tasks;
using Bogus;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;

namespace fiskaltrust.Middleware.SCU.ME.InMemory;

#nullable enable
public class InMemorySCU : IMESSCD
{
    private readonly Faker _faker;
    public InMemorySCU()
    {
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

    public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest) =>
        Task.FromResult(new RegisterInvoiceResponse
        {
            FIC = _faker.Random.Guid().ToString(),
            IIC = _faker.Random.Hash(32)
        });

    public Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTCRRequest) =>
        Task.FromResult(new RegisterTcrResponse
        {
            TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
        });

    public Task UnregisterTcrAsync(RegisterTcrRequest registerTCRRequest) => Task.CompletedTask;
}