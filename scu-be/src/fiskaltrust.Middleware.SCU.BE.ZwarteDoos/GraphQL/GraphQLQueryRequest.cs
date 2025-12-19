using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLQueryRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; set; }
}