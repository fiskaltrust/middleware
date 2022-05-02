using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using fiskaltrust.Middleware.SCU.ME.FiscalizationService.Helpers;
using Microsoft.Extensions.Logging;

using SoapFiscalizationService = FiscalizationService;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService;

#nullable enable
public class FiscalizationServiceSCU : IMESSCD
{
    private readonly SoapFiscalizationService.FiscalizationServicePortTypeClient _fiscalizationServiceClient;
    private readonly ScuMEConfiguration _configuration;
    private readonly ILogger<FiscalizationServiceSCU> _logger;

    public FiscalizationServiceSCU(ILogger<FiscalizationServiceSCU> logger, ScuMEConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _fiscalizationServiceClient = new SoapFiscalizationService.FiscalizationServicePortTypeClient();
        
        _fiscalizationServiceClient.Endpoint.EndpointBehaviors.Add(new DateTimeBehaviour());
        _fiscalizationServiceClient.Endpoint.EndpointBehaviors.Add(new SigningBehaviour(_configuration.Certificate));
    }

    public Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => Task.FromResult(new ScuMeEchoResponse { Message = request.Message });

    public async Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterCashDepositRequest
        {
            Header = new SoapFiscalizationService.RegisterCashDepositRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerCashDepositRequest.RequestId.ToString(),
            },
            CashDeposit = new SoapFiscalizationService.CashDepositType
            {
                CashAmt = registerCashDepositRequest.Amount,
                ChangeDateTime = registerCashDepositRequest.Moment,
                IssuerTIN = _configuration.TIN,
                Operation = SoapFiscalizationService.CashDepositOperationSType.INITIAL,
                TCRCode = registerCashDepositRequest.TcrCode
            }
        };

        var response = await _fiscalizationServiceClient.registerCashDepositAsync(request);

        return new RegisterCashDepositResponse
        {
            FCDC = response.RegisterCashDepositResponse.FCDC
        };
    }

    public async Task<RegisterCashWithdrawalResponse> RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashWithdrawalRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterCashDepositRequest
        {
            Header = new SoapFiscalizationService.RegisterCashDepositRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerCashWithdrawalRequest.RequestId.ToString()
            },
            CashDeposit = new SoapFiscalizationService.CashDepositType
            {
                CashAmt = registerCashWithdrawalRequest.Amount,
                ChangeDateTime = registerCashWithdrawalRequest.Moment,
                IssuerTIN = _configuration.TIN,
                Operation = SoapFiscalizationService.CashDepositOperationSType.WITHDRAW,
                TCRCode = registerCashWithdrawalRequest.TcrCode
            }
        };

        _ = await _fiscalizationServiceClient.registerCashDepositAsync(request);

        return new RegisterCashWithdrawalResponse { };
    }

    public Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest) => throw new NotImplementedException();

    public async Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTCRRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterTCRRequest
        {
            Header = new SoapFiscalizationService.RegisterTCRRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerTCRRequest.RequestId.ToString()
            },
            TCR = new SoapFiscalizationService.TCRType
            {
                BusinUnitCode = registerTCRRequest.BusinessUnitCode,
                IssuerTIN = _configuration.TIN,
                MaintainerCode = registerTCRRequest.TcrSoftwareMaintainerCode,
                SoftCode = registerTCRRequest.TcrSoftwareCode,
                TCRIntID = registerTCRRequest.InternalTcrIdentifier,
                TypeSpecified = registerTCRRequest.TcrType is not null,
                ValidFrom = sendDateTime,
                ValidFromSpecified = true,
                ValidToSpecified = false
            },
        };

        if(registerTCRRequest.TcrType is not null)
        {
            request.TCR.Type =(SoapFiscalizationService.TCRSType)registerTCRRequest.TcrType;
        }


        var response = await _fiscalizationServiceClient.registerTCRAsync(request);

        return new RegisterTcrResponse
        {
            TcrCode = response.RegisterTCRResponse.TCRCode,
        };
    }

    public async Task UnregisterTcrAsync(RegisterTcrRequest registerTCRRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterTCRRequest
        {
            Header = new SoapFiscalizationService.RegisterTCRRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerTCRRequest.RequestId.ToString()
            },
            TCR = new SoapFiscalizationService.TCRType
            {
                BusinUnitCode = registerTCRRequest.BusinessUnitCode,
                IssuerTIN = _configuration.TIN,
                MaintainerCode = registerTCRRequest.TcrSoftwareMaintainerCode,
                SoftCode = registerTCRRequest.TcrSoftwareCode,
                TCRIntID = registerTCRRequest.InternalTcrIdentifier,
                TypeSpecified = registerTCRRequest.TcrType is not null,
                ValidFromSpecified = false,
                ValidTo = sendDateTime,
                ValidToSpecified = true
            },
        };

        if(registerTCRRequest.TcrType is not null)
        {
            request.TCR.Type =(SoapFiscalizationService.TCRSType)registerTCRRequest.TcrType;
        }

        _ = await _fiscalizationServiceClient.registerTCRAsync(request);

        return;
    }
}