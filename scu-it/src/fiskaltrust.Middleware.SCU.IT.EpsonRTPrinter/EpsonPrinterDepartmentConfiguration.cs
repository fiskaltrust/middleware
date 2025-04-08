using System.Collections.Generic;

public class EpsonPrinterDepartmentConfiguration
{
    public Dictionary<string, long> DepartmentMapping { get; set; } = new Dictionary<string, long>();


    public static EpsonPrinterDepartmentConfiguration Default => new EpsonPrinterDepartmentConfiguration
    {
        DepartmentMapping = new Dictionary<string, long>
        {
            { "0", 8 }, // unknown
            { "1", 2 }, // reduced1 => 10%
            { "2", 3 }, // reduced 2 => 5%
            { "3", 1 }, // basic => 22%
            { "4", -1 }, // superreduced 1
            { "5", -1 }, // superreduced 2
            { "6", -1 }, // parking rate
            { "7", 7 }, // zero rate => 0%
            { "8", 8 }, // not taxable => 0%
        }
    };
}