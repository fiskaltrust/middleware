using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.SQLServer.Migrations
{
    public partial class Italy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ftJournalIT",
                columns: table => new
                {
                    ftJournalITId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftSignaturCreationUnitITId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecNumber = table.Column<int>(type: "int", nullable: false),
                    ZRecNumber = table.Column<int>(type: "int", nullable: false),
                    JournalType = table.Column<long>(type: "bigint", nullable: false),
                    cbReceiptReference = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RecordDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftJournalIT", x => x.ftJournalITId);
                });

            migrationBuilder.CreateTable(
                name: "ftQueueIT",
                columns: table => new
                {
                    ftQueueITId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ftSignaturCreationUnitITId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSCDFailCount = table.Column<int>(type: "int", nullable: false),
                    SSCDFailMoment = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SSCDFailQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "int", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftQueueIT", x => x.ftQueueITId);
                });

            migrationBuilder.CreateTable(
                name: "ftSignaturCreationUnitIT",
                columns: table => new
                {
                    ftSignaturCreationUnitITId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InfoJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ftSignaturCreationUnitIT", x => x.ftSignaturCreationUnitITId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ftJournalIT_cbReceiptReference",
                table: "ftJournalIT",
                column: "cbReceiptReference");

            migrationBuilder.CreateIndex(
                name: "IX_ftJournalIT_TimeStamp",
                table: "ftJournalIT",
                column: "TimeStamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ftJournalIT");

            migrationBuilder.DropTable(
                name: "ftQueueIT");

            migrationBuilder.DropTable(
                name: "ftSignaturCreationUnitIT");
        }
    }
}
