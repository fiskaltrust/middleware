using Microsoft.EntityFrameworkCore.Migrations;

namespace fiskaltrust.Middleware.Storage.EFCore.SQLServer.Migrations
{
    public partial class MECleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnuType",
                table: "ftSignaturCreationUnitME");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ftSignaturCreationUnitME");

            migrationBuilder.DropColumn(
                name: "ModeConfigurationJson",
                table: "ftSignaturCreationUnitME");

            migrationBuilder.DropColumn(
                name: "TseInfoJson",
                table: "ftSignaturCreationUnitME");

            migrationBuilder.RenameColumn(
                name: "ftOrdinalNumber",
                table: "ftJournalME",
                newName: "YearlyOrdinalNumber");

            migrationBuilder.RenameColumn(
                name: "ftInvoiceNumber",
                table: "ftJournalME",
                newName: "InvoiceNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "YearlyOrdinalNumber",
                table: "ftJournalME",
                newName: "ftOrdinalNumber");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "ftJournalME",
                newName: "ftInvoiceNumber");

            migrationBuilder.AddColumn<string>(
                name: "EnuType",
                table: "ftSignaturCreationUnitME",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "ftSignaturCreationUnitME",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModeConfigurationJson",
                table: "ftSignaturCreationUnitME",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TseInfoJson",
                table: "ftSignaturCreationUnitME",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
