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
            var converter = _caseConverterFactory.CreateInstance(request.ftJournalType);
            converter.ConvertftJournalTypeToV1(request);
            return request;

        }
    }
}
