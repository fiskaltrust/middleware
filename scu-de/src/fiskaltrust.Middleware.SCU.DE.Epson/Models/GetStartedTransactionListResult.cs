using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class GetStartedTransactionListResult
    {
        public List<ulong> StartedTransactionNumberList { get; set; }
    }
}