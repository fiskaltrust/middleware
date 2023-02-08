using System.Xml.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    [XmlType("displayText")]
    public class DisplayText
    {
        public DisplayText(string data) => Data = data;

        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "data")]
        public string Data { get; set; }
    }

    [XmlType("printRecMessage")]
    public class PrintRecMessage
    {
        public PrintRecMessage(string message) => Message = message;

        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "messageType")]
        public string? MessageType { get; set; }
        [XmlAttribute(AttributeName = "index")]
        public string? Index { get; set; }
        [XmlAttribute(AttributeName = "font")]
        public string? Font { get; set; }
        [XmlAttribute(AttributeName = "message")]
        public string Message { get; set; }
        [XmlAttribute(AttributeName = "comment")]
        public string? Comment { get; set; }
    }

    [XmlRoot(ElementName = "beginFiscalReceipt")]
    public class BeginFiscalReceipt
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlRoot(ElementName = "printRecItem")]
    public class PrintRecItem
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string? Description { get; set; }
        [XmlAttribute(AttributeName = "quantity")]
        public decimal Quantity { get; set; }
        [XmlAttribute(AttributeName = "unitPrice")]
        public decimal UnitPrice { get; set; }
        [XmlAttribute(AttributeName = "department")]
        public int Department { get; set; } = 1;
        [XmlAttribute(AttributeName = "justification")]
        public int Justification { get; set; } = 1; 
    }

    [XmlRoot(ElementName = "printRecItemVoid")]
    public class PrintRecItemVoid
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string? Description { get; set; }
        [XmlAttribute(AttributeName = "quantity")]
        public int Quantity { get; set; }
        [XmlAttribute(AttributeName = "unitPrice")]
        public decimal UnitPrice { get; set; }
        [XmlAttribute(AttributeName = "department")]
        public string? Department { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public string? Justification { get; set; }
    }

    [XmlRoot(ElementName = "printRecSubtotalAdjustment")]
    public class PrintRecSubtotalAdjustment
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string? Description { get; set; }

        /// <summary>  
        /// +value is surcharge -value is discount 
        ///determines discount/surcharge operation to perform:
        ///1 = Discount on subtotal with subtotal printed out
        ///2 = Discount on subtotal without subtotal printed out
        /// 6 = Surcharge on subtotal with subtotal printed out
        /// 7 = Surcharge on subtotal without subtotal printed out
        /// </summary>
        [XmlAttribute(AttributeName = "adjustmentType")]
        public int AdjustmentType { get; set; }
        [XmlAttribute(AttributeName = "amount")]
        public decimal Amount { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public int Justification { get; set; }
    }

    [XmlRoot(ElementName = "printRecSubtotal")]
    public class PrintRecSubtotal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "option")]
        public int Option { get; set; } = 0;
    }

    [XmlRoot(ElementName = "printBarCode")]
    public class PrintBarCode
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "position")]
        public int Position { get; set; }
        [XmlAttribute(AttributeName = "width")]
        public int Width { get; set; }
        [XmlAttribute(AttributeName = "height")]
        public int Height { get; set; }
        [XmlAttribute(AttributeName = "hRIPosition")]
        public int HRIPosition { get; set; }
        [XmlAttribute(AttributeName = "hRIFont")]
        public char HRIFont { get; set; }
        [XmlAttribute(AttributeName = "codeType")]
        public string? CodeType { get; set; }
        [XmlAttribute(AttributeName = "code")]
        public string? Code { get; set; }
    }

    [XmlRoot(ElementName = "printRecTotal")]
    public class PrintRecTotal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string? Description { get; set; }
        [XmlAttribute(AttributeName = "payment")]
        public decimal Payment { get; set; }
        [XmlAttribute(AttributeName = "paymentType")]
        public int PaymentType { get; set; }
        [XmlAttribute(AttributeName = "index")]
        public int Index { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public int Justification { get; set; } = 1;
    }

    [XmlRoot(ElementName = "endFiscalReceipt")]
    public class EndFiscalReceipt
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; } 
    }

    [XmlRoot(ElementName = "printerFiscalReceipt")]
    public class FiscalReceipt
    {
        [XmlElement(ElementName = "displayText")]
        public List<DisplayText>? DisplayText { get; set; }
        [XmlElement(ElementName = "printRecMessage")]
        public List<PrintRecMessage>? PrintRecMessage { get; set; }
        [XmlElement(ElementName = "beginFiscalReceipt")]
        public BeginFiscalReceipt BeginFiscalReceipt { get; set; } = new BeginFiscalReceipt();
        [XmlElement(ElementName = "printRecItem")]
        public List<PrintRecItem>? PrintRecItem { get; set; }
        [XmlElement(ElementName = "printRecItemVoid")]
        public PrintRecItemVoid? PrintRecItemVoid { get; set; }
        [XmlElement(ElementName = "printRecSubtotalAdjustment")]
        public PrintRecSubtotalAdjustment? PrintRecSubtotalAdjustment { get; set; }
        [XmlElement(ElementName = "printRecSubtotal")]
        public PrintRecSubtotal? PrintRecSubtotal { get; set; }
        [XmlElement(ElementName = "printBarCode")]
        public PrintBarCode? PrintBarCode { get; set; }
        [XmlElement(ElementName = "printRecTotal")]
        public List<PrintRecTotal>? PrintRecTotal { get; set; }
        [XmlElement(ElementName = "endFiscalReceipt")]
        public EndFiscalReceipt EndFiscalReceipt { get; set; }= new EndFiscalReceipt();
    }

}
