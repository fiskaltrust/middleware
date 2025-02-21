using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// The signature entry is only used for the receipt response.
    /// </summary>
    [DataContract]
    public class SignaturItem
    {
        public enum Types : long
        {
            /// <summary>
            /// unknown
            /// </summary>
            Unknown = 0x0000,
            /// <summary>
            /// information notification
            /// </summary>
            Information = 0x1000,
            /// <summary>
            /// alert notification
            /// </summary>
            Warning = 0x2000,
            /// <summary>
            /// failure notification
            /// </summary>
            Error = 0x3000,
            /// <summary>
            /// unknown
            /// </summary>
            AT_Unknown = 0x4154000000000000,
            /// <summary>
            /// signature according to RKSV
            /// </summary>
            AT_RKSV = 0x4154000000000001,
            /// <summary>
            /// Archiving required according to RKSV or BAO §132.E.g. notification of collective receipt after failure, ini-tial receipt, monthly receipt, etc..
            /// </summary>
            AT_StorageObligation = 0x4154000000000002,
            /// <summary>
            /// FinanzOnline notification
            /// </summary>
            AT_FinanzOnline = 0x4154000000000003,
            /// <summary>
            /// unknown
            /// </summary>
            DE_Unknown = 0x4445000000000000,
            /// <summary>
            /// hash
            /// </summary>
            DE_Hash = 0x4445000000000001,
            /// <summary>
            /// archiving required
            /// </summary>
            DE_StorageObligation = 0x4445000000000002
        }

        public enum Formats : long
        {
            /// <summary>
            /// no format defined
            /// </summary>
            Unknown = 0x00,
            /// <summary>
            /// Text
            /// </summary>
            Text = 0x01,
            /// <summary>
            /// Link
            /// </summary>
            Link = 0x02,
            /// <summary>
            /// 2D Code
            /// </summary>
            QR_Code = 0x03,
            /// <summary>
            /// Barcode
            /// </summary>
            Code128 = 0x04,
            /// <summary>
            /// optical character recognition, possible for Base32 data
            /// </summary>
            OCR_A = 0x05,
            /// <summary>
            /// 2D Code
            /// </summary>
            PDF417 = 0x06,
            /// <summary>
            /// 2D Code
            /// </summary>
            DATAMATRIX = 0x07,
            /// <summary>
            /// 2D Code
            /// </summary>
            AZTEC = 0x08,
            /// <summary>
            /// Barcode
            /// </summary>
            EAN_8 = 0x09,
            /// <summary>
            /// Barcode
            /// </summary>
            EAN_13 = 0x0A,
            /// <summary>
            /// Barcode
            /// </summary>
            UPC_A = 0x0B,
            /// <summary>
            /// Barcode, possible for Base32 data
            /// </summary>
            Code39 = 0x0C,
            /// <summary>
            /// Base64 data
            /// </summary>
            Base64 = 0x0D
        }

        /// <summary>
        /// Format for displaying signature data according to the reference table in the appendix.
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = true, IsRequired = true)]
        public long ftSignatureFormat { get; set; }

        /// <summary>
        /// Type of signature according to the reference table in the appendix, e.g.: signature indicating a failure notification
        /// </summary>
        [DataMember(Order = 20, EmitDefaultValue = true, IsRequired = true)]
        public long ftSignatureType { get; set; }

        /// <summary>
        /// Heading, which has to be displayed as text above the signature data.
        /// </summary>
        [DataMember(Order = 30, EmitDefaultValue = false, IsRequired = false)]
        public string Caption { get; set; }

        /// <summary>
        /// Signature content which has to be displayed in the specified format.
        /// </summary>
        [DataMember(Order = 40, EmitDefaultValue = true, IsRequired = true)]
        public string Data { get; set; }

        public SignaturItem()
        {
            ftSignatureFormat = 0x0;
            ftSignatureType = 0x0;
            Caption = string.Empty;
            Data = string.Empty;
        }
    }
}
