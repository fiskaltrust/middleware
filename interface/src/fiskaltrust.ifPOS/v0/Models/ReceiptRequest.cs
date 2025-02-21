using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v0
{
    /// <summary>
    /// The cash register transfers the data of an entire receipt request to the fiskaltrust.Middleware using the ReceiptRequest data structure.
    /// </summary>
    [DataContract]
    public partial class ReceiptRequest
    {
        /// <summary>
        /// This ID is assigned by the fiskaltrust-user portal and is a part of the authentication of the cash register.
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = true, IsRequired = true)]
        public string ftCashBoxID { get; set; }

        /// <summary>
        /// The QueueID is required only when a load balancer is used. The value of the ftQueueID allows the load balancer to find the correct route to the corresponding Queue.
        /// </summary>
        [DataMember(Order = 15, EmitDefaultValue = false, IsRequired = false)]
        public string ftQueueID { get; set; }

        /// <summary>
        /// This field identifies and documents the type and software version of the PosSystem sending the request. It is used for audits and as a base for commission calculation. The PosSystem itself has to be created in the portal and its ID can be implemented as a constant value by the PosCreator.
        /// </summary>
        [DataMember(Order = 16, EmitDefaultValue = false, IsRequired = false)]
        public string ftPosSystemId { get; set; }

        /// <summary>
        /// The unique identification of the input station/ cash register within a ftCashBoxID
        /// </summary>
        [DataMember(Order = 20, EmitDefaultValue = true, IsRequired = true)]
        public string cbTerminalID { get; set; }

        /// <summary>
        /// Reference number returned by the cash register. Ideally, this value would be a unique receipt number for the cash register, to allow saving of the return value to the cash register data set.
        /// </summary>
        [DataMember(Order = 30, EmitDefaultValue = true, IsRequired = true)]
        public string cbReceiptReference { get; set; }

        /// <summary>
        /// The time of receipt creation. Must be provided in UTC.
        /// </summary>
        [DataMember(Order = 40, EmitDefaultValue = true, IsRequired = true)]
        public DateTime cbReceiptMoment { get; set; }

        /// <summary>
        /// List of services or items sold.
        /// </summary>
        [DataMember(Order = 50, EmitDefaultValue = true, IsRequired = true)]
        public ChargeItem[] cbChargeItems { get; set; }

        /// <summary>
        /// List of payment received.
        /// </summary>
        [DataMember(Order = 60, EmitDefaultValue = true, IsRequired = true)]
        public PayItem[] cbPayItems { get; set; }

        /// <summary>
        /// Type of business transaction according to the reference table in the appendix. It is used to choose the right processing logic.
        /// </summary>
        [DataMember(Order = 70, EmitDefaultValue = true, IsRequired = true)]
        public long ftReceiptCase { get; set; }

        /// <summary>
        /// Additional data for the business transaction, currently accepted only in JSON format. Although all string values are supported, we suggest using data structures serialized into JSON format.
        /// </summary>
        [DataMember(Order = 80, EmitDefaultValue = false, IsRequired = false)]
        public string ftReceiptCaseData { get; set; }


        /// <summary>
        /// Total receipt amount incl. taxes (gross receipt amount). If it is not provided, it can be calculated with the sum of the amounts of the cbChargeItems. It can be useful and important for systems working with net amounts, as it helps to apply different methods of calculation and rounding.
        /// </summary>
        [DataMember(Order = 90, EmitDefaultValue = false, IsRequired = false)]
        public decimal? cbReceiptAmount { get; set; }

        /// <summary>
        /// Identification of the user, who creates the receipt. Although all string values are supported, we suggest using data structures serialized into JSON format.
        /// </summary>
        [DataMember(Order = 100, EmitDefaultValue = false, IsRequired = false)]
        public string cbUser { get; set; }

        /// <summary>
        /// Identification of the section/field, in which the receipt is created. Although all string values are supported, we suggest using data structures serialized into JSON format.
        /// </summary>
        [DataMember(Order = 110, EmitDefaultValue = false, IsRequired = false)]
        public string cbArea { get; set; }

        /// <summary>
        /// Identification of the client, for whom the receipt is created. Although all string values are supported, we suggest using data structures serialized into JSON format.
        /// </summary>
        [DataMember(Order = 120, EmitDefaultValue = false, IsRequired = false)]
        public string cbCustomer { get; set; }

        /// <summary>
        /// Settlement identification where this receipt will be added.
        /// </summary>
        [DataMember(Order = 130, EmitDefaultValue = false, IsRequired = false)]
        public string cbSettlement { get; set; }

        /// <summary>
        /// cbReceiptReference of the previous receipt. Used to connect multiple requests for a single Business Case.
        /// </summary>
        [DataMember(Order = 140, EmitDefaultValue = false, IsRequired = false)]
        public string cbPreviousReceiptReference { get; set; }

        public ReceiptRequest()
        {
            ftCashBoxID = string.Empty;
            cbTerminalID = string.Empty;
            cbReceiptReference = string.Empty;
            cbReceiptMoment = DateTime.UtcNow;
            cbChargeItems = new ChargeItem[] { };
            cbPayItems = new PayItem[] { };
            ftReceiptCase = 0x0;
        }
    }
}
