using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    internal class YearlyClosingReceiptCommand : ClosingReceiptCommand
    {
        public YearlyClosingReceiptCommand(IServiceProvider services) : base(services) { }
   }
}
