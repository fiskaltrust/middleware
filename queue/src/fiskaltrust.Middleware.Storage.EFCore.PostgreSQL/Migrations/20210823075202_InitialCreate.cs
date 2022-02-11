using System;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable
namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountMasterData",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Zip = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    VatId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountMasterData", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "AgencyMasterData",
                columns: table => new
                {
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Zip = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    VatId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyMasterData", x => x.AgencyId);
                });

            migrationBuilder.CreateTable(
                name: "FailedFinishTransaction",
                columns: table => new
                {
                    cbReceiptReference = table.Column<string>(type: "text", nullable: false),
                    TransactionNumber = table.Column<long>(type: "bigint", nullable: true),
                    FinishMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Request = table.Column<string>(type: "text", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedFinishTransaction", x => x.cbReceiptReference);
                });

            migrationBuilder.CreateTable(
                name: "FailedStartTransaction",
                columns: table => new
                {
                    cbReceiptReference = table.Column<string>(type: "text", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashBoxIdentification = table.Column<string>(type: "text", nullable: true),
                    Request = table.Column<string>(type: "text", nullable: true),
                    StartMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedStartTransaction", x => x.cbReceiptReference);
                });

            migrationBuilder.CreateTable(
                name: "ftActionJournal",
                columns: table => new
                {
                    ftActionJournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Moment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    DataBase64 = table.Column<string>(type: "text", nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftActionJournal", x => x.ftActionJournalId);
                });

            migrationBuilder.CreateTable(
                name: "ftCashBox",
                columns: table => new
                {
                    ftCashBoxId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftCashBox", x => x.ftCashBoxId);
                });

            migrationBuilder.CreateTable(
                name: "ftJournalAT",
                columns: table => new
                {
                    ftJournalATId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftSignaturCreationUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    JWSHeaderBase64url = table.Column<string>(type: "text", nullable: true),
                    JWSPayloadBase64url = table.Column<string>(type: "text", nullable: true),
                    JWSSignatureBase64url = table.Column<string>(type: "text", nullable: true),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftJournalAT", x => x.ftJournalATId);
                });

            migrationBuilder.CreateTable(
                name: "ftJournalDE",
                columns: table => new
                {
                    ftJournalDEId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileExtension = table.Column<string>(type: "text", nullable: true),
                    FileContentBase64 = table.Column<string>(type: "text", nullable: true),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftJournalDE", x => x.ftJournalDEId);
                });

            migrationBuilder.CreateTable(
                name: "ftJournalFR",
                columns: table => new
                {
                    ftJournalFRId = table.Column<Guid>(type: "uuid", nullable: false),
                    JWT = table.Column<string>(type: "text", nullable: true),
                    JsonData = table.Column<string>(type: "text", nullable: true),
                    ReceiptType = table.Column<string>(type: "text", nullable: true),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftJournalFR", x => x.ftJournalFRId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueue",
                columns: table => new
                {
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftCashBoxId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftCurrentRow = table.Column<long>(type: "bigint", nullable: false),
                    ftQueuedRow = table.Column<long>(type: "bigint", nullable: false),
                    ftReceiptNumerator = table.Column<long>(type: "bigint", nullable: false),
                    ftReceiptTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    ftReceiptHash = table.Column<string>(type: "text", nullable: true),
                    StartMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StopMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    Timeout = table.Column<int>(type: "integer", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftQueue", x => x.ftQueueId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueueAT",
                columns: table => new
                {
                    ftQueueATId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashBoxIdentification = table.Column<string>(type: "text", nullable: true),
                    EncryptionKeyBase64 = table.Column<string>(type: "text", nullable: true),
                    SignAll = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedSystemKind = table.Column<string>(type: "text", nullable: true),
                    ClosedSystemValue = table.Column<string>(type: "text", nullable: true),
                    ClosedSystemNote = table.Column<string>(type: "text", nullable: true),
                    LastSettlementMonth = table.Column<int>(type: "integer", nullable: false),
                    LastSettlementMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastSettlementQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SSCDFailCount = table.Column<int>(type: "integer", nullable: false),
                    SSCDFailMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SSCDFailQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SSCDFailMessageSent = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "integer", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsedMobileCount = table.Column<int>(type: "integer", nullable: false),
                    UsedMobileMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedMobileQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    MessageMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastSignatureHash = table.Column<string>(type: "text", nullable: true),
                    LastSignatureZDA = table.Column<string>(type: "text", nullable: true),
                    LastSignatureCertificateSerialNumber = table.Column<string>(type: "text", nullable: true),
                    ftCashNumerator = table.Column<long>(type: "bigint", nullable: false),
                    ftCashTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftQueueAT", x => x.ftQueueATId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueueDE",
                columns: table => new
                {
                    ftQueueDEId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftSignaturCreationUnitDEId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastHash = table.Column<string>(type: "text", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "text", nullable: true),
                    SSCDFailCount = table.Column<int>(type: "integer", nullable: false),
                    SSCDFailMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SSCDFailQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "integer", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    DailyClosingNumber = table.Column<int>(type: "integer", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftQueueDE", x => x.ftQueueDEId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueueFR",
                columns: table => new
                {
                    ftQueueFRId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftSignaturCreationUnitFRId = table.Column<Guid>(type: "uuid", nullable: false),
                    Siret = table.Column<string>(type: "text", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "text", nullable: true),
                    TNumerator = table.Column<long>(type: "bigint", nullable: false),
                    TTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    TCITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    TCITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    TCITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    TCITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    TCITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    TCITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    TPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    TPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    TPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    TPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    TLastHash = table.Column<string>(type: "text", nullable: true),
                    PNumerator = table.Column<long>(type: "bigint", nullable: false),
                    PTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    PPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    PPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    PPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    PPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    PLastHash = table.Column<string>(type: "text", nullable: true),
                    INumerator = table.Column<long>(type: "bigint", nullable: false),
                    ITotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    ICITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    ICITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    ICITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    ICITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    ICITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    ICITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    IPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    IPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    IPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    IPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    ILastHash = table.Column<string>(type: "text", nullable: true),
                    GNumerator = table.Column<long>(type: "bigint", nullable: false),
                    GLastHash = table.Column<string>(type: "text", nullable: true),
                    GShiftTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftCITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftCITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftCITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftCITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftCITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftCITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    GShiftPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GLastShiftMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GLastShiftQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    GDayTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayCITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayCITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayCITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayCITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayCITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayCITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    GDayPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GLastDayMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GLastDayQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    GMonthTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthCITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthCITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthCITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthCITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthCITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthCITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    GMonthPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GLastMonthMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GLastMonthQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    GYearTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearCITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearCITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearCITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearCITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearCITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearCITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    GYearPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    GLastYearMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GLastYearQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    BNumerator = table.Column<long>(type: "bigint", nullable: false),
                    BTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    BCITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    BCITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    BCITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    BCITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    BCITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    BCITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    BPITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    BPITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    BPITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    BPITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    BLastHash = table.Column<string>(type: "text", nullable: true),
                    LNumerator = table.Column<long>(type: "bigint", nullable: false),
                    LLastHash = table.Column<string>(type: "text", nullable: true),
                    ANumerator = table.Column<long>(type: "bigint", nullable: false),
                    ALastHash = table.Column<string>(type: "text", nullable: true),
                    ATotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    ACITotalNormal = table.Column<decimal>(type: "numeric", nullable: false),
                    ACITotalReduced1 = table.Column<decimal>(type: "numeric", nullable: false),
                    ACITotalReduced2 = table.Column<decimal>(type: "numeric", nullable: false),
                    ACITotalReducedS = table.Column<decimal>(type: "numeric", nullable: false),
                    ACITotalZero = table.Column<decimal>(type: "numeric", nullable: false),
                    ACITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    APITotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    APITotalNonCash = table.Column<decimal>(type: "numeric", nullable: false),
                    APITotalInternal = table.Column<decimal>(type: "numeric", nullable: false),
                    APITotalUnknown = table.Column<decimal>(type: "numeric", nullable: false),
                    ALastMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ALastQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    XNumerator = table.Column<long>(type: "bigint", nullable: false),
                    XTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    XLastHash = table.Column<string>(type: "text", nullable: true),
                    CNumerator = table.Column<long>(type: "bigint", nullable: false),
                    CTotalizer = table.Column<decimal>(type: "numeric", nullable: false),
                    CLastHash = table.Column<string>(type: "text", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "integer", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    MessageMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftQueueFR", x => x.ftQueueFRId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueueItem",
                columns: table => new
                {
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueRow = table.Column<long>(type: "bigint", nullable: false),
                    ftQueueMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ftQueueTimeout = table.Column<int>(type: "integer", nullable: false),
                    ftWorkMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ftDoneMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cbReceiptMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    cbTerminalID = table.Column<string>(type: "text", nullable: true),
                    cbReceiptReference = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "text", nullable: true),
                    request = table.Column<string>(type: "text", nullable: true),
                    requestHash = table.Column<string>(type: "text", nullable: true),
                    response = table.Column<string>(type: "text", nullable: true),
                    responseHash = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftQueueItem", x => x.ftQueueItemId);
                });

            migrationBuilder.CreateTable(
                name: "ftReceiptJournal",
                columns: table => new
                {
                    ftReceiptJournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftReceiptMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ftReceiptNumber = table.Column<long>(type: "bigint", nullable: false),
                    ftReceiptTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftReceiptHash = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftReceiptJournal", x => x.ftReceiptJournalId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitAT",
                columns: table => new
                {
                    ftSignaturCreationUnitATId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    ZDA = table.Column<string>(type: "text", nullable: true),
                    SN = table.Column<string>(type: "text", nullable: true),
                    CertificateBase64 = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftSignaturCreationUnitAT", x => x.ftSignaturCreationUnitATId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitDE",
                columns: table => new
                {
                    ftSignaturCreationUnitDEId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    TseInfoJson = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    ModeConfigurationJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftSignaturCreationUnitDE", x => x.ftSignaturCreationUnitDEId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitFR",
                columns: table => new
                {
                    ftSignaturCreationUnitFRId = table.Column<Guid>(type: "uuid", nullable: false),
                    Siret = table.Column<string>(type: "text", nullable: true),
                    PrivateKey = table.Column<string>(type: "text", nullable: true),
                    CertificateBase64 = table.Column<string>(type: "text", nullable: true),
                    CertificateSerialNumber = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftSignaturCreationUnitFR", x => x.ftSignaturCreationUnitFRId);
                });

            migrationBuilder.CreateTable(
                name: "OpenTransaction",
                columns: table => new
                {
                    cbReceiptReference = table.Column<string>(type: "text", nullable: false),
                    TransactionNumber = table.Column<long>(type: "bigint", nullable: false),
                    StartTransactionSignatureBase64 = table.Column<string>(type: "text", nullable: true),
                    StartMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenTransaction", x => x.cbReceiptReference);
                });

            migrationBuilder.CreateTable(
                name: "OutletMasterData",
                columns: table => new
                {
                    OutletId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutletName = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Zip = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    VatId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutletMasterData", x => x.OutletId);
                });

            migrationBuilder.CreateTable(
                name: "PosSystemMasterData",
                columns: table => new
                {
                    PosSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    SoftwareVersion = table.Column<string>(type: "text", nullable: true),
                    BaseCurrency = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSystemMasterData", x => x.PosSystemId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountMasterData");

            migrationBuilder.DropTable(
                name: "AgencyMasterData");

            migrationBuilder.DropTable(
                name: "FailedFinishTransaction");

            migrationBuilder.DropTable(
                name: "FailedStartTransaction");

            migrationBuilder.DropTable(
                name: "ftActionJournal");

            migrationBuilder.DropTable(
                name: "ftCashBox");

            migrationBuilder.DropTable(
                name: "ftJournalAT");

            migrationBuilder.DropTable(
                name: "ftJournalDE");

            migrationBuilder.DropTable(
                name: "ftJournalFR");

            migrationBuilder.DropTable(
                name: "ftQueue");

            migrationBuilder.DropTable(
                name: "ftQueueAT");

            migrationBuilder.DropTable(
                name: "ftQueueDE");

            migrationBuilder.DropTable(
                name: "ftQueueFR");

            migrationBuilder.DropTable(
                name: "ftQueueItem");

            migrationBuilder.DropTable(
                name: "ftReceiptJournal");

            migrationBuilder.DropTable(
                name: "ftSignaturCreationUnitAT");

            migrationBuilder.DropTable(
                name: "ftSignaturCreationUnitDE");

            migrationBuilder.DropTable(
                name: "ftSignaturCreationUnitFR");

            migrationBuilder.DropTable(
                name: "OpenTransaction");

            migrationBuilder.DropTable(
                name: "OutletMasterData");

            migrationBuilder.DropTable(
                name: "PosSystemMasterData");
        }
    }
}
