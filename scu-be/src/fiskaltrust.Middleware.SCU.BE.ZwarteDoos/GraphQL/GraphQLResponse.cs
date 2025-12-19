using System.Collections.Generic;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLQueryResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<MessageItem>? Errors { get; set; }
}


public class GraphQLResponse<T>
{
    [JsonPropertyName("data")]
    public SignResponse<T>? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<MessageItem>? Errors { get; set; }
}

public class SignResponse<T>
{
    [JsonPropertyName("signResult")]
    public T? SignResult { get; set; }
}