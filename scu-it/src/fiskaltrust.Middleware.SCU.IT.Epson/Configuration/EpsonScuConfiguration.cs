using System.Reflection.Emit;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Configuration
{
    public class EpsonScuConfiguration
    {
        /// <summary>
        /// position defines the starting position from the left margin (range 0 to 511). Three special values 
        /// can also be used:
        /// o 900 = Left aligned
        /// o 901 = Centred
        ///  o 902 = Right aligned
        /// </summary>
        public int BarCodePosition { get; set; }

        /// <summary>
        ///  Indicates the print dot width of each distinct bar (range 1 to 6). Please note that not all readers are able to read 
        /// barcodes with a 1 dot width
        /// </summary>
        public int BarCodeWidth { get; set; }

        /// <summary>
        ///  Indicates the height of the bar code measured in print dots (range 1 to 255).
        /// </summary>
        public int BarCodeHeight { get; set; }

        /// <summary>
        ///  selects one of three ways to print the alphanumeric representation of the barcode 
        /// or to disable it altogether.
        /// The options are as follows:
        /// o 0 = Disabled
        /// o 1 = Above the barcode
        /// o 2 = Below the barcode
        /// o 3 = Below and above the barcode
        /// </summary>
        public int BarCodeHRIPosition { get; set; }

        /// <summary>
        ///  Indicates the font to be used for the HRI string.The options are as follows:
        ///  o A
        ///  o B
        ///  o C
        /// </summary>
        public char BarCodeHRIFont { get; set; }

        /// <summary>
        ///  Indicates the barcode or QR code standard.Choose from one of the following:
        ///  o UPC-A => 65
        ///  o UPC-E / 66
        ///  o EAN13 / 67
        ///  o EAN8 / 68
        ///  o CODE39 / 69
        ///  o ITF / 70
        ///  o CODABAR / 71
        ///  o CODE93 / 72
        ///  o CODE128 / 73
        ///  o 74 to 78 *
        ///  o QRCODE1 / 91
        ///  o QRCODE2 / 92
        /// </summary>
        public int CodeType { get; set; }

    }
}