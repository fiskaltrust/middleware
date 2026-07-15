using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("fixedLines")]
    public class FixedLines : IFiscalRecord, ICommand
    {
        // Description max 44 bytes (per Custom RT XML protocol).
        [XmlAttribute("description")]
        public string Description { get; set; }

        // pitch values include: 1=normal, 2=bold, B=customer fiscal code/VAT,
        // C=customer description, Q–X=tax credit fields, Z=half height.
        [XmlAttribute("pitch")]
        public string Pitch { get; set; }

        [XmlAttribute("extra_pitch")]
        public string ExtraPitch { get; set; }

        // Only serialize extra_pitch when set (required only for pitch Q–X).
        public bool ShouldSerializeExtraPitch() => !string.IsNullOrEmpty(ExtraPitch);
    }
}
