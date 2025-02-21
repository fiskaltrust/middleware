using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// The fiskaltrust.Middleware sends back the processed data to the cash register through the receipt response.
    /// </summary>
    [DataContract]
    public class ReceiptResponse
    {
        /// <summary>
        /// Allocated from request to response.
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = true, IsRequired = true)]
        public string ftCashBoxID { get; set; }

        /// <summary>
        /// QueueId used for processing.
        /// </summary>
        [DataMember(Order = 14, EmitDefaultValue = true, IsRequired = true)]
        public string ftQueueID { get; set; }

        /// <summary>
        /// QueueItemId used for processing.
        /// </summary>
        [DataMember(Order = 15, EmitDefaultValue = true, IsRequired = true)]
        public string ftQueueItemID { get; set; }

        /// <summary>
        /// QueueRow used for processing.
        /// </summary>
        [DataMember(Order = 16, EmitDefaultValue = true, IsRequired = true)]
        public long ftQueueRow { get; set; }

        /// <summary>
        /// Allocated from request to response.
        /// </summary>
        [DataMember(Order = 20, EmitDefaultValue = true, IsRequired = true)]
        public string cbTerminalID { get; set; }

        /// <summary>
        /// Allocated from request to response.
        /// </summary>
        [DataMember(Order = 30, EmitDefaultValue = true, IsRequired = true)]
        public string cbReceiptReference { get; set; }

        /// <summary>
        /// Cash register identification number.
        /// </summary>
        [DataMember(Order = 35, EmitDefaultValue = true, IsRequired = true)]
        public string ftCashBoxIdentification { get; set; }

        /// <summary>
        /// Upcounting receipt number allocated through fiskaltrust.SecurityMechanisms.
        /// </summary>
        [DataMember(Order = 40, EmitDefaultValue = true, IsRequired = true)]
        public string ftReceiptIdentification { get; set; }

        /// <summary>
        /// Time of receipt processing through fiskaltrust.Middleware, provided in UTC.
        /// </summary>
        [DataMember(Order = 50, EmitDefaultValue = true, IsRequired = true)]
        public DateTime ftReceiptMoment { get; set; }

        /// <summary>
        /// Additional header for the receipt. Each row can contain up to 4096 characters. Line breaks should be inserted by the cash register independently
        /// </summary>
        [DataMember(Order = 60, EmitDefaultValue = false, IsRequired = false)]
        public string[] ftReceiptHeader { get; set; }

        /// <summary>
        /// Additional data sets in the charge items block which the cash register has to print onto the receipt. By default no additional data is provided. If additional data is provided, these data sets state an amount of „0“.
        /// </summary>
        [DataMember(Order = 70, EmitDefaultValue = false, IsRequired = false)]
        public ChargeItem[] ftChargeItems { get; set; }

        /// <summary>
        /// Additional text line for the charge items block which the cash register has to print onto the receipt. Each row can contain up to 4096 characters, line breaks should be inserted by the cash register independently.
        /// </summary>
        [DataMember(Order = 80, EmitDefaultValue = false, IsRequired = false)]
        public string[] ftChargeLines { get; set; }

        /// <summary>
        /// Additional data set in the pay items block which the cash register has to print onto the receipt. By default no additional data is provided. If additional data is provided, these data sets state an amount of „0“.
        /// </summary>
        [DataMember(Order = 90, EmitDefaultValue = false, IsRequired = false)]
        public PayItem[] ftPayItems { get; set; }

        /// <summary>
        /// Additional text line for the pay items block which the cash register has to print onto the receipt. Each row can contain up to 4096 characters, line breaks should be inserted by the cash register independently.
        /// </summary>
        [DataMember(Order = 100, EmitDefaultValue = false, IsRequired = false)]
        public string[] ftPayLines { get; set; }

        /// <summary>
        /// Signature block, which the cash register has to print onto the receipt.
        /// </summary>
        [DataMember(Order = 110, EmitDefaultValue = true, IsRequired = true)]
        public SignaturItem[] ftSignatures { get; set; }

        /// <summary>
        /// Additional footer for the receipt. Each row can contain up to 4096 characters, line breaks should be inserted by the cash register independently.
        /// </summary>
        [DataMember(Order = 120, EmitDefaultValue = false, IsRequired = false)]
        public string[] ftReceiptFooter { get; set; }

        /// <summary>
        /// Flag indicating the status of the fiskaltrust.Middleware; set accordingly to the reference table in the appendix.
        /// </summary>
        [DataMember(Order = 130, EmitDefaultValue = true, IsRequired = true)]
        public long ftState { get; set; }

        /// <summary>
        /// Additional information regarding the status of the fiskaltrust.Middleware, currently accepted only in JSON format.
        /// </summary>
        [DataMember(Order = 140, EmitDefaultValue = false, IsRequired = false)]
        public string ftStateData { get; set; }

        public ReceiptResponse()
        {
            ftCashBoxID = string.Empty;
            ftQueueID = string.Empty;
            ftQueueItemID = string.Empty;
            ftQueueRow = 0;
            cbTerminalID = string.Empty;
            cbReceiptReference = string.Empty;
            ftCashBoxIdentification = string.Empty;
            ftReceiptIdentification = string.Empty;
            ftReceiptMoment = DateTime.UtcNow;
            ftReceiptHeader = null;
            ftChargeItems = null;
            ftChargeLines = null;
            ftPayItems = null;
            ftPayLines = null;
            ftSignatures = new SignaturItem[] { };
            ftReceiptFooter = null;
            ftState = 0x0;
        }
    }
}
