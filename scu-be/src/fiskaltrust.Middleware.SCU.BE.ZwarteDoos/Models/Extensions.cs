using System;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class Extensions
{
    public string Category { get; set; } = null!;
    public string Code { get; set; } = null!;
    public ExtensionData[] Data { get; set; } = Array.Empty<ExtensionData>();
    public bool ShowPos { get; set; }
}