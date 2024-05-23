using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Interfaces;

namespace fiskaltrust.Interface.Tagging
{
    public class JournalConverter
    {
        private readonly ICaseConverterFactory _caseConverterFactory;
        public JournalConverter()
        {

            _caseConverterFactory = new CaseConverterFactory();

        }
        public JournalRequest ConvertRequestToV1(JournalRequest request)
        {
            try
            {
                var converter = _caseConverterFactory.CreateInstance(request.ftJournalType);
                request.ftJournalType = converter.ConvertftJournalTypeToV1(request.ftJournalType);
                return request;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        } 
    }
}
