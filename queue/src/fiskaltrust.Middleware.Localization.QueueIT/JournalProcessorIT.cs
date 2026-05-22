using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT;

public class JournalProcessorIT : IJournalProcessor
{
    public (ContentType contentType, IAsyncEnumerable<byte[]> result) ProcessAsync(JournalRequest request)
        => (new ContentType(MediaTypeNames.Text.Plain), EmptyAsync());

    private static async IAsyncEnumerable<byte[]> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }
}
