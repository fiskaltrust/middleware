using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

public class GraphQLError
{
    public string Message { get; set; } = null!;
    public List<GraphQLErrorLocation>? Locations { get; set; }
    public List<string>? Path { get; set; }
}

public class GraphQLErrorLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
}