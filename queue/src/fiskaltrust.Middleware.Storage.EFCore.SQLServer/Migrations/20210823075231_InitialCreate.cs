using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.SQLServer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountMasterData",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VatId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountMasterData", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "AgencyMasterData",
                columns: table => new
                {
                    AgencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VatId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyMasterData", x => x.AgencyId);
                });

            migrationBuilder.CreateTable(
                name: "FailedFinishTransaction",
                columns: table => new
                {
                    TransactionNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinishMoment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cbReceiptReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedFinishTransaction", x => x.TransactionNumber);
                });

            migrationBuilder.CreateTable(
                name: "FailedStartTransaction",
                columns: table => new
                {
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashBoxIdentification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartMoment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cbReceiptReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedStartTransaction", x => x.ftQueueItemId);
                });

            migrationBuilder.CreateTable(
                name: "ftActionJournal",
                columns: table => new
                {
                    ftActionJournalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Moment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    ftCashBoxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    ftJournalATId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftSignaturCreationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    JWSHeaderBase64url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JWSPayloadBase64url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JWSSignatureBase64url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    ftJournalDEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileExtension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileContentBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    ftJournalFRId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JWT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftCashBoxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftCurrentRow = table.Column<long>(type: "bigint", nullable: false),
                    ftQueuedRow = table.Column<long>(type: "bigint", nullable: false),
                    ftReceiptNumerator = table.Column<long>(type: "bigint", nullable: false),
                    ftReceiptTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ftReceiptHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StopMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timeout = table.Column<int>(type: "int", nullable: false),
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
                    ftQueueATId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashBoxIdentification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptionKeyBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignAll = table.Column<bool>(type: "bit", nullable: false),
                    ClosedSystemKind = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedSystemValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedSystemNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSettlementMonth = table.Column<int>(type: "int", nullable: false),
                    LastSettlementMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSettlementQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SSCDFailCount = table.Column<int>(type: "int", nullable: false),
                    SSCDFailMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SSCDFailQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SSCDFailMessageSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "int", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsedMobileCount = table.Column<int>(type: "int", nullable: false),
                    UsedMobileMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedMobileQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    MessageMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSignatureHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSignatureZDA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSignatureCertificateSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ftCashNumerator = table.Column<long>(type: "bigint", nullable: false),
                    ftCashTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    ftQueueDEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftSignaturCreationUnitDEId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSCDFailCount = table.Column<int>(type: "int", nullable: false),
                    SSCDFailMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SSCDFailQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "int", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DailyClosingNumber = table.Column<int>(type: "int", nullable: false),
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
                    ftQueueFRId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftSignaturCreationUnitFRId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Siret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TNumerator = table.Column<long>(type: "bigint", nullable: false),
                    TTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TCITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TCITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TCITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TCITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TCITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TCITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PNumerator = table.Column<long>(type: "bigint", nullable: false),
                    PTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INumerator = table.Column<long>(type: "bigint", nullable: false),
                    ITotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ICITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ICITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ICITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ICITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ICITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ICITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ILastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GNumerator = table.Column<long>(type: "bigint", nullable: false),
                    GLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GShiftTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftCITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftCITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftCITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftCITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftCITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftCITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GShiftPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GLastShiftMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GLastShiftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GDayTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayCITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayCITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayCITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayCITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayCITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayCITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GDayPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GLastDayMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GLastDayQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GMonthTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthCITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthCITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthCITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthCITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthCITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthCITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GMonthPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GLastMonthMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GLastMonthQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GYearTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearCITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearCITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearCITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearCITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearCITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearCITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GYearPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GLastYearMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GLastYearQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BNumerator = table.Column<long>(type: "bigint", nullable: false),
                    BTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BCITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BCITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BCITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BCITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BCITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BCITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BPITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BPITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BPITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BPITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LNumerator = table.Column<long>(type: "bigint", nullable: false),
                    LLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ANumerator = table.Column<long>(type: "bigint", nullable: false),
                    ALastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ATotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACITotalNormal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACITotalReduced1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACITotalReduced2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACITotalReducedS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACITotalZero = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ACITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    APITotalCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    APITotalNonCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    APITotalInternal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    APITotalUnknown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ALastMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ALastQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    XNumerator = table.Column<long>(type: "bigint", nullable: false),
                    XTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    XLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CNumerator = table.Column<long>(type: "bigint", nullable: false),
                    CTotalizer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CLastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "int", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    MessageMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueRow = table.Column<long>(type: "bigint", nullable: false),
                    ftQueueMoment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ftQueueTimeout = table.Column<int>(type: "int", nullable: false),
                    ftWorkMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ftDoneMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cbReceiptMoment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cbTerminalID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cbReceiptReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requestHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    responseHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    ftReceiptJournalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftReceiptMoment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ftReceiptNumber = table.Column<long>(type: "bigint", nullable: false),
                    ftReceiptTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftReceiptHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    ftSignaturCreationUnitATId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZDA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
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
                    ftSignaturCreationUnitDEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    TseInfoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ModeConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftSignaturCreationUnitDE", x => x.ftSignaturCreationUnitDEId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitFR",
                columns: table => new
                {
                    ftSignaturCreationUnitFRId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Siret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    TransactionNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTransactionSignatureBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartMoment = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cbReceiptReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenTransaction", x => x.TransactionNumber);
                });

            migrationBuilder.CreateTable(
                name: "OutletMasterData",
                columns: table => new
                {
                    OutletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutletName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VatId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutletMasterData", x => x.OutletId);
                });

            migrationBuilder.CreateTable(
                name: "PosSystemMasterData",
                columns: table => new
                {
                    PosSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoftwareVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaseCurrency = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
