using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.ifPOS.v1.errors
{
    // Mimic the original type structure
    public class SSCDErrorException
    {
        // Fake the backing property
        public SSCDErrorType ErrorType { get; set; }

        // Add other properties if needed
        public string Message { get; set; }
    }

    // Fake the type that EF complained about
    public class SSCDErrorType
    {
        // You can leave it empty or add minimal fields
        public int Code { get; set; }
        public string Description { get; set; }
    }
}
