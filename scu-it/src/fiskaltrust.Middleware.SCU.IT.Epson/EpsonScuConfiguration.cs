namespace fiskaltrust.Middleware.SCU.IT.Epson
{
    public class EpsonScuConfiguration
    {
        /// <summary>
        /// The URL or IP address of the RT Printer or Server, e.g. http://192.168.0.100
        /// </summary>
        public string? DeviceUrl { get; set; }

        /// <summary>
        /// The HTTP client timeout used when communicating with the RT Printer or Server
        /// </summary>
        public int ClientTimeoutMs { get; set; } = 15000;

        /// <summary>
        /// The server/printer timeout for executing commands
        /// </summary>
        public int ServerTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// The timeout used for locking the SCU during requests. Should be greater than ClientTimeoutMs.
        /// </summary>
        public int LockTimeoutMs { get; set; } = 16000;

        /// <summary>
        /// This value defines the starting position from the left margin (range 0 to 511). 
        /// </summary>
        /// <remarks>
        /// Three special values can also be used:
        /// - 900 = Left aligned
        /// - 901 = Centred
        /// - 902 = Right aligned
        /// </remarks>
        public int BarCodePosition { get; set; } = 10;

        /// <summary>
        /// Indicates the print dot width of each distinct bar (range 1 to 6). Please note that not all readers are able to read barcodes with a 1 dot width
        /// </summary>
        public int BarCodeWidth { get; set; } = 2;

        /// <summary>
        /// Indicates the height of the bar code measured in print dots (range 1 to 255).
        /// </summary>
        public int BarCodeHeight { get; set; } = 66;

        /// <summary>
        /// Selects one of three ways to print the alphanumeric representation of the barcode or to disable it altogether.
        /// </summary>
        /// <remarks>
        /// The options are as follows:
        /// o 0 = Disabled
        /// o 1 = Above the barcode
        /// o 2 = Below the barcode
        /// o 3 = Below and above the barcode
        /// </remarks>
        public int BarCodeHRIPosition { get; set; } = 1;

        /// <summary>
        /// Indicates the font to be used for the HRI string.
        /// </summary>
        /// <remarks>
        /// The options are as follows:
        /// - A
        /// - B
        /// - C
        /// </remarks>
        public char BarCodeHRIFont { get; set; } = 'C';

        /// <summary>
        /// Indicates the barcode or QR code standard.
        /// </summary>
        /// <remarks>
        /// Choose from one of the following:
        /// - UPC-A => 65
        /// - UPC-E / 66
        /// - EAN13 / 67
        /// - EAN8 / 68
        /// - CODE39 / 69
        /// - ITF / 70
        /// - CODABAR / 71
        /// - CODE93 / 72
        /// - CODE128 / 73
        /// - 74 to 78 *
        /// - QRCODE1 / 91
        /// - QRCODE2 / 92
        /// </remarks>
        public string CodeType { get; set; } = "CODE39";
    }
}