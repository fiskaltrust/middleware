using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Interfaces;
using fiskaltrust.Interface.Tagging.ErrorHandling;
using fiskaltrust.Interface.Tagging.Models.Extensions;

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
            if (!request.IsVersionV2())
            {
                throw new JournalTypeVersionException($"It's NOT a V2 journal type. Found V{request.GetVersion()}.");
            }
            converter.ConvertftJournalTypeToV1(request);
            return request;

        }
    }
}
