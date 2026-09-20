using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultFlairFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Flairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: every pre-existing community gets the fixed default-tag set too (newly
            // created communities get these via CommunityService.CreateAsync). Skips a name for a
            // community that already has a (custom) flair with that exact name, so the
            // (CommunityId, Name) unique index can't be violated.
            migrationBuilder.Sql(
                """
                INSERT INTO "Flairs" ("Id", "CommunityId", "Name", "ColorHex", "IsDefault", "CreatedByModId", "CreatedAtUtc")
                SELECT gen_random_uuid(), c."Id", names.name, '#6B7280', true, c."CreatedByUserId", now()
                FROM "Communities" c
                CROSS JOIN (VALUES ('שאלה'), ('דיון'), ('עזרה')) AS names(name)
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Flairs" f WHERE f."CommunityId" = c."Id" AND f."Name" = names.name
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Flairs");
        }
    }
}
