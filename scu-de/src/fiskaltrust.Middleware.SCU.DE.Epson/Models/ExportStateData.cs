using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Models
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
