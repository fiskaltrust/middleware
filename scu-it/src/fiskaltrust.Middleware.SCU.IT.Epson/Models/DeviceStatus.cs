using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    /// <summary>
    /// Byte 1: DeviceStatus, from STATUS given by the 5 bytes Alphanumeric
    /// </summary>
    [DataContract]
    public enum Printer
    {
        /// <summary>
        /// OK
        /// </summary>
        [DataMember]
        Ok = 0,
        /// <summary>
        /// Paper roll or SLIP station nearly empty
        /// </summary>
        [DataMember]
        NearlyEmpty = 2,
        /// <summary>
        /// Printer offline (No paper or cover open)
        /// </summary>
        [DataMember]
        PrinterOffline = 3,
    }


    /// <summary>
    /// Byte 2: EJ Electronic Journal, from STATUS given by the 5 bytes Alphanumeric
    /// </summary>
    [DataContract]
    public enum ElectronicJournal
    {
        /// <summary>
        /// Electronic Journal is OK
        /// </summary>
        [DataMember]
        Ok = 0,
        /// <summary>
        /// Electronic Journal is nearly full
        /// </summary>
        [DataMember]
        NearlyFull = 1,
        /// <summary>
        /// Electronic Journal is unformatted
        /// </summary>
        [DataMember]
        Unformatted = 2,
        /// <summary>
        /// Previous Electronic Journal
        /// </summary>
        [DataMember]
        Previous = 3,
        /// <summary>
        /// Electronic Journal is from another fiscal printer
        /// </summary>
        [DataMember]
        FromAnotherPrinter = 4,
        /// <summary>
        /// Electronic Journal is full
        /// </summary>
        [DataMember]
        Full = 5,
    }

    /// <summary>
    /// Byte 3: CashDrawer, from STATUS given by the 5 bytes Alphanumeric
    /// </summary>
    [DataContract]
    public enum CashDrawer
    {
        /// <summary>
        /// Open
        /// </summary>
        [DataMember]
        Open = 0,
        /// <summary>
        /// The "1 – closed" indication appears if no cash drawer is connected or if one is connected that has no 
        /// sensor.
        /// </summary>
        [DataMember]
        Closed = 1,
    }

    /// <summary>
    /// Byte 4: Commercial Document / invoice, from STATUS given by the 5 bytes Alphanumeric
    /// </summary>
    [DataContract]
    public enum Invoice
    {
        /// <summary>
        /// Open
        /// </summary>
        [DataMember]
        Open = 0,
        /// <summary>
        /// No current open document (STATO REGISTRAZIONE) 
        /// </summary>
        [DataMember]
        NoDocument = 1,
        /// <summary>
        /// Management document open
        /// </summary>
        [DataMember]
        ManagementDocument = 2,
        /// <summary>
        /// Payment in progress (Commercial document)
        /// </summary>
        [DataMember]
        Payment = 3,
        /// <summary>
        /// Commercial document – Error whilst transmitting ESC/POS commands (at ending phase) [5]
        /// The ESC/POS error condition is cleared whenever a new commercial or management document is opened,
        /// or the reset command is received. Typical errors are paper out, initial timeout expired, or inter-character timeout expired.
        /// </summary>
        [DataMember]
        ErrorOnTransmit = 4,
        /// <summary>
        /// Negative subtotal
        /// (Commercial document) or the reset command is received. Typical errors are paper out, initial timeout expired, or inter-character timeout expired.
        /// </summary>
        [DataMember]
        NegativeSubtotal = 5,
        /// <summary>
        /// Management document – Error whilst transmitting ESC/POS commands   
        ///  The ESC/POS error condition is cleared whenever a new commercial or management document is opened,
        /// or the reset command is received. Typical errors are paper out, initial timeout expired, or inter-character
        /// timeout expired
        /// </summary>
        [DataMember]
        ErrorOnTransmission = 6,
        /// <summary>
        /// JavaPOS-UPOS mode – Awaiting closure command (Commercial document)
        /// </summary>
        [DataMember]
        AwaitingClosureCommand = 7,
        /// <summary>
        /// Direct invoice open
        /// Byte 4 is always 8 for open direct invoices irrespective of subtotal, payment or JavaPOS conditions.
        /// </summary>
        [DataMember]
        DirectInvoiceOpen = 8,
        /// <summary>
        /// Value A. Box office ticket open
        /// </summary>
        [DataMember]
        BoxOfficeTicketOpen = 10,
        /// <summary>
        /// Value B. Box office ticket open
        /// </summary>
        [DataMember]
        BoxOfficeTicketClosed = 11,
    }

    /// <summary>
    /// Byte 5: Operative
    /// </summary>
    [DataContract]
    public enum Operative
    {
        /// <summary>
        ///  Registration State: Known as STATO GESTRAZIONE in Italian. The condition in which the 
        ///  printer behaves normally and can emit receipts / documents, invoices etc.
        /// </summary>
        [DataMember]
        RegistrationState = 0,
        /// <summary>
        /// X State: The condition whereby it is possible to print out non-fiscal / management daily and 
        /// periodic reports plus view daily totals on the customer display.
        /// </summary>
        [DataMember]
        XState = 1,
        /// <summary>
        ///  Z State: Whilst in this condition, important sequences can be entered to perform the following 
        ///  operations:
        ///  o Fiscal closure printing and transmission
        ///  o Printing followed by counter resetting of daily and periodic financial reports
        ///  o Fiscal memory data report printing
        ///  o Electronic journal (DGFE / MPD) data reprinting
        ///  o SD card formatting (for the Electronic Journal)
        ///  o Retail header lines programming
        /// </summary>
        [DataMember]
        ZState = 2,
        /// <summary>
        ///  S State: The condition whereby printer programming can be performed
        /// </summary>
        [DataMember]
        SState = 3,
        /// <summary>
        ///  BoxOffice
        /// </summary>
        [DataMember]
        BoxOffice = 4,
    }

    /// <summary>
    /// STATUS given by the 5 bytes Alphanumeric
    /// </summary>
    public class DeviceStatus
    {
        public DeviceStatus(int[] code)
        {
            Printer = (Printer) code[0];
            ElectronicJournal = (ElectronicJournal) code[1];
            CashDrawer = (CashDrawer) code[2];
            Invoice = (Invoice) code[3];
            Operative = (Operative) code[4];
        }

        /// <summary>
        /// Byte 1: DeviceStatus
        /// </summary>
        [DataMember]
        public Printer Printer { get; set; }

        /// <summary>
        /// Byte 2: ElectronicJournal
        /// </summary>
        [DataMember]
        public ElectronicJournal ElectronicJournal { get; set; }

        /// <summary>
        /// Byte 3: CashDrawer
        /// </summary>
        [DataMember]
        public CashDrawer CashDrawer { get; set; }

        /// <summary>
        /// Byte 4: Commercial Document / invoice
        /// </summary>
        [DataMember]
        public Invoice Invoice { get; set; }

        /// <summary>
        /// Byte 5: Operative
        /// </summary>
        [DataMember]
        public Operative Operative { get; set; }

    }
}
