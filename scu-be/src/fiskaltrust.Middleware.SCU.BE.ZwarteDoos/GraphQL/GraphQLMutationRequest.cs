using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLMutationRequest<T>
{
    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("operationName")]
    public required string OperationName { get; set; }

    [JsonPropertyName("variables")]
    public required GraphQLVariables<T> Variables { get; set; }
}
