using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedProtestStatementInfoFlairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: every pre-existing community gets the three new default tags too (newly
            // created communities get these via CommunityService.CreateAsync +
            // DefaultFlairs.NameToColorHex). Skips a name for a community that already has a
            // (custom) flair with that exact name, so the (CommunityId, Name) unique index can't
            // be violated.
            migrationBuilder.Sql(
                """
                INSERT INTO "Flairs" ("Id", "CommunityId", "Name", "ColorHex", "IsDefault", "CreatedByModId", "CreatedAtUtc")
                SELECT gen_random_uuid(), c."Id", names.name, names.color, true, c."CreatedByUserId", now()
                FROM "Communities" c
                CROSS JOIN (VALUES ('מחאה', '#F97316'), ('הצהרה', '#A855F7'), ('מידע', '#14B8A6')) AS names(name, color)
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Flairs" f WHERE f."CommunityId" = c."Id" AND f."Name" = names.name
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Flairs"
                WHERE "IsDefault" = true AND "Name" IN ('מחאה', 'הצהרה', 'מידע');
                """);
        }
    }
}
