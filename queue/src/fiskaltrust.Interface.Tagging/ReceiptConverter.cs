using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.Interface.Tagging.ErrorHandling;
using fiskaltrust.Interface.Tagging.Models.Extensions;
namespace fiskaltrust.Interface.Tagging
{
    public class ReceiptConverter
    {
        private readonly ICaseConverterFactory _caseConverterFactory;
        public ReceiptConverter()
        {

            _caseConverterFactory = new CaseConverterFactory();

        }
        public ReceiptRequest ConvertRequestToV1(ReceiptRequest request)
        {
            var converter = _caseConverterFactory.CreateInstance(request.ftReceiptCase);
            if (!request.IsVersionV2())
            {
                throw new ReceiptCaseVersionException($"It's NOT a V2 receipt case. Found V{request.GetVersion()}.");
            }
            converter.ConvertftReceiptCaseToV1(request);

            if (request.cbChargeItems != null)
            {
                foreach (var chargeItem in request.cbChargeItems)
                {
                    if (!chargeItem.IsVersionV2())
                    {
                        throw new ChargeItemVersionException($"Using the v2 tagging in ftReceiptCases requires the usage of v2 ChargeItems (mixing versions within requests is not possible). Found v{chargeItem.GetVersion()} instead of v2.");
                    }
                    converter.ConvertftChargeItemCaseToV1(chargeItem);
                }
            }

            if (request.cbPayItems != null)
            {
                foreach (var payItem in request.cbPayItems)
                {
                    if (!payItem.IsVersionV2())
                    {
                        throw new PayItemVersionException($"Using the v2 tagging in ftReceiptCases requires the usage of v2 PayItems (mixing versions within requests is not possible). Found v{payItem.GetVersion()} instead of v2.");
                    }
                    converter.ConvertftPayItemCaseToV1(payItem);
                }
            }
            return request;

        }
        public ReceiptResponse ConvertResponseToV2(ReceiptResponse response)
        {
            var converter = _caseConverterFactory.CreateInstance(response.ftState);
            if (!response.IsVersionV1())
            {
                throw new StateVersionException($"It's NOT a V1 state.Found V{response.GetVersion()}.");
            }
            converter.ConvertftStateToV2(response);
            if (response.ftSignatures != null)
            {
                foreach (var signature in response.ftSignatures)
                {
                    if (!signature.IsTypeVersionV1())
                    {
                        throw new SignatureTypeVersionException($"It's NOT a V1 signature Item.");
                    }
                    
                    converter.ConvertftSignatureTypeToV2(signature);
                    converter.ConvertftSignatureFormatToV2(signature);

                }
            }
            return response;

        }


    }
}
