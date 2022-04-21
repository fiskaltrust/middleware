using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Invoice
    {
        [DataMember]
        public string OperatorCode { get; set; }
        [DataMember]
        public bool IsIssuerInVAT { get; set; } = true;
        [DataMember]
        public bool IsSimplifiedInv { get; set; } = true;
        [DataMember]
        public string TypeOfSelfiss { get; set; }
        [DataMember]
        public decimal? MarkUpAmt { get; set; }
        [DataMember]
        public decimal? GoodsExAmt { get; set; }
        [DataMember]
        public DateTime? PayDeadline { get; set; }
        [DataMember]
        public string ParagonBlockNum { get; set; }
        [DataMember]
        public string TaxPeriod { get; set; }
        [DataMember]
        public CorrectiveInv CorrectiveInv { get; set; }
        [DataMember]
        public Fee[] Fees { get; set; }
        [DataMember]
        public BadDebt BadDebt { get; set; }
    }
}
