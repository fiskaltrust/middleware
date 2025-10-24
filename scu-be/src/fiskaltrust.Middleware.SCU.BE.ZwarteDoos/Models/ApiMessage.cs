using System;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class ApiMessage
{
    public string Message { get; set; } = null!;
    public GraphQLError[] Locations { get; set; } = Array.Empty<GraphQLError>();
    public Extensions Extensions { get; set; } = null!;
}