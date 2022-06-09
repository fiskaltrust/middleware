using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.SQLServer.Migrations
{
    public partial class QueueME : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ftJournalME",
                columns: table => new
                {
                    ftJournalMEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cbReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ftInvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ftOrdinalNumber = table.Column<int>(type: "int", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    IIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FCDC = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    ftQueueMEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftSignaturCreationUnitMEId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ftQueueME", x => x.ftQueueMEId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitME",
                columns: table => new
                {
                    ftSignaturCreationUnitMEId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    TseInfoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ModeConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuerTin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessUnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TcrIntId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoftwareCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaintainerCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnuType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TcrCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
