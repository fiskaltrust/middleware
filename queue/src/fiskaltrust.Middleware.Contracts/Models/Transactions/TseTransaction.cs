using System;

namespace fiskaltrust.Middleware.Contracts.Models.Transactions
{
    public abstract class TseTransaction
    {
        public string cbReceiptReference { get; set; }
    }
}
