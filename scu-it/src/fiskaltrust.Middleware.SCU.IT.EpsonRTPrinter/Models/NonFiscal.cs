using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{
    [XmlType("beginNonFiscal")]
    public class BeginNonFiscal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlType("endNonFiscal")]
    public class EndNonFiscal
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlInclude(typeof(PrintBarCode))]
    [XmlInclude(typeof(PrintGraphicCoupon))]
    [XmlInclude(typeof(PrintNormal))]
    public class PrintItem
    {
        [XmlAttribute(AttributeName = "operator")]
        public string? Operator { get; set; }
    }

    [XmlType("printNormal")]
    public class PrintNormal : PrintItem 
    {
        [Description("All four fonts are supported (1 to 4).")]
        [XmlAttribute(AttributeName = "font")]
        public int Font { get; set; }

        [XmlAttribute(AttributeName = "data")]
        public string? Data { get; set; }
    }

    public enum PrintBarCodeType
    {
        [XmlEnum(Name = "65")]
        UPCA = 65,
        [XmlEnum(Name = "66")]
        UPCE = 66,
        [XmlEnum(Name = "67")]
        EAN13 = 67,
        [XmlEnum(Name = "68")]
        EAN8 = 68,
        [XmlEnum(Name = "69")]
        CODE39 = 69,
        [XmlEnum(Name = "70")]
        ITF = 70,
        [XmlEnum(Name = "71")]
        CODABAR = 71,
        [XmlEnum(Name = "72")]
        CODE93 = 72,
        [XmlEnum(Name = "73")]
        CODE128 = 73,
        [XmlEnum(Name = "74")]
        CodeType74 = 74,
        [XmlEnum(Name = "75")]
        CodeType75 = 75,
        [XmlEnum(Name = "76")]
        CodeType76 = 76,
        [XmlEnum(Name = "77")]
        CodeType77 = 77,
        [XmlEnum(Name = "78")]
        CodeType78 = 78,
        [XmlEnum(Name = "91")]
        QRCODE1 = 91,
        [XmlEnum(Name = "92")]
        QRCODE2 = 92
    }

    public enum PrintBarCodeWidth
    {
        [XmlEnum(Name = "1")]
        Width1 = 1,
        [XmlEnum(Name = "2")]
        Width2 = 2,
        [XmlEnum(Name = "3")]
        Width3 = 3,
        [XmlEnum(Name = "4")]
        Width4 = 4,
        [XmlEnum(Name = "5")]
        Width5 = 5,
        [XmlEnum(Name = "6")]
        Width6 = 6
    }

    public enum PrintBarCodeQRCodeAlignment
    {
        [XmlEnum(Name = "0")]
        LeftAligned = 0,
        [XmlEnum(Name = "1")]
        Centred = 1,
        [XmlEnum(Name = "2")]
        RightAligned = 2
    }

    public enum PrintBarCodeHRIPosition
    {
        [XmlEnum(Name = "0")]
        Disabled = 0,
        [XmlEnum(Name = "1")]
        Above = 1,
        [XmlEnum(Name = "2")]
        Below = 2,
        [XmlEnum(Name = "3")]
        AboveAndBelow = 3
    }

    public enum PrintBarCodeQRCodeDataType
    {
        [XmlEnum(Name = "0")]
        AlphaNumeric = 0,
        [XmlEnum(Name = "9")]
        Binary = 9
    }

    public enum PrintBarCodeHRIFont
    {
        [XmlEnum(Name = "A")]
        A,
        [XmlEnum(Name = "B")]
        B,
        [XmlEnum(Name = "C")]
        C
    }

    public enum PrintBarCodeQRCodeErrorCorrection
    {
        [XmlEnum(Name = "0")]
        Low = 0,
        [XmlEnum(Name = "1")]
        MediumLow = 1,
        [XmlEnum(Name = "2")]
        MediumHigh = 2,
        [XmlEnum(Name = "3")]
        High = 3
    }

    [XmlType("printBarCode")]
    public class PrintBarCode : PrintItem
    {
        [Description("Barcode or QR code standard.")]
        [XmlAttribute(AttributeName = "codeType")]
        public PrintBarCodeType CodeType { get; set; }

        [Description("Defines the starting position from the left margin (range 0 to 511). Three special values can also be used: 900 = Left aligned; 901 = Centred; 902 = Right aligned. This attribute is not used with QR codes and can therefore be omitted.")]
        [XmlAttribute(AttributeName = "position")]
        public string? Position { get; set; }

        [Description("Print dot width of each distinct bar (range 1 to 6). This attribute is not used with QR codes and can therefore be omitted. Please note that not all readers are able to read barcodes with a 1 dot width.")]
        [XmlIgnore]
        public PrintBarCodeWidth? Width { get; set; }

        [XmlAttribute(AttributeName = "width")]
        public string? WidthSerialized
        {
            get => Width.HasValue ? ((int) Width.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    Width = (PrintBarCodeWidth) intValue;
                }
                else
                {
                    Width = null;
                }
            }
        }

        [Description("Height of the bar code measured in print dots (range 1 to 255). This attribute is not used with QR codes and can therefore be omitted.")]
        [XmlIgnore]
        public int? Height { get; set; }

        [XmlAttribute(AttributeName = "height")]
        public string? HeightSerialized
        {
            get => Height.HasValue ? ((int) Height.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    Height = intValue;
                }
                else
                {
                    Height = null;
                }
            }
        }

        [Description("QR code position.")]
        [XmlIgnore]
        public PrintBarCodeQRCodeAlignment? QRCodeAlignment { get; set; }

        [XmlAttribute(AttributeName = "qRCodeAlignment")]
        public string? QRCodeAlignmentSerialized
        {
            get => QRCodeAlignment.HasValue ? ((int) QRCodeAlignment.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    QRCodeAlignment = (PrintBarCodeQRCodeAlignment) intValue;
                }
                else
                {
                    QRCodeAlignment = null;
                }
            }
        }

        [Description("QR code dimension (range 1 to 16).")]
        [XmlIgnore]
        public int? QRCodeSize { get; set; }

        [XmlAttribute(AttributeName = "qRCodeSize")]
        public string? QRCodeSizeSerialized
        {
            get => QRCodeSize.HasValue ? ((int) QRCodeSize.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    QRCodeSize = intValue;
                }
                else
                {
                    QRCodeSize = null;
                }
            }
        }

        [Description("Selects one of three ways to print the alphanumeric representation of the barcode or to disable it altogether. This attribute is not used with QR codes and can therefore be omitted.")]
        [XmlIgnore]
        public PrintBarCodeHRIPosition? HRIPosition { get; set; }

        [XmlAttribute(AttributeName = "hRIPosition")]
        public string? HRIPositionSerialized
        {
            get => HRIPosition.HasValue ? ((int) HRIPosition.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    HRIPosition = (PrintBarCodeHRIPosition) intValue;
                }
                else
                {
                    HRIPosition = null;
                }
            }
        }

        [Description("Indicates whether the QR code data is alphanumeric or binary. When binary is chosen, the code attribute value is represented by pairs of hexadecimal digits. For example, HELLO = 48454C4C4F.")]
        [XmlIgnore]
        public PrintBarCodeQRCodeDataType? QRCodeDataType { get; set; }

        [XmlAttribute(AttributeName = "qRCodeDataType")]
        public string? QRCodeDataTypeSerialized
        {
            get => QRCodeDataType.HasValue ? ((int) QRCodeDataType.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    QRCodeDataType = (PrintBarCodeQRCodeDataType) intValue;
                }
                else
                {
                    QRCodeDataType = null;
                }
            }
        }

        [Description("The font to be used for the HRI string. This attribute is not used with QR codes and can therefore be omitted.")]
        [XmlIgnore]
        public PrintBarCodeHRIFont? HRIFont { get; set; }

        [XmlAttribute(AttributeName = "hRIFont")]
        public string? HRIFontSerialized
        {
            get => HRIFont.HasValue ? HRIFont.Value.ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    HRIFont = (PrintBarCodeHRIFont) intValue;
                }
                else
                {
                    HRIFont = null;
                }
            }
        }

        [Description("The level of error correction to employ (range 0 to 3).")]
        [XmlIgnore]
        public int? QRCodeErrorCorrection { get; set; }

        [XmlAttribute(AttributeName = "qRCodeErrorCorrection")]
        public string? QRCodeErrorCorrectionSerialized
        {
            get => QRCodeErrorCorrection.HasValue ? ((int) QRCodeErrorCorrection.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    QRCodeErrorCorrection = intValue;
                }
                else
                {
                    QRCodeErrorCorrection = null;
                }
            }
        }

        [Description("Represents the barcode or QR code itself. QR codes up to 256 characters can be printed. If the qRCodeDataType attribute indicates a binary representation, this attribute value is represented by pairs of hexadecimal digits. For example, HELLO = 48454C4C4F.")]
        [XmlAttribute(AttributeName = "code")]
        public string Code { get; set; }
    }

    public enum PrintGraphicCouponGraphicFormat
    {
        [XmlEnum(Name = "B")]
        BMP,
        [XmlEnum(Name = "R")]
        Raster
    }

    [XmlType("printGraphicCoupon")]
    public class PrintGraphicCoupon : PrintItem
    {
        [Description("The original image format.")]
        [XmlIgnore]
        public PrintGraphicCouponGraphicFormat? GraphicFormat { get; set; }

        [XmlAttribute(AttributeName = "graphicFormat")]
        public string? GraphicFormatSerialized
        {
            get => GraphicFormat.HasValue ? ((int) GraphicFormat.Value).ToString() : null;
            set
            {
                if (int.TryParse(value, out var intValue))
                {
                    GraphicFormat = (PrintGraphicCouponGraphicFormat) intValue;
                }
                else
                {
                    GraphicFormat = null;
                }
            }
        }

        [Description("The binary data must be supplied in the base64 format and is passed as element data after the attributes rather than being a specific attribute value. The sub-element cannot be auto closed with /> and must therefore contain the closing sub-element name prepended with /. Do not use this command to print coupons without base64 data – It is not meant to be used with files uploaded via the upload.cgi service. The maximum Base 64 data size is 26668 bytes which corresponds with an unencoded limit of 20000 bytes.")]
        [XmlText]
        public string Base64Data { get; set; }
    }

    [XmlType("printerNonFiscal")]
    public class PrinterNonFiscal
    {
        [XmlElement(ElementName = "beginNonFiscal")]
        public BeginNonFiscal BeginNonFiscal { get; set; } = new BeginNonFiscal();

        [XmlElement(ElementName = "printNormal", Type = typeof(PrintNormal))]
        [XmlElement(ElementName = "printBarCode", Type = typeof(PrintBarCode))]
        [XmlElement(ElementName = "printGraphicCoupon", Type = typeof(PrintGraphicCoupon))]
        public List<PrintItem> PrintItems { get; set; } = new List<PrintItem>();

        [XmlElement(ElementName = "endNonFiscal")]
        public EndNonFiscal EndNonFiscal { get; set; } = new EndNonFiscal();
    }
}
