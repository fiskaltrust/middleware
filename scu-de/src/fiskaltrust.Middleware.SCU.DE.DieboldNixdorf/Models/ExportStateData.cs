using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public enum ExportState
    {
        Unkown,
        Running,
        Succeeded,
        Failed
    }

    public class ExportStateData
    {
        public int ReadPointer { get; set; } = -1;
        public Exception Error { get; set; } = null;
        public ExportState State { get; set; }
    }
}