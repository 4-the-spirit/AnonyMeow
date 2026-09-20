using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReactionsSingleUniquePerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-existing rows may have more than one reaction per (TargetType, TargetId,
            // AppUserId) from before this constraint existed — the new unique index below can't
            // be created until those are collapsed to one. Keeps the most recently created
            // reaction per group and drops the rest.
            migrationBuilder.Sql(
                """
                DELETE FROM "Reactions" r
                USING "Reactions" newer
                WHERE r."TargetType" = newer."TargetType"
                  AND r."TargetId" = newer."TargetId"
                  AND r."AppUserId" = newer."AppUserId"
                  AND r."Id" <> newer."Id"
                  AND (r."CreatedAtUtc", r."Id") < (newer."CreatedAtUtc", newer."Id");
                """);

            migrationBuilder.DropIndex(
                name: "IX_Reactions_TargetType_TargetId_AppUserId_Emoji",
                table: "Reactions");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_TargetType_TargetId_AppUserId",
                table: "Reactions",
                columns: new[] { "TargetType", "TargetId", "AppUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reactions_TargetType_TargetId_AppUserId",
                table: "Reactions");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_TargetType_TargetId_AppUserId_Emoji",
                table: "Reactions",
                columns: new[] { "TargetType", "TargetId", "AppUserId", "Emoji" },
                unique: true);
        }
    }
}
