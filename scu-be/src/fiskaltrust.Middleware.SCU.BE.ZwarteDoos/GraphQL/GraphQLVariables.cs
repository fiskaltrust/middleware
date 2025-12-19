using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLVariables<T>
{
    [JsonPropertyName("data")]
    public required T Data { get; set; }

    [JsonPropertyName("isTraining")]
    public required bool IsTraining { get; set; }
}