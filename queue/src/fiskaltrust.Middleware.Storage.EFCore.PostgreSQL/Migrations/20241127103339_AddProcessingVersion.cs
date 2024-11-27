using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL.Migrations
{
    public partial class AddProcessingVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessingVersion",
                table: "ftQueueItem",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ftQueueItem_ProcessingVersion",
                table: "ftQueueItem",
                column: "ProcessingVersion");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ftQueueItem_ProcessingVersion",
                table: "ftQueueItem");

            migrationBuilder.DropColumn(
                name: "ProcessingVersion",
                table: "ftQueueItem");
        }
    }
}
