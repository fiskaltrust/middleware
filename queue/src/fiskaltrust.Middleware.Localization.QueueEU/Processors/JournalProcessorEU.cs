using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using JournalRequest = fiskaltrust.ifPOS.v2.JournalRequest;

namespace fiskaltrust.Middleware.Localization.QueueEU.Processors;

public class JournalProcessorEU : IJournalProcessor
{

    public JournalProcessorEU()
    {
    }

    public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        throw new NotImplementedException();
    }
}
