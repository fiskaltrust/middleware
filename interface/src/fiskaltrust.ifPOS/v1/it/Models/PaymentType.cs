using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{

    /// <summary>
    /// Sale items on a commercial sale document.
    /// </summary>
    [DataContract]
    public enum PaymentType
    {
        /// <summary>
        ///Bar
        /// </summary>
        [EnumMember]
        Cash = 0,
        /// <summary>
        ///Cheque
        /// </summary>
        [EnumMember]
        Cheque = 1,
        /// <summary>
        ///Credit or credit card. Credit now interpreted as mixed not paid.
        /// </summary>
        [EnumMember] 
        CreditCard = 2,
        /// <summary>
        ///Ticket
        /// </summary>
        [EnumMember] 
        Ticket = 3,
        /// <summary>
        ///In the case of multiple tickets, index is used to indicate the quantity
        /// </summary>
        [EnumMember] 
        MultipleTickets = 4,
        /// <summary>
        ///With respect to Not paid, index specifies the sub-type. Definded in NotPaidIndex.
        /// </summary>
        [EnumMember] 
        NotPaid = 5,
        /// <summary>
        ///With respect to Payment discount, index specifies the sub-type. Definded in PaymentDiscountIndex
        /// </summary>
        [EnumMember] 
        PaymentDiscount = 6,
        /// <summary>
        ///Multi use voucher
        /// </summary>
        [EnumMember]
        Voucher = 7
    }
}
