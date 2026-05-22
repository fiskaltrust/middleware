using System.Text.Json;
using V1 = fiskaltrust.ifPOS.v1;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Scu;

internal static class V1ScuAdapter
{
    private static readonly JsonSerializerOptions Options = new() { IncludeFields = false };

    public static V1.it.ProcessRequest ToV1ProcessRequest(V2.ReceiptRequest request, V2.ReceiptResponse response)
        => new V1.it.ProcessRequest
        {
            ReceiptRequest = RoundTrip<V2.ReceiptRequest, V1.ReceiptRequest>(request)!,
            ReceiptResponse = RoundTrip<V2.ReceiptResponse, V1.ReceiptResponse>(response)!,
        };

    public static void MergeIntoV2(V2.ReceiptResponse target, V1.ReceiptResponse source)
    {
        target.ftState = (V2.Cases.State) (ulong) source.ftState;
        target.ftStateData = source.ftStateData;
        target.ftReceiptIdentification = source.ftReceiptIdentification;
        target.ftSignatures = source.ftSignatures is { Length: > 0 }
            ? source.ftSignatures.Select(ToV2Signature).ToList()
            : new List<V2.SignatureItem>();
    }

    public static V2.SignatureItem ToV2Signature(V1.SignaturItem v1) => new V2.SignatureItem
    {
        Caption = v1.Caption,
        Data = v1.Data,
        ftSignatureFormat = (V2.Cases.SignatureFormat) (ulong) v1.ftSignatureFormat,
        ftSignatureType = (V2.Cases.SignatureType) (ulong) v1.ftSignatureType,
    };

    private static TTarget? RoundTrip<TSource, TTarget>(TSource source) where TTarget : new()
    {
        if (source is null) return default;
        var json = JsonSerializer.Serialize(source, Options);
        return JsonSerializer.Deserialize<TTarget>(json, Options);
    }
}
