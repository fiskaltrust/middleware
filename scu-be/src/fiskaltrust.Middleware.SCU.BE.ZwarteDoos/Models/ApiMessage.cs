using System;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class ApiMessage
{
    public string Message { get; set; } = null!;
    public Location[] Locations { get; set; } = Array.Empty<Location>();
    public Extensions Extensions { get; set; } = null!;
}