using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

#nullable disable

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Models
{
    [Serializable]
    [XmlType("TicketBai", Namespace = "urn:ticketbai:emision", AnonymousType = true)]
    [XmlRoot("TicketBai", Namespace = "urn:ticketbai:emision")]
    public class TicketBaiRequest
    {
        [Required]
        [XmlElement("Cabecera")]
        public Cabecera Cabecera { get; set; }

        [Required]
        [XmlElement("Sujetos")]
        public Sujetos Sujetos { get; set; }

        [Required]
        [XmlElement("Factura")]
        public Factura Factura { get; set; }

        [Required]
        [XmlElement("HuellaTBAI")]
        public HuellaTBAI HuellaTBAI { get; set; }

        [Required]
        [XmlElement("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature { get; set; }
    }

    [Serializable]
    [XmlType("Cabecera", Namespace = "urn:ticketbai:emision")]
    public class Cabecera
    {
        [Required]
        [XmlElement("IDVersionTBAI")]
        public IDVersionTicketBaiType IDVersionTBAI { get; set; }
    }

    [Serializable]
    [XmlType("IDVersionTicketBaiType", Namespace = "urn:ticketbai:emision")]
    public enum IDVersionTicketBaiType
    {

        [XmlEnum("1.2")]
        Item1Period2,
    }

    [Serializable]
    [XmlType("Sujetos", Namespace = "urn:ticketbai:emision")]
    public class Sujetos
    {
        [Required]
        [XmlElement("Emisor")]
        public Emisor Emisor { get; set; }

        [XmlArray("Destinatarios")]
        [XmlArrayItem("IDDestinatario")]
        public List<IDDestinatario> Destinatarios { get; set; }

        [XmlElement("VariosDestinatarios")]
        public SiNoType VariosDestinatarios { get; set; }

        [XmlElement("EmitidaPorTercerosODestinatario")]
        public EmitidaPorTercerosType EmitidaPorTercerosODestinatario { get; set; }
    }


    [Serializable]
    [XmlType("Emisor", Namespace = "urn:ticketbai:emision")]
    public class Emisor
    {
        /// <summary>
        /// Secuencia de 9 dígitos o letras mayúsculas</para>
        /// </summary>
        [MinLength(9)]
        [MaxLength(9)]
        [RegularExpression("(([a-z|A-Z]{1}\\d{7}[a-z|A-Z]{1})|(\\d{8}[a-z|A-Z]{1})|([a-z|A-Z]{1}\\d{8}))")]
        [Required]
        [XmlElement("NIF")]
        public string NIF { get; set; }

        [MaxLength(120)]
        [Required]
        [XmlElement("ApellidosNombreRazonSocial")]
        public string ApellidosNombreRazonSocial { get; set; }
    }


    [Serializable]
    [XmlType("Destinatarios", Namespace = "urn:ticketbai:emision")]
    public class Destinatarios
    {
        [Required]
        [XmlElement("IDDestinatario")]
        public List<IDDestinatario> IDDestinatario { get; set; }
    }

    [Serializable]
    [XmlType("IDDestinatario", Namespace = "urn:ticketbai:emision")]
    public class IDDestinatario
    {
        /// <summary>
        /// Secuencia de 9 dígitos o letras mayúsculas
        /// </summary>
        [MinLength(9)]
        [MaxLength(9)]
        [RegularExpression("(([a-z|A-Z]{1}\\d{7}[a-z|A-Z]{1})|(\\d{8}[a-z|A-Z]{1})|([a-z|A-Z]{1}\\d{8}))")]
        [XmlElement("NIF")]
        public string NIF { get; set; }

        [XmlElement("IDOtro")]
        public IDOtro IDOtro { get; set; }

        [MaxLength(120)]
        [Required]
        [XmlElement("ApellidosNombreRazonSocial")]
        public string ApellidosNombreRazonSocial { get; set; }

        [MaxLength(20)]
        [XmlElement("CodigoPostal")]
        public string CodigoPostal { get; set; }

        [MaxLength(250)]
        [XmlElement("Direccion")]
        public string Direccion { get; set; }
    }


    [Serializable]
    [XmlType("IDOtro", Namespace = "urn:ticketbai:emision")]
    public class IDOtro
    {
        [XmlElement("CodigoPais")]
        public CountryType2 CodigoPais { get; set; }

        [Required]
        [XmlElement("IDType")]
        public IDTypeType IDType { get; set; }

        [MaxLength(20)]
        [Required]
        [XmlElement("ID")]
        public string ID { get; set; }
    }

    [Serializable]
    [XmlType("CountryType2", Namespace = "urn:ticketbai:emision")]
    public enum CountryType2
    {
        AF,
        AL,
        DE,
        AD,
        AO,
        AI,
        AQ,
        AG,
        SA,
        DZ,
        AR,
        AM,
        AW,
        AU,
        AT,
        AZ,
        BS,
        BH,
        BD,
        BB,
        BE,
        BZ,
        BJ,
        BM,
        BY,
        BO,
        BA,
        BW,
        BV,
        BR,
        BN,
        BG,
        BF,
        BI,
        BT,
        CV,
        KY,
        KH,
        CM,
        CA,
        CF,
        CC,
        CO,
        KM,
        CG,
        CD,
        CK,
        KP,
        KR,
        CI,
        CR,
        HR,
        CU,
        TD,
        CZ,
        CL,
        CN,
        CY,
        CW,
        DK,
        DM,
        DO,
        EC,
        EG,
        AE,
        ER,
        SK,
        SI,
        ES,
        US,
        EE,
        ET,
        FO,
        PH,
        FI,
        FJ,
        FR,
        GA,
        GM,
        GE,
        GS,
        GH,
        GI,
        GD,
        GR,
        GL,
        GU,
        GT,
        GG,
        GN,
        GQ,
        GW,
        GY,
        HT,
        HM,
        HN,
        HK,
        HU,
        IN,
        ID,
        IR,
        IQ,
        IE,
        IM,
        IS,
        IL,
        IT,
        JM,
        JP,
        JE,
        JO,
        KZ,
        KE,
        KG,
        KI,
        KW,
        LA,
        LS,
        LV,
        LB,
        LR,
        LY,
        LI,
        LT,
        LU,
        XG,
        MO,
        MK,
        MG,
        MY,
        MW,
        MV,
        ML,
        MT,
        FK,
        MP,
        MA,
        MH,
        MU,
        MR,
        YT,
        UM,
        MX,
        FM,
        MD,
        MC,
        MN,
        ME,
        MS,
        MZ,
        MM,
        NA,
        NR,
        CX,
        NP,
        NI,
        NE,
        NG,
        NU,
        NF,
        NO,
        NC,
        NZ,
        IO,
        OM,
        NL,
        BQ,
        PK,
        PW,
        PA,
        PG,
        PY,
        PE,
        PN,
        PF,
        PL,
        PT,
        PR,
        QA,
        GB,
        RW,
        RO,
        RU,
        SB,
        SV,
        WS,
        AS,
        KN,
        SM,
        SX,
        PM,
        VC,
        SH,
        LC,
        ST,
        SN,
        RS,
        SC,
        SL,
        SG,
        SY,
        SO,
        LK,
        SZ,
        ZA,
        SD,
        SS,
        SE,
        CH,
        SR,
        TH,
        TW,
        TZ,
        TJ,
        PS,
        TF,
        TL,
        TG,
        TK,
        TO,
        TT,
        TN,
        TC,
        TM,
        TR,
        TV,
        UA,
        UG,
        UY,
        UZ,
        VU,
        VA,
        VE,
        VN,
        VG,
        VI,
        WF,
        YE,
        DJ,
        ZM,
        ZW,
        QU,
        XB,
        XU,
        XN,
        AX,
        BL,
        EH,
        GF,
        GP,
        MF,
        MQ,
        RE,
        SJ,
    }

    [Serializable]
    [XmlType("IDTypeType", Namespace = "urn:ticketbai:emision")]
    public enum IDTypeType
    {
        /// <summary>
        /// IFZ BEZ - NIF IVA
        /// </summary>
        [XmlEnum("02")]
        NIF,

        /// <summary>
        /// Pasaportea - Pasaporte
        /// </summary>
        [XmlEnum("03")]
        Pasaporte,

        /// <summary>
        /// Egoitza dagoen herrialdeak edo lurraldeak emandako nortasun agiri ofiziala - Documento oficial de identificación expedido por el país o territorio de residencia
        /// </summary>
        [XmlEnum("04")]
        DocumentoOficialDeIdentificacion,

        /// <summary>
        /// Egoitza ziurtagiria - Certificado de residencia
        /// </summary>
        [XmlEnum("05")]
        CertificadoDeResidencia,

        /// <summary>
        /// Beste frogagiri bat - Otro documento probatorio
        /// </summary>
        [XmlEnum("06")]
        OtroDocumentoProbatorio
    }

    [Serializable]
    [XmlType("SiNoType", Namespace = "urn:ticketbai:emision")]
    public enum SiNoType
    {
        S,
        N,
    }

    [Serializable]
    [XmlType("EmitidaPorTercerosType", Namespace = "urn:ticketbai:emision")]
    public enum EmitidaPorTercerosType
    {
        /// <summary>
        /// Hartzaileak berak emandako faktura - Factura emitida por el propio emisor o emisora
        /// </summary>
        N,

        /// <summary>
        /// Hirugarren batek egindako faktura - Factura emitida por tercero o tercera - Invoice generated by a third party
        /// </summary>
        T,

        /// <summary>
        /// Eragiketaren hartzaileak emandako faktura - Factura emitida por el destinatario o la destinataria de la operación
        /// </summary>
        D
    }


    [Serializable]
    [XmlType("Factura", Namespace = "urn:ticketbai:emision")]
    public class Factura
    {
        [Required]
        public CabeceraFacturaType CabeceraFactura { get; set; }

        [Required]
        public DatosFacturaType DatosFactura { get; set; }

        [Required]
        public TipoDesgloseType TipoDesglose { get; set; }
    }

    [Serializable]
    [XmlType("CabeceraFacturaType", Namespace = "urn:ticketbai:emision")]
    public class CabeceraFacturaType
    {
        [MaxLength(20)]
        [XmlElement("SerieFactura")]
        public string SerieFactura { get; set; }

        [MaxLength(20)]
        [Required]
        [XmlElement("NumFactura")]
        public string NumFactura { get; set; }
        
        /// <summary>
        /// Format dd-mm-aaaaa
        /// </summary>
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression("\\d{2,2}-\\d{2,2}-\\d{4,4}")]
        [Required]
        [XmlElement("FechaExpedicionFactura")]
        public string FechaExpedicionFactura { get; set; }
        
        /// <summary>
        /// Format hh:mm:ss
        /// </summary>
        [RegularExpression("\\d{2,2}:\\d{2,2}:\\d{2,2}")]
        [Required]
        [XmlElement("HoraExpedicionFactura")]
        public string HoraExpedicionFactura { get; set; }

        [XmlElement("FacturaSimplificada")]
        public SiNoType FacturaSimplificada { get; set; }

        [XmlElement("FacturaEmitidaSustitucionSimplificada")]
        public SiNoType FacturaEmitidaSustitucionSimplificada { get; set; }

        [XmlElement("FacturaRectificativa")]
        public FacturaRectificativaType FacturaRectificativa { get; set; }

        [XmlArray("FacturasRectificadasSustituidas")]
        [XmlArrayItem("IDFacturaRectificadaSustituida")]
        public List<IDFacturaRectificadaSustituidaType> FacturasRectificadasSustituidas { get; set; }
    }


    [Serializable]
    [XmlType("FacturaRectificativaType", Namespace = "urn:ticketbai:emision")]
    public class FacturaRectificativaType
    {
        [Required]
        [XmlElement("Codigo")]
        public ClaveTipoFacturaType Codigo { get; set; }

        [Required]
        [XmlElement("Tipo")]
        public ClaveTipoRectificativaType Tipo { get; set; }

        [XmlElement("ImporteRectificacionSustitutiva")]
        public ImporteRectificacionSustitutivaType ImporteRectificacionSustitutiva { get; set; }
    }

    [Serializable]
    [XmlType("ClaveTipoFacturaType", Namespace = "urn:ticketbai:emision")]
    public enum ClaveTipoFacturaType
    {
        /// <summary>
        /// Faktura zuzentzailea: zuzenbidean oinarritutako akatsa eta BEZaren Foru Arauaren 80.artikuluko Bat, Bi eta Sei - Factura rectificativa: error fundado en derecho y Art. 80 Uno, Dos y Seis de la Norma Foral del IVA
        /// </summary>
        R1,

        /// <summary>
        /// Faktura zuzentzailea: BEZari buruzko Foru Arauko 80. artikuluko Hiru - Factura rectificativa: artículo 80 Tres de la Norma Foral del IVA
        /// </summary>
        R2,

        /// <summary>
        /// Faktura zuzentzailea: BEZari buruzko Foru Arauko 80. artikuluko Lau - Factura rectificativa: artículo 80 Cuatro de la Norma Foral del IVA
        /// </summary>
        R3,

        /// <summary>
        /// Faktura zuzentzailea: Gainerakoak - Factura rectificativa: Resto
        /// </summary>
        R4,

        /// <summary>
        /// Faktura zuzentzailea faktura erraztuetan - Factura rectificativa en facturas simplificadas
        /// </summary>
        R5,
    }

    [Serializable]
    [XmlType("ClaveTipoRectificativaType", Namespace = "urn:ticketbai:emision")]
    public enum ClaveTipoRectificativaType
    {
        /// <summary>
        /// <para>Ordezkatzeagatiko faktura zuzentzailea - Factura rectificativa por sustitución
        /// </summary>
        S,

        /// <summary>
        /// <para>Ezberdintasunengatiko faktura zuzentzailea - Factura rectificativa por diferencias
        /// </summary>
        I
    }

    [Serializable]
    [XmlType("ImporteRectificacionSustitutivaType", Namespace = "urn:ticketbai:emision")]
    public class ImporteRectificacionSustitutivaType
    {
        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [Required]
        [XmlElement("BaseRectificada")]
        public string BaseRectificada { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [Required]
        [XmlElement("CuotaRectificada")]
        public string CuotaRectificada { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [XmlElement("CuotaRecargoRectificada")]
        public string CuotaRecargoRectificada { get; set; }
    }

    [Serializable]
    [XmlType("FacturasRectificadasSustituidasType", Namespace = "urn:ticketbai:emision")]
    public class FacturasRectificadasSustituidasType
    {
        [Required]
        [XmlElement("IDFacturaRectificadaSustituida")]
        public List<IDFacturaRectificadaSustituidaType> IDFacturaRectificadaSustituida { get; set; }
    }

    [Serializable]
    [XmlType("IDFacturaRectificadaSustituidaType", Namespace = "urn:ticketbai:emision")]
    public class IDFacturaRectificadaSustituidaType
    {
        [MaxLength(20)]
        [XmlElement("SerieFactura")]
        public string SerieFactura { get; set; }

        [MaxLength(20)]
        [Required]
        [XmlElement("NumFactura")]
        public string NumFactura { get; set; }
        
        /// <summary>
        /// Format dd-mm-aaaaa
        /// </summary>
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression("\\d{2,2}-\\d{2,2}-\\d{4,4}")]
        [Required]
        [XmlElement("FechaExpedicionFactura")]
        public string FechaExpedicionFactura { get; set; }
    }


    [Serializable]
    [XmlType("DatosFacturaType", Namespace = "urn:ticketbai:emision")]
    public class DatosFacturaType
    {
        /// <summary>
        /// Format dd-mm-aaaaa
        /// </summary>
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression("\\d{2,2}-\\d{2,2}-\\d{4,4}")]
        [XmlElement("FechaOperacion")]
        public string FechaOperacion { get; set; }

        [MaxLength(250)]
        [Required]
        [XmlElement("DescripcionFactura")]
        public string DescripcionFactura { get; set; }

        [XmlArray("DetallesFactura")]
        [XmlArrayItem("IDDetalleFactura")]
        public List<IDDetalleFacturaType> DetallesFactura { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [Required]
        [XmlElement("ImporteTotalFactura")]
        public string ImporteTotalFactura { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [XmlElement("RetencionSoportada")]
        public string RetencionSoportada { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [XmlElement("BaseImponibleACoste")]
        public string BaseImponibleACoste { get; set; }

        [Required]
        [XmlArray("Claves")]
        [XmlArrayItem("IDClave")]
        public List<IDClaveType> Claves { get; set; }
    }

    [Serializable]
    [XmlType("DetallesFacturaType", Namespace = "urn:ticketbai:emision")]
    public class DetallesFacturaType
    {
        [Required]
        [XmlElement("IDDetalleFactura")]
        public List<IDDetalleFacturaType> IDDetalleFactura { get; set; }
    }

    [Serializable]
    [XmlType("IDDetalleFacturaType", Namespace = "urn:ticketbai:emision")]
    public class IDDetalleFacturaType
    {
        [MaxLength(250)]
        [Required]
        [XmlElement("DescripcionDetalle")]
        public string DescripcionDetalle { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,8})?")]
        [Required]
        [XmlElement("Cantidad")]
        public string Cantidad { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,8})?")]
        [Required]
        [XmlElement("ImporteUnitario")]
        public string ImporteUnitario { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,8})?")]
        [XmlElement("Descuento")]
        public string Descuento { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,8})?")]
        [Required]
        [XmlElement("ImporteTotal")]
        public string ImporteTotal { get; set; }
    }

    [Serializable]
    [XmlType("ClavesType", Namespace = "urn:ticketbai:emision")]
    public class ClavesType
    {
        [Required]
        [XmlElement("IDClave")]
        public List<IDClaveType> IDClave { get; set; }
    }

    [Serializable]
    [XmlType("IDClaveType", Namespace = "urn:ticketbai:emision")]
    public class IDClaveType
    {
        [Required]
        [XmlElement("ClaveRegimenIvaOpTrascendencia")]
        public IdOperacionesTrascendenciaTributariaType ClaveRegimenIvaOpTrascendencia { get; set; }
    }

    [Serializable]
    [XmlType("IdOperacionesTrascendenciaTributariaType", Namespace = "urn:ticketbai:emision")]
    public enum IdOperacionesTrascendenciaTributariaType
    {
        /// <summary>
        /// Erregimen orokorreko eragiketa eta hurrengo balioetan jaso gabe dagoen beste edozein kasu - Operación de régimen general y cualquier otro supuesto que no esté recogido en los siguientes valores
        /// </summary>
        [XmlEnum("01")]
        Item01,

        /// <summary>
        /// Esportazioa - Exportación
        /// </summary>
        [XmlEnum("02")]
        Item02,

        /// <summary>
        /// Erabilitako ondasunen, arte objektuen, zaharkinen eta bilduma objektuen araudi berezia aplikatzen zaien eragiketak - Operaciones a las que se aplique el régimen especial de bienes usados, objetos de arte, antigüedades y objetos de colección
        /// </summary>
        [XmlEnum("03")]
        Item03,

        /// <summary>
        /// Inbertsio urrearen araubide berezia - Régimen especial del oro de inversión
        /// </summary>
        [XmlEnum("04")]
        Item04,

        /// <summary>
        /// Bidaia-agentzien araubide berezia - Régimen especial de las agencias de viajes
        /// </summary>
        [XmlEnum("05")]
        Item05,

        /// <summary>
        /// BEZeko erakundeen multzoaren araudi berezia (maila aurreratua) - Régimen especial grupo de entidades en IVA (Nivel Avanzado)
        /// </summary>
        [XmlEnum("06")]
        Item06,

        /// <summary>
        /// Kutxa-irizpidearen araubide berezia - Régimen especial del criterio de caja
        /// </summary>
        [XmlEnum("07")]
        Item07,

        /// <summary>
        /// Ekoizpen, Zerbitzu eta Inportazioaren gaineko Zergari / Kanarietako Zeharkako Zerga Orokorrari lotutako eragiketak - Operaciones sujetas al IPSI/IGIC (Impuesto sobre la Producción, los Servicios y la Importación / Impuesto General Indirecto Canario)
        /// </summary>
        [XmlEnum("08")]
        Item08,

        /// <summary>
        /// Besteren izenean eta kontura ari diren bidai agentziek egindako zerbitzuen fakturazioa(Fakturazio Araudiko 3. xedapen gehigarria) - Facturación de las prestaciones de servicios de agencias de viaje que actúan como mediadoras en nombre y por cuenta ajena (disposición adicional 3ª del Reglamento de Facturación)
        /// </summary>
        [XmlEnum("09")]
        Item09,

        /// <summary>
        /// Hirugarrenen kontura kobratzea ordainsari profesionalak edo jabetza industrialetik eratorritako eskubideak, egilearenak edo bazkideen, bazkideen edo elkargokideen kontura kobratzeko eginkizun horiek betetzen dituzten sozietate, elkarte, elkargo profesional edo bestelako erakundeek egindakoak - Cobros por cuenta de terceros o terceras de honorarios profesionales o de derechos derivados de la propiedad industrial, de autor u otros por cuenta de sus socios, socias, asociados, asociadas, colegiados o colegiadas efectuados por sociedades, asociaciones, colegios profesionales u otras entidades que realicen estas funciones de cobro
        /// </summary>
        [XmlEnum("10")]
        Item10,

        /// <summary>
        /// Negozio lokala errentatzeko eragiketak, atxikipenari lotuak - Operaciones de arrendamiento de local de negocio sujetos a retención
        /// </summary>
        [XmlEnum("11")]
        Item11,

        /// <summary>
        /// Negozio lokala errentatzeko eragiketak, atxikipenari lotu gabeak - Operaciones de arrendamiento de local de negocio no sujetos a retención
        /// </summary>
        [XmlEnum("12")]
        Item12,

        /// <summary>
        /// Negozio lokala errentatzeko eragiketak, atxikipenari lotuak eta lotu gabeak - Operaciones de arrendamiento de local de negocio sujetas y no sujetas a retención
        /// </summary>
        [XmlEnum("13")]
        Item13,

        /// <summary>
        /// Hartzailea administrazio publiko bat denean ordaintzeke dauden BEZdun fakturak, obra ziurtagirietakoak - Factura con IVA pendiente de devengo en certificaciones de obra cuyo destinatario sea una Administración Pública
        /// </summary>
        [XmlEnum("14")]
        Item14,

        /// <summary>
        /// Segidako traktuko eragiketetan ordaintzeke dagoen BEZdun faktura - Factura con IVA pendiente de devengo en operaciones de tracto sucesivo
        /// </summary>
        [XmlEnum("15")]
        Item15,

        /// <summary>
        /// IX. tituluko XI. kapituluan aurreikusitako araubideren bati atxikitako eragiketa (OSS eta IOSS) - Operación acogida a alguno de los regímenes previstos en el Capítulo XI del Título IX (OSS e IOSS)
        /// </summary>
        [XmlEnum("17")]
        Item17,

        /// <summary>
        /// Nekazaritza, abeltzaintza eta arrantzaren araubide berezian dauden jardueren eragiketak (NAAAB) - Operaciones de actividades incluidas en el Régimen Especial de Agricultura, Ganadería y Pesca (REAGYP)
        /// </summary>
        [XmlEnum("19")]
        Item19,

        /// <summary>
        /// Baliokidetasun errekarguko eragiketak - Operaciones en recargo de equivalencia
        /// </summary>
        [XmlEnum("51")]
        Item51,

        /// <summary>
        /// Erregimen erraztuko eragiketak - Operaciones en régimen simplificado
        /// </summary>
        [XmlEnum("52")]
        Item52,

        /// <summary>
        /// BEZaren ondorioetarako enpresari edo profesionaltzat jotzen ez diren pertsona edo erakundeek egindako eragiketak - Operaciones realizadas por personas o entidades que no tengan la consideración de empresarios, empresarias o profesionales a efectos del IVA
        /// </summary>
        [XmlEnum("53")]
        Item53
    }

    [Serializable]
    [XmlType("TipoDesgloseType", Namespace = "urn:ticketbai:emision")]
    public class TipoDesgloseType
    {
        [XmlElement("DesgloseFactura")]
        public DesgloseFacturaType DesgloseFactura { get; set; }

        [XmlElement("DesgloseTipoOperacion")]
        public DesgloseTipoOperacionType DesgloseTipoOperacion { get; set; }
    }

    [Serializable]
    [XmlType("DesgloseFacturaType", Namespace = "urn:ticketbai:emision")]
    public class DesgloseFacturaType
    {

        [XmlElement("Sujeta")]
        public SujetaType Sujeta { get; set; }

        [XmlArray("NoSujeta")]
        [XmlArrayItem("DetalleNoSujeta")]
        public List<DetalleNoSujeta> NoSujeta { get; set; }
    }

    [Serializable]
    [XmlType("SujetaType", Namespace = "urn:ticketbai:emision")]
    public class SujetaType
    {
        [XmlArray("Exenta")]
        [XmlArrayItem("DetalleExenta")]
        public List<DetalleExentaType> Exenta { get; set; }

        [XmlArray("NoExenta")]
        [XmlArrayItem("DetalleNoExenta")]
        public List<DetalleNoExentaType> NoExenta { get; set; }
    }

    [Serializable]
    [XmlType("ExentaType", Namespace = "urn:ticketbai:emision")]
    public class ExentaType
    {
        [Required]
        [XmlElement("DetalleExenta")]
        public List<DetalleExentaType> DetalleExenta { get; set; }
    }

    [Serializable]
    [XmlType("DetalleExentaType", Namespace = "urn:ticketbai:emision")]
    public class DetalleExentaType
    {
        [Required]
        [XmlElement("CausaExencion")]
        public CausaExencionType CausaExencion { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [Required]
        [XmlElement("BaseImponible")]
        public string BaseImponible { get; set; }
    }

    [Serializable]
    [XmlType("CausaExencionType", Namespace = "urn:ticketbai:emision")]
    public enum CausaExencionType
    {
        /// <summary>
        /// BEZaren Foru Arauaren 20. artikuluak salbuetsia - Exenta por el artículo 20 de la Norma Foral del IVA
        /// </summary>
        E1,

        /// <summary>
        /// BEZaren Foru Arauaren 21. artikuluak salbuetsia - Exenta por el artículo 21 de la Norma Foral del IVA
        /// </summary>
        E2,

        /// <summary>
        /// BEZaren Foru Arauaren 22. artikuluak salbuetsia - Exenta por el artículo 22 de la Norma Foral del IVA
        /// </summary>
        E3,

        /// <summary>
        /// BEZaren Foru Arauaren 23. eta 24. artikuluek salbuetsia - Exenta por el artículo 23 y 24 de la Norma Foral del IVA
        /// </summary>
        E4,

        /// <summary>
        /// BEZaren Foru Arauaren 25. artikuluak salbuetsia - Exenta por el artículo 25 de la Norma Foral del IVA
        /// </summary>
        E5,

        /// <summary>
        /// Beste arrazoi bategatik salbuetsia - Exenta por otra causa
        /// </summary>
        E6
    }

    [Serializable]
    [XmlType("NoExentaType", Namespace = "urn:ticketbai:emision")]
    public class NoExentaType
    {
        [Required]
        [XmlElement("DetalleNoExenta")]
        public List<DetalleNoExentaType> DetalleNoExenta { get; set; }
    }

    [Serializable]
    [XmlType("DetalleNoExentaType", Namespace = "urn:ticketbai:emision")]
    public class DetalleNoExentaType
    {
        [Required]
        [XmlElement("TipoNoExenta")]
        public TipoOperacionSujetaNoExentaType TipoNoExenta { get; set; }

        [Required]
        [XmlArray("DesgloseIVA")]
        [XmlArrayItem("DetalleIVA")]
        public List<DetalleIVAType> DesgloseIVA { get; set; }
    }

    [Serializable]
    [XmlType("TipoOperacionSujetaNoExentaType", Namespace = "urn:ticketbai:emision")]
    public enum TipoOperacionSujetaNoExentaType
    {
        /// <summary>
        /// <para>Subjektu pasiboaren inbertsiorik gabe - Sin inversión del sujeto pasivo</para>
        /// </summary>
        S1,

        /// <summary>
        /// <para>Subjektu pasiboaren inbertsioarekin - Con inversión del sujeto pasivo</para>
        /// </summary>
        S2
    }

    [Serializable]
    [XmlType("DesgloseIVAType", Namespace = "urn:ticketbai:emision")]
    public class DesgloseIVAType
    {
        [Required]
        [XmlElement("DetalleIVA")]
        public List<DetalleIVAType> DetalleIVA { get; set; }
    }

    [Serializable]
    [XmlType("DetalleIVAType", Namespace = "urn:ticketbai:emision")]
    public class DetalleIVAType
    {
        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [Required]
        [XmlElement("BaseImponible")]
        public string BaseImponible { get; set; }

        [RegularExpression("\\d{1,3}(\\.\\d{0,2})?")]
        [XmlElement("TipoImpositivo")]
        public string TipoImpositivo { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [XmlElement("CuotaImpuesto")]
        public string CuotaImpuesto { get; set; }

        [RegularExpression("\\d{1,3}(\\.\\d{0,2})?")]
        [XmlElement("TipoRecargoEquivalencia")]
        public string TipoRecargoEquivalencia { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [XmlElement("CuotaRecargoEquivalencia")]
        public string CuotaRecargoEquivalencia { get; set; }

        [XmlElement("OperacionEnRecargoDeEquivalenciaORegimenSimplificado")]
        public SiNoType OperacionEnRecargoDeEquivalenciaORegimenSimplificado { get; set; }
    }


    [Serializable]
    [XmlType("NoSujetaType", Namespace = "urn:ticketbai:emision")]
    public class NoSujetaType
    {
        [Required]
        [XmlElement("DetalleNoSujeta")]
        public List<DetalleNoSujeta> DetalleNoSujeta { get; set; }
    }

    [Serializable]
    [XmlType("DetalleNoSujeta", Namespace = "urn:ticketbai:emision")]
    public class DetalleNoSujeta
    {
        [Required]
        [XmlElement("Causa")]
        public CausaNoSujetaType Causa { get; set; }

        [RegularExpression("(\\+|-)?\\d{1,12}(\\.\\d{0,2})?")]
        [Required]
        [XmlElement("Importe")]
        public string Importe { get; set; }
    }

    [Serializable]
    [XmlType("CausaNoSujetaType", Namespace = "urn:ticketbai:emision")]
    public enum CausaNoSujetaType
    {
        /// <summary>
        /// <para>BEZaren Foru Arauaren 7. artikuluak lotu gabea Lotu gabeko beste kasu batzuk - No sujeto por el artículo 7 de la Norma Foral de IVA Otros supuestos de no sujeción</para>
        /// </summary>
        OT,

        /// <summary>
        /// <para>Lokalizazio arauengatik lotu gabe - No sujeto por reglas de localización</para>
        /// </summary>
        RL,
    }

    [Serializable]
    [XmlType("DesgloseTipoOperacionType", Namespace = "urn:ticketbai:emision")]
    public class DesgloseTipoOperacionType
    {
        [XmlElement("PrestacionServicios")]
        public PrestacionServicios PrestacionServicios { get; set; }

        [XmlElement("Entrega")]
        public Entrega Entrega { get; set; }
    }

    [Serializable]
    [XmlType("PrestacionServicios", Namespace = "urn:ticketbai:emision")]
    public class PrestacionServicios
    {
        [XmlElement("Sujeta")]
        public SujetaType Sujeta { get; set; }

        [XmlArray("NoSujeta")]
        [XmlArrayItem("DetalleNoSujeta")]
        public List<DetalleNoSujeta> NoSujeta { get; set; }
    }

    [Serializable]
    [XmlType("Entrega", Namespace = "urn:ticketbai:emision")]
    public class Entrega
    {
        [XmlElement("Sujeta")]
        public SujetaType Sujeta { get; set; }

        [XmlArray("NoSujeta")]
        [XmlArrayItem("DetalleNoSujeta")]
        public List<DetalleNoSujeta> NoSujeta { get; set; }
    }

    [Serializable]
    [XmlType("EncadenamientoFacturaAnteriorType", Namespace = "urn:ticketbai:emision")]
    public class EncadenamientoFacturaAnteriorType
    {
        [MaxLength(20)]
        [XmlElement("SerieFacturaAnterior")]
        public string SerieFacturaAnterior { get; set; }

        [MaxLength(20)]
        [Required]
        [XmlElement("NumFacturaAnterior")]
        public string NumFacturaAnterior { get; set; }
        
        /// <summary>
        /// Format dd-mm-aaaaa
        /// </summary>
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression("\\d{2,2}-\\d{2,2}-\\d{4,4}")]
        [Required]
        [XmlElement("FechaExpedicionFacturaAnterior")]
        public string FechaExpedicionFacturaAnterior { get; set; }

        [MaxLength(100)]
        [Required]
        [XmlElement("SignatureValueFirmaFacturaAnterior")]
        public string SignatureValueFirmaFacturaAnterior { get; set; }
    }

    [Serializable]
    [XmlType("HuellaTBAI", Namespace = "urn:ticketbai:emision")]
    public class HuellaTBAI
    {
        [XmlElement("EncadenamientoFacturaAnterior")]
        public EncadenamientoFacturaAnteriorType EncadenamientoFacturaAnterior { get; set; }

        [Required]
        [XmlElement("Software")]
        public SoftwareFacturacionType Software { get; set; }

        [MaxLength(30)]
        [XmlElement("NumSerieDispositivo")]
        public string NumSerieDispositivo { get; set; }
    }

    [Serializable]
    [XmlType("SoftwareFacturacionType", Namespace = "urn:ticketbai:emision")]
    public class SoftwareFacturacionType
    {
        [MaxLength(20)]
        [Required]
        [XmlElement("LicenciaTBAI")]
        public string LicenciaTBAI { get; set; }

        [Required]
        [XmlElement("EntidadDesarrolladora")]
        public EntidadDesarrolladoraType EntidadDesarrolladora { get; set; }

        [MaxLength(120)]
        [Required]
        [XmlElement("Nombre")]
        public string Nombre { get; set; }

        [MaxLength(20)]
        [Required]
        [XmlElement("Version")]
        public string Version { get; set; }
    }

    [Serializable]
    [XmlType("EntidadDesarrolladoraType", Namespace = "urn:ticketbai:emision")]
    public class EntidadDesarrolladoraType
    {

        /// <summary>
        /// <para>NIF: Secuencia de 9 dígitos o letras mayúsculas</para>
        /// </summary>
        [MinLength(9)]
        [MaxLength(9)]
        [RegularExpression("(([a-z|A-Z]{1}\\d{7}[a-z|A-Z]{1})|(\\d{8}[a-z|A-Z]{1})|([a-z|A-Z]{1}\\d{8}))")]
        [XmlElement("NIF")]
        public string NIF { get; set; }

        [XmlElement("IDOtro")]
        public IDOtro IDOtro { get; set; }
    }
}
