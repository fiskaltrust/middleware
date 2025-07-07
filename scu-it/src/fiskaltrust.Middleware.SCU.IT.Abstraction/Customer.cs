namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public class Customer
{
    public string? CustomerName { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerStreet { get; set; }
    public string? CustomerZip { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerCountry { get; set; }
    public string? CustomerVATId { get; set; }
}


public class ReceiptCaseLotteryData
{
    public servizi_lotteriadegliscontrini_gov_it? servizi_lotteriadegliscontrini_gov_it { get; set; }
}

public class servizi_lotteriadegliscontrini_gov_it
{
    public string? codicelotteria { get; set; }
}