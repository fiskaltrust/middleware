
using System.Xml.Serialization;
using System.Collections.Generic;
namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    [XmlRoot(ElementName = "displayText")]
    public class DisplayText
    {
        public DisplayText(string data) => Data = data;

        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
        [XmlAttribute(AttributeName = "data")]
        public string Data { get; set; }
    }

    [XmlRoot(ElementName = "printRecMessage")]
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
        public string? Quantity { get; set; }
        [XmlAttribute(AttributeName = "unitPrice")]
        public string? UnitPrice { get; set; }
        [XmlAttribute(AttributeName = "department")]
        public string? Department { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public string? Justification { get; set; }
    }

    [XmlRoot(ElementName = "printRecItemVoid")]
    public class PrintRecItemVoid
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }
        [XmlAttribute(AttributeName = "quantity")]
        public string Quantity { get; set; }
        [XmlAttribute(AttributeName = "unitPrice")]
        public string UnitPrice { get; set; }
        [XmlAttribute(AttributeName = "department")]
        public string Department { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public string Justification { get; set; }
    }

    [XmlRoot(ElementName = "printRecItemAdjustment")]
    public class PrintRecItemAdjustment
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }
        [XmlAttribute(AttributeName = "adjustmentType")]
        public string AdjustmentType { get; set; }
        [XmlAttribute(AttributeName = "amount")]
        public string Amount { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public string Justification { get; set; } = "1";
    }

    [XmlRoot(ElementName = "printRecSubtotalAdjustment")]
    public class PrintRecSubtotalAdjustment
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }
        [XmlAttribute(AttributeName = "adjustmentType")]
        public string AdjustmentType { get; set; }
        [XmlAttribute(AttributeName = "amount")]
        public string Amount { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public string Justification { get; set; }
    }

    [XmlRoot(ElementName = "printRecSubtotal")]
    public class PrintRecSubtotal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }
        [XmlAttribute(AttributeName = "option")]
        public string Option { get; set; }
    }

    [XmlRoot(ElementName = "printBarCode")]
    public class PrintBarCode
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }
        [XmlAttribute(AttributeName = "position")]
        public string Position { get; set; }
        [XmlAttribute(AttributeName = "width")]
        public string Width { get; set; }
        [XmlAttribute(AttributeName = "height")]
        public string Height { get; set; }
        [XmlAttribute(AttributeName = "hRIPosition")]
        public string HRIPosition { get; set; }
        [XmlAttribute(AttributeName = "hRIFont")]
        public string HRIFont { get; set; }
        [XmlAttribute(AttributeName = "codeType")]
        public string CodeType { get; set; }
        [XmlAttribute(AttributeName = "code")]
        public string Code { get; set; }
    }

    [XmlRoot(ElementName = "printRecTotal")]
    public class PrintRecTotal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }
        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }
        [XmlAttribute(AttributeName = "payment")]
        public string Payment { get; set; }
        [XmlAttribute(AttributeName = "paymentType")]
        public string PaymentType { get; set; }
        [XmlAttribute(AttributeName = "index")]
        public string Index { get; set; }
        [XmlAttribute(AttributeName = "justification")]
        public string Justification { get; set; }
    }

    [XmlRoot(ElementName = "endFiscalReceipt")]
    public class EndFiscalReceipt
    {
        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; } 
    }

    [XmlRoot(ElementName = "printerFiscalReceipt")]
    public class FiscalReceipt
    {
        [XmlElement(ElementName = "displayText")]
        public List<DisplayText> DisplayText { get; set; }
        [XmlElement(ElementName = "printRecMessage")]
        public List<PrintRecMessage> PrintRecMessage { get; set; }
        [XmlElement(ElementName = "beginFiscalReceipt")]
        public BeginFiscalReceipt BeginFiscalReceipt { get; set; }
        [XmlElement(ElementName = "printRecItem")]
        public List<PrintRecItem> PrintRecItem { get; set; }
        [XmlElement(ElementName = "printRecItemVoid")]
        public PrintRecItemVoid PrintRecItemVoid { get; set; }
        [XmlElement(ElementName = "printRecItemAdjustment")]
        public PrintRecItemAdjustment PrintRecItemAdjustment { get; set; }
        [XmlElement(ElementName = "printRecSubtotalAdjustment")]
        public PrintRecSubtotalAdjustment PrintRecSubtotalAdjustment { get; set; }
        [XmlElement(ElementName = "printRecSubtotal")]
        public PrintRecSubtotal PrintRecSubtotal { get; set; }
        [XmlElement(ElementName = "printBarCode")]
        public PrintBarCode PrintBarCode { get; set; }
        [XmlElement(ElementName = "printRecTotal")]
        public PrintRecTotal PrintRecTotal { get; set; }
        [XmlElement(ElementName = "endFiscalReceipt")]
        public EndFiscalReceipt EndFiscalReceipt { get; set; }
    }

}
