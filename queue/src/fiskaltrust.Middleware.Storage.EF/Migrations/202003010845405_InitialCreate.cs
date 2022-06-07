using System;
using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ftActionJournal",
                c => new
                    {
                        ftActionJournalId = c.Guid(nullable: false),
                        ftQueueId = c.Guid(nullable: false),
                        ftQueueItemId = c.Guid(nullable: false),
                        Moment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Priority = c.Int(nullable: false),
                        Type = c.String(),
                        Message = c.String(),
                        DataBase64 = c.String(),
                        DataJson = c.String(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftActionJournalId, clustered: false);
            
            CreateTable(
                "dbo.ftCashBox",
                c => new
                    {
                        ftCashBoxId = c.Guid(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftCashBoxId, clustered: false);
            
            CreateTable(
                "dbo.ftJournalAT",
                c => new
                    {
                        ftJournalATId = c.Guid(nullable: false),
                        ftSignaturCreationUnitId = c.Guid(nullable: false),
                        Number = c.Long(nullable: false),
                        JWSHeaderBase64url = c.String(),
                        JWSPayloadBase64url = c.String(),
                        JWSSignatureBase64url = c.String(),
                        ftQueueId = c.Guid(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftJournalATId, clustered: false);
            
            CreateTable(
                "dbo.ftJournalDE",
                c => new
                    {
                        ftJournalDEId = c.Guid(nullable: false),
                        Number = c.Long(nullable: false),
                        FileName = c.String(),
                        FileExtension = c.String(),
                        FileContentBase64 = c.String(),
                        ftQueueItemId = c.Guid(nullable: false),
                        ftQueueId = c.Guid(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftJournalDEId, clustered: false);
            
            CreateTable(
                "dbo.ftJournalFR",
                c => new
                    {
                        ftJournalFRId = c.Guid(nullable: false),
                        JWT = c.String(),
                        JsonData = c.String(),
                        ReceiptType = c.String(),
                        Number = c.Long(nullable: false),
                        ftQueueItemId = c.Guid(nullable: false),
                        ftQueueId = c.Guid(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftJournalFRId, clustered: false);

            CreateTable(
                "dbo.ftJournalME",
                c => new
                {
                    ftJournalMEId = c.Guid(nullable: false),
                    cbReference = c.String(),
                    ftInvoiceNumber = c.String(),
                    ftOrdinalNumber = c.Int(),
                    ftQueueItemId = c.Guid(nullable: false),
                    ftQueueId = c.Guid(nullable: false),
                    Number = c.Long(nullable: false),
                    TimeStamp = c.Long(nullable: false),
                    IIC = c.String(),
                    FIC = c.String(),
                    FCDC = c.String(),
                    JournalType = c.Long(nullable: false)
                })
                .PrimaryKey(t => t.ftJournalMEId, clustered: false);

            CreateTable(
                "dbo.ftQueueAT",
                c => new
                    {
                        ftQueueATId = c.Guid(nullable: false),
                        CashBoxIdentification = c.String(),
                        EncryptionKeyBase64 = c.String(),
                        SignAll = c.Boolean(nullable: false),
                        ClosedSystemKind = c.String(),
                        ClosedSystemValue = c.String(),
                        ClosedSystemNote = c.String(),
                        LastSettlementMonth = c.Int(nullable: false),
                        LastSettlementMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        LastSettlementQueueItemId = c.Guid(),
                        SSCDFailCount = c.Int(nullable: false),
                        SSCDFailMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        SSCDFailQueueItemId = c.Guid(),
                        SSCDFailMessageSent = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedCount = c.Int(nullable: false),
                        UsedFailedMomentMin = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedMomentMax = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedQueueItemId = c.Guid(),
                        UsedMobileCount = c.Int(nullable: false),
                        UsedMobileMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedMobileQueueItemId = c.Guid(),
                        MessageCount = c.Int(nullable: false),
                        MessageMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        LastSignatureHash = c.String(),
                        LastSignatureZDA = c.String(),
                        LastSignatureCertificateSerialNumber = c.String(),
                        ftCashNumerator = c.Long(nullable: false),
                        ftCashTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueATId, clustered: false);
            
            CreateTable(
                "dbo.ftQueueDE",
                c => new
                    {
                        ftQueueDEId = c.Guid(nullable: false),
                        ftSignaturCreationUnitDEId = c.Guid(),
                        LastHash = c.String(),
                        CashBoxIdentification = c.String(),
                        SSCDFailCount = c.Int(nullable: false),
                        SSCDFailMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        SSCDFailQueueItemId = c.Guid(),
                        UsedFailedCount = c.Int(nullable: false),
                        UsedFailedMomentMin = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedMomentMax = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedQueueItemId = c.Guid(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueDEId, clustered: false);
            
            CreateTable(
                "dbo.ftQueueFR",
                c => new
                    {
                        ftQueueFRId = c.Guid(nullable: false),
                        ftSignaturCreationUnitFRId = c.Guid(nullable: false),
                        Siret = c.String(),
                        CashBoxIdentification = c.String(),
                        TNumerator = c.Long(nullable: false),
                        TTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TCITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TCITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TCITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TCITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TCITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TCITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TLastHash = c.String(),
                        PNumerator = c.Long(nullable: false),
                        PTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PLastHash = c.String(),
                        INumerator = c.Long(nullable: false),
                        ITotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ICITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ICITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ICITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ICITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ICITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ICITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ILastHash = c.String(),
                        GNumerator = c.Long(nullable: false),
                        GLastHash = c.String(),
                        GShiftTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftCITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftCITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftCITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftCITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftCITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftCITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GShiftPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GLastShiftMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        GLastShiftQueueItemId = c.Guid(),
                        GDayTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayCITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayCITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayCITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayCITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayCITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayCITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GDayPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GLastDayMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        GLastDayQueueItemId = c.Guid(),
                        GMonthTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthCITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthCITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthCITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthCITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthCITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthCITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GMonthPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GLastMonthMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        GLastMonthQueueItemId = c.Guid(),
                        GYearTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearCITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearCITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearCITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearCITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearCITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearCITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GYearPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GLastYearMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        GLastYearQueueItemId = c.Guid(),
                        BNumerator = c.Long(nullable: false),
                        BTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BCITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BCITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BCITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BCITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BCITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BCITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BPITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BPITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BPITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BPITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BLastHash = c.String(),
                        LNumerator = c.Long(nullable: false),
                        LLastHash = c.String(),
                        ANumerator = c.Long(nullable: false),
                        ALastHash = c.String(),
                        ATotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ACITotalNormal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ACITotalReduced1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ACITotalReduced2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ACITotalReducedS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ACITotalZero = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ACITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        APITotalCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        APITotalNonCash = c.Decimal(nullable: false, precision: 18, scale: 2),
                        APITotalInternal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        APITotalUnknown = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ALastMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        ALastQueueItemId = c.Guid(),
                        XNumerator = c.Long(nullable: false),
                        XTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        XLastHash = c.String(),
                        CNumerator = c.Long(nullable: false),
                        CTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CLastHash = c.String(),
                        UsedFailedCount = c.Int(nullable: false),
                        UsedFailedMomentMin = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedMomentMax = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedQueueItemId = c.Guid(),
                        MessageCount = c.Int(nullable: false),
                        MessageMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueFRId, clustered: false);

            CreateTable(
                "dbo.ftQueueME",
                c => new
                {
                    ftQueueMEId = c.Guid(nullable: false),
                    ftSignaturCreationUnitMEId = c.Guid(),
                    LastHash = c.String(),
                    SSCDFailCount = c.Int(),
                    SSCDFailMoment = c.Long(),
                    SSCDFailQueueItemId = c.Guid(),
                    UsedFailedCount = c.Int(),
                    UsedFailedMomentMin = c.Long(nullable: false),
                    UsedFailedMomentMax = c.Long(nullable: false),
                    UsedFailedQueueItemId = c.Guid(),
                    DailyClosingNumber = c.Int(),
                })
                .PrimaryKey(t => t.ftQueueMEId, clustered: false);

            CreateTable(
                "dbo.ftQueueItem",
                c => new
                    {
                        ftQueueItemId = c.Guid(nullable: false),
                        ftQueueId = c.Guid(nullable: false),
                        ftQueueRow = c.Long(nullable: false),
                        ftQueueMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ftQueueTimeout = c.Int(nullable: false),
                        ftWorkMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        ftDoneMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        cbReceiptMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        cbTerminalID = c.String(),
                        cbReceiptReference = c.String(),
                        country = c.String(),
                        version = c.String(),
                        request = c.String(),
                        requestHash = c.String(),
                        response = c.String(),
                        responseHash = c.String(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueItemId, clustered: false);
            
            CreateTable(
                "dbo.ftQueue",
                c => new
                    {
                        ftQueueId = c.Guid(nullable: false),
                        ftCashBoxId = c.Guid(nullable: false),
                        ftCurrentRow = c.Long(nullable: false),
                        ftQueuedRow = c.Long(nullable: false),
                        ftReceiptNumerator = c.Long(nullable: false),
                        ftReceiptTotalizer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ftReceiptHash = c.String(),
                        StartMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        StopMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        CountryCode = c.String(),
                        Timeout = c.Int(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueId, clustered: false);
            
            CreateTable(
                "dbo.ftReceiptJournal",
                c => new
                    {
                        ftReceiptJournalId = c.Guid(nullable: false),
                        ftReceiptMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ftReceiptNumber = c.Long(nullable: false),
                        ftReceiptTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ftQueueId = c.Guid(nullable: false),
                        ftQueueItemId = c.Guid(nullable: false),
                        ftReceiptHash = c.String(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftReceiptJournalId, clustered: false);
            
            CreateTable(
                "dbo.ftSignaturCreationUnitAT",
                c => new
                    {
                        ftSignaturCreationUnitATId = c.Guid(nullable: false),
                        Url = c.String(),
                        ZDA = c.String(),
                        SN = c.String(),
                        CertificateBase64 = c.String(),
                        Mode = c.Int(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftSignaturCreationUnitATId, clustered: false);
            
            CreateTable(
                "dbo.ftSignaturCreationUnitDE",
                c => new
                    {
                        ftSignaturCreationUnitDEId = c.Guid(nullable: false),
                        Url = c.String(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftSignaturCreationUnitDEId, clustered: false);
            
            CreateTable(
                "dbo.ftSignaturCreationUnitFR",
                c => new
                    {
                        ftSignaturCreationUnitFRId = c.Guid(nullable: false),
                        Siret = c.String(),
                        PrivateKey = c.String(),
                        CertificateBase64 = c.String(),
                        CertificateSerialNumber = c.String(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftSignaturCreationUnitFRId, clustered: false);

            CreateTable(
                "dbo.ftSignaturCreationUnitME",
                c => new
                {
                    ftSignaturCreationUnitMEId = c.Guid(nullable: false),
                    Url = c.String(),
                    TimeStamp = c.Long(nullable: false),
                    Mode = c.Int(),
                    ModeConfigurationJson = c.String(),
                    IssuerTin = c.String(),
                    BusinessUnitCode = c.String(),
                    TcrIntId = c.String(),
                    SoftwareCode = c.String(),
                    MaintainerCode = c.String(),
                    ValidFrom = c.Long(),
                    ValidTo = c.Long(),
                    EnuType = c.String(),
                    TcrCode = c.String(),
                })
                .PrimaryKey(t => t.ftSignaturCreationUnitMEId, clustered: false);
    }
        
        public override void Down()
        {
            DropTable("dbo.ftSignaturCreationUnitFR");
            DropTable("dbo.ftSignaturCreationUnitDE");
            DropTable("dbo.ftSignaturCreationUnitAT");
            DropTable("dbo.ftSignaturCreationUnitmE");
            DropTable("dbo.ftReceiptJournal");
            DropTable("dbo.ftQueue");
            DropTable("dbo.ftQueueItem");
            DropTable("dbo.ftQueueFR");
            DropTable("dbo.ftQueueDE");
            DropTable("dbo.ftQueueAT");
            DropTable("dbo.ftQueueME");
            DropTable("dbo.ftJournalFR");
            DropTable("dbo.ftJournalDE");
            DropTable("dbo.ftJournalAT");
            DropTable("dbo.ftJournalME");
            DropTable("dbo.ftCashBox");
            DropTable("dbo.ftActionJournal");
        }
    }
}
