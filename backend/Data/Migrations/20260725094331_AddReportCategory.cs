using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // Existing rows predate categorization — backfilled as Other (6), not the enum's
            // first value (Spam), since we have no real signal for their actual category.
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Reports");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Reports",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
