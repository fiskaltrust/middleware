using System;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class FdmRef
{
    public string FdmId { get; set; } = null!;
    public string FdmDateTime { get; set; } = null!;
    public string EventLabel { get; set; } = null!;
    public int EventCounter { get; set; }
    public int TotalCounter { get; set; }
}