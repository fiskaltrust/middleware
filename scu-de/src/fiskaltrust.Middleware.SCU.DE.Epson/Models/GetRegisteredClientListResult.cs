using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class OutputGetRegisteredClientListResult
    {
        public List<string> RegisteredClientIdList { get; } = new List<string>();
    }
}
