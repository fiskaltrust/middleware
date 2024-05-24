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
            try
            {
                var converter = _caseConverterFactory.CreateInstance(request.ftReceiptCase);
                converter.ConvertftReceiptCaseToV1(request.ftReceiptCase);
                request.cbChargeItems.Select(x => x.ftChargeItemCase = converter.ConvertftChargeItemCaseToV1(x.ftChargeItemCase));
                request.cbPayItems.Select(x => x.ftPayItemCase = converter.ConvertftPayItemCaseToV1(x.ftPayItemCase));
                return request;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public ReceiptResponse ConvertResponseToV2(ReceiptResponse response)
        {
            try
            {
                var converter = _caseConverterFactory.CreateInstance(response.ftState);
                response.ftState = converter.ConvertftStateToV2(response.ftState);
                response.ftSignatures.Select(x => x.ftSignatureFormat = converter.ConvertftSignatureFormatToV2(x.ftSignatureFormat));
                response.ftSignatures.Select(x => x.ftSignatureType = converter.ConvertftSignatureFormatToV2(x.ftSignatureType));
                return response;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


    }
}
