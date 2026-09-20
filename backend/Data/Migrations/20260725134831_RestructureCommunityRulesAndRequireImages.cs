using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestructureCommunityRulesAndRequireImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RulesText's free-form content can't be mechanically split into the new
            // Title/Description shape, so it's dropped rather than migrated — reviewed as a
            // deliberate, one-way loss of any existing rules text (dev-only data at the time of
            // writing this migration).
            migrationBuilder.DropColumn(
                name: "RulesText",
                table: "Communities");

            // Icon/banner become mandatory below (NOT NULL). Existing rows without one wouldn't
            // otherwise satisfy that constraint, so they're backfilled with an empty string
            // placeholder first — moderators can replace it via community settings afterward.
            migrationBuilder.Sql(
                """
                UPDATE "Communities" SET "IconImageUrl" = '' WHERE "IconImageUrl" IS NULL;
                UPDATE "Communities" SET "BannerImageUrl" = '' WHERE "BannerImageUrl" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IconImageUrl",
                table: "Communities",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BannerImageUrl",
                table: "Communities",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CommunityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityRules_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRules_CommunityId_Order",
                table: "CommunityRules",
                columns: new[] { "CommunityId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityRules");

            migrationBuilder.AlterColumn<string>(
                name: "IconImageUrl",
                table: "Communities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BannerImageUrl",
                table: "Communities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "RulesText",
                table: "Communities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
