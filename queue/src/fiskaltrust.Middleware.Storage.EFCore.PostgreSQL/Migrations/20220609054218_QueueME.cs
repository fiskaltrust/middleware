using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL.Migrations
{
    public partial class QueueME : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ftJournalME",
                columns: table => new
                {
                    ftJournalMEId = table.Column<Guid>(type: "uuid", nullable: false),
                    cbReference = table.Column<string>(type: "text", nullable: true),
                    ftInvoiceNumber = table.Column<string>(type: "text", nullable: true),
                    ftOrdinalNumber = table.Column<int>(type: "integer", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    IIC = table.Column<string>(type: "text", nullable: true),
                    FIC = table.Column<string>(type: "text", nullable: true),
                    FCDC = table.Column<string>(type: "text", nullable: true),
                    JournalType = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftJournalME", x => x.ftJournalMEId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueueME",
                columns: table => new
                {
                    ftQueueMEId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftSignaturCreationUnitMEId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastHash = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ftQueueME", x => x.ftQueueMEId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitME",
                columns: table => new
                {
                    ftSignaturCreationUnitMEId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    TseInfoJson = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    ModeConfigurationJson = table.Column<string>(type: "text", nullable: true),
                    IssuerTin = table.Column<string>(type: "text", nullable: true),
                    BusinessUnitCode = table.Column<string>(type: "text", nullable: true),
                    TcrIntId = table.Column<string>(type: "text", nullable: true),
                    SoftwareCode = table.Column<string>(type: "text", nullable: true),
                    MaintainerCode = table.Column<string>(type: "text", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EnuType = table.Column<string>(type: "text", nullable: true),
                    TcrCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftSignaturCreationUnitME", x => x.ftSignaturCreationUnitMEId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ftJournalME");

            migrationBuilder.DropTable(
                name: "ftQueueME");

            migrationBuilder.DropTable(
                name: "ftSignaturCreationUnitME");
        }
    }
}
