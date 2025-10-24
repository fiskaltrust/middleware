namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

internal class GraphQLError
{
    public string Message { get; set; } = null!;
    public Location[]? Locations { get; set; }
    public string[]? Path { get; set; }
}