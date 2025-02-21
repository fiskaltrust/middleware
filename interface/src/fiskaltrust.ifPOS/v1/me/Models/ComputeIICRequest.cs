using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable enable
namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class ComputeIICRequest
    {
        /// <summary>
        /// Unique code of the cash register, assigned by the central invoice register (CIS) service when registering a cash register.
        /// </summary>
        [DataMember(Order = 10)]
        public string TcrCode { get; set; }

        /// <summary>
        /// The moment in which the invoice is created and issued.
        /// </summary>
        [DataMember(Order = 20)]
        public DateTime Moment { get; set; }

        /// <summary>
        /// Code of the business unit in which the invoice is issued.
        /// </summary>
        /// <remarks>
        /// Must have the following format: [a-z]{2}[0-9]{3}[a-Z]{2}[0-9]{3} (e.g. ab123ab123)
        /// </remarks>
        [DataMember(Order = 30)]
        public string BusinessUnitCode { get; set; }

        /// <summary>
        /// Code of the software used for issuing the invoice.
        /// </summary>
        /// <remarks>
        /// Must have the following format: [a-z]{2}[0-9]{3}[a-Z]{2}[0-9]{3} (e.g. ab123ab123)
        /// </remarks>
        [DataMember(Order = 40)]
        public string SoftwareCode { get; set; }

        /// <summary>
        /// The ordinal number of the ionvoice. Each new invoice gets a new upcounting number, starting from zero on the start of each new year.
        /// </summary>
        [DataMember(Order = 50)]
        public ulong YearlyOrdinalNumber { get; set; }
        
        /// <summary>
        /// Total price of all items including taxes and discounts.
        /// </summary>
        [DataMember(Order = 60)]
        public decimal GrossAmount { get; set; }
    }
}
