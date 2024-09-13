namespace fiskaltrust.Middleware.Localization.QueuePT.Models
{
    public class PTInvoiceElement
    {
        public DateTime InvoiceDate { get; set; } // AAAA-MM-DD
        public DateTime SystemEntryDate { get; set; } // AAAA-MM-DDTHH:MM:SS
        public required string InvoiceNo { get; set; } // Composed by the internal document code followed by a space, followed by an identifier of the series of the document (mandatory), followed by a bar (/) and by a sequential number of the document within the series. [^ ]+ [^/^ ]+/[0-9]+
        public decimal GrossTotal { get; set; } //Numerical field with two decimal points, decimal separator “.” (dot) and without any separator for the thousands.
        public required string Hash { get; set; } // Base 64
    }
}