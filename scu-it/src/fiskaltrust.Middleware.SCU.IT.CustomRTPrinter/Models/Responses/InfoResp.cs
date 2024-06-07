using System;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses
{
    [XmlRoot("infoResp")]
    public class InfoResp : IResponse
    {
        [XmlAttribute("success")]
        public bool Success { get; set; }

        [XmlAttribute("status")]
        public int Status { get; set; }

        [XmlElement("serialNumber")]
        public string SerialNumber { get; set; }

        [XmlElement("fiscalized")]
        public Booleano FiscalizedBooleano { get; set; }

        [XmlIgnore()]
        public bool Fiscalized { get => FiscalizedBooleano.ToBoolean(); set => FiscalizedBooleano = value.ToBooleano(); }

        [XmlElement("fpuState")]
        public string FpuState { get; set; } // TODO: make enum

        [XmlElement("hwInitExhausted")]
        public Booleano HwInitExhaustedBooleano { get; set; }

        [XmlIgnore()]
        public bool HwInitExhausted { get => HwInitExhaustedBooleano.ToBoolean(); set => HwInitExhaustedBooleano = value.ToBooleano(); }

        [XmlElement("hwInitNumber")]
        public int HwInitNumber { get; set; }

        [XmlElement("fmPresent")]
        public Booleano FmPresentBooleano { get; set; }

        [XmlIgnore()]
        public bool FmPresent { get => FmPresentBooleano.ToBoolean(); set => FmPresentBooleano = value.ToBooleano(); }

        [XmlElement("mfExhausted")]
        public Booleano MfExhaustedBooleano { get; set; }

        [XmlIgnore()]
        public bool MfExhausted { get => MfExhaustedBooleano.ToBoolean(); set => MfExhaustedBooleano = value.ToBooleano(); }

        [XmlElement("zSetNumber")]
        public int ZSetNumber { get; set; }

        [XmlElement("ejPresent")]
        public Booleano EjPresentBooleano { get; set; }

        [XmlIgnore()]
        public bool EjPresent { get => EjPresentBooleano.ToBoolean(); set => EjPresentBooleano = value.ToBooleano(); }

        [XmlElement("ejFull")]
        public Booleano EjFullBooleano { get; set; }

        [XmlIgnore()]
        public bool EjFull { get => EjFullBooleano.ToBoolean(); set => EjFullBooleano = value.ToBooleano(); }

        [XmlElement("ejFilling")]
        public string EjFilling { get; set; }

        [XmlElement("simulation")]
        public Booleano SimulationBooleano { get; set; }

        [XmlIgnore()]
        public bool Simulation { get => SimulationBooleano.ToBoolean(); set => SimulationBooleano = value.ToBooleano(); }

        [XmlElement("demoMode")]
        public Booleano DemoModeBooleano { get; set; }

        [XmlIgnore()]
        public bool DemoMode { get => DemoModeBooleano.ToBoolean(); set => DemoModeBooleano = value.ToBooleano(); }

        [XmlElement("vatSplit")]
        public Booleano VatSplitBooleano { get; set; }

        [XmlIgnore()]
        public bool VatSplit { get => VatSplitBooleano.ToBoolean(); set => VatSplitBooleano = value.ToBooleano(); }

        [XmlElement("privatekey")]
        public Booleano PrivateKeyBooleano { get; set; }

        [XmlIgnore()]
        public bool PrivateKey { get => PrivateKeyBooleano.ToBoolean(); set => PrivateKeyBooleano = value.ToBooleano(); }

        [XmlElement("certificate")]
        public Booleano CertificateBooleano { get; set; }

        [XmlIgnore()]
        public bool Certificate { get => CertificateBooleano.ToBoolean(); set => CertificateBooleano = value.ToBooleano(); }

        [XmlElement("certValidFrom")]
        public string CertValidFromString { get; set; }

        [XmlElement("certValidTo")]
        public string CertValidToString { get; set; }

        [XmlElement("certExpired")]
        public Booleano CertExpiredBooleano { get; set; }

        [XmlIgnore()]
        public bool CertExpired { get => CertExpiredBooleano.ToBoolean(); set => CertExpiredBooleano = value.ToBooleano(); }

        [XmlElement("dateProg")]
        public string DateProgString { get; set; }

        [XmlElement("minWaste")]
        public int? MinWaste { get; set; } // optional

        [XmlElement("maxWaste")]
        public int? MaxWaste { get; set; } // optional

        [XmlElement("delaysNum")]
        public int? DelaysNum { get; set; } // optional

        [XmlElement("advancesNum")]
        public int? AdvancesNum { get; set; } // optional

        [XmlElement("timeSync")]
        public Booleano TimeSyncBooleano { get; set; }

        [XmlIgnore()]
        public bool TimeSync { get => TimeSyncBooleano.ToBoolean(); set => TimeSyncBooleano = value.ToBooleano(); }

        [XmlElement("vatNumberDealer")]
        public string VatNumberDealer { get; set; }

        [XmlElement("pointOfSaleNum")]
        public string PointOfSaleNum { get; set; }

        [XmlElement("vatNumberRetailer")]
        public string VatNumberRetailer { get; set; }

        [XmlElement("retailerDescription")]
        public string RetailerDescription { get; set; }

        [XmlElement("retailerPostalCode")]
        public string RetailerPostalCode { get; set; }
    }
}