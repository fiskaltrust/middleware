using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Interfaces;

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
            converter.ConvertftReceiptCaseToV1(request);
            foreach (var chargeItem in request.cbChargeItems)
            {
                converter.ConvertftChargeItemCaseToV1(chargeItem);
            }
            foreach (var payItem in request.cbPayItems)
            {
                converter.ConvertftPayItemCaseToV1(payItem);
            }
            return request;

        }
        public ReceiptResponse ConvertResponseToV2(ReceiptResponse response)
        {

            var converter = _caseConverterFactory.CreateInstance(response.ftState);
            converter.ConvertftStateToV2(response);
            foreach (var signature in response.ftSignatures)
            {
                converter.ConvertftSignatureFormatToV2(signature);
                converter.ConvertftSignatureTypeToV2(signature);
            }
            return response;

        }


    }
}
