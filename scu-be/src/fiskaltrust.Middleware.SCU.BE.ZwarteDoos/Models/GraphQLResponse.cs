namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

internal class GraphQLResponse<T>
{
    public T? Data { get; set; }
    public GraphQLError[]? Errors { get; set; }
}
