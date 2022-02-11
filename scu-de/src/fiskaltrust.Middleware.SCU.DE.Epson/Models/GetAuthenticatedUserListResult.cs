using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class OutputAuthenticatedUserListResult
    {
        public List<string> AuthenticatedUserList { get; } = new List<string>();
    }
}
