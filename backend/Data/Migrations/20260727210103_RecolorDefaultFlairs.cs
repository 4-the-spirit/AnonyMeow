using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecolorDefaultFlairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only: recolors pre-existing default flairs (previously all one shared grey,
            // #6B7280) to match the new per-name palette in DefaultFlairs.NameToColorHex.
            migrationBuilder.Sql(
                """
                UPDATE "Flairs" SET "ColorHex" = '#4169E1' WHERE "IsDefault" = true AND "Name" = 'שאלה';
                UPDATE "Flairs" SET "ColorHex" = '#22C55E' WHERE "IsDefault" = true AND "Name" = 'דיון';
                UPDATE "Flairs" SET "ColorHex" = '#EF4444' WHERE "IsDefault" = true AND "Name" = 'עזרה';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Flairs" SET "ColorHex" = '#6B7280'
                WHERE "IsDefault" = true AND "Name" IN ('שאלה', 'דיון', 'עזרה');
                """);
        }
    }
}
