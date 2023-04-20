using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL.Migrations
{
    public partial class Italy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ftJournalIT",
                columns: table => new
                {
                    ftJournalITId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftSignaturCreationUnitITId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<long>(type: "bigint", nullable: false),
                    ZRepNumber = table.Column<long>(type: "bigint", nullable: false),
                    JournalType = table.Column<long>(type: "bigint", nullable: false),
                    cbReceiptReference = table.Column<string>(type: "text", nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    ReceiptDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    ftQueueITId = table.Column<Guid>(type: "uuid", nullable: false),
                    ftSignaturCreationUnitITId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastHash = table.Column<string>(type: "text", nullable: true),
                    CashBoxIdentification = table.Column<string>(type: "text", nullable: true),
                    SSCDFailCount = table.Column<int>(type: "integer", nullable: false),
                    SSCDFailMoment = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SSCDFailQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsedFailedCount = table.Column<int>(type: "integer", nullable: false),
                    UsedFailedMomentMin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedMomentMax = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsedFailedQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    ftSignaturCreationUnitITId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    InfoJson = table.Column<string>(type: "text", nullable: true)
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
