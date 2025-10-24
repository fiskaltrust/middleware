namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class VatCalc
{
    public string Label { get; set; } = null!;
    public decimal Rate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool OutOfScope { get; set; }
}