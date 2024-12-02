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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingVersion",
                table: "ftQueueItem");
        }
    }
}