using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTextLengthLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres won't ALTER COLUMN TYPE on a column a generated column depends on (the
            // Phase 10 full-text-search SearchVector tsvector columns depend on every column being
            // narrowed here), so those generated columns/indexes are dropped and recreated around
            // the narrowing ALTER COLUMNs below.
            migrationBuilder.DropIndex(name: "IX_Posts_SearchVector", table: "Posts");
            migrationBuilder.DropIndex(name: "IX_Communities_SearchVector", table: "Communities");
            migrationBuilder.DropIndex(name: "IX_Comments_SearchVector", table: "Comments");

            migrationBuilder.DropColumn(name: "SearchVector", table: "Posts");
            migrationBuilder.DropColumn(name: "SearchVector", table: "Communities");
            migrationBuilder.DropColumn(name: "SearchVector", table: "Comments");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BodyMarkdown",
                table: "Posts",
                type: "character varying(40000)",
                maxLength: 40000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RulesText",
                table: "Communities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Communities",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Communities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyMarkdown",
                table: "Comments",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Posts",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "BodyMarkdown" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Communities",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Comments",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "BodyMarkdown" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_SearchVector",
                table: "Communities",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_SearchVector",
                table: "Comments",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Posts_SearchVector", table: "Posts");
            migrationBuilder.DropIndex(name: "IX_Communities_SearchVector", table: "Communities");
            migrationBuilder.DropIndex(name: "IX_Comments_SearchVector", table: "Comments");

            migrationBuilder.DropColumn(name: "SearchVector", table: "Posts");
            migrationBuilder.DropColumn(name: "SearchVector", table: "Communities");
            migrationBuilder.DropColumn(name: "SearchVector", table: "Comments");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "BodyMarkdown",
                table: "Posts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40000)",
                oldMaxLength: 40000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RulesText",
                table: "Communities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Communities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Communities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyMarkdown",
                table: "Comments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10000)",
                oldMaxLength: 10000);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Posts",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "BodyMarkdown" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Communities",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Comments",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "BodyMarkdown" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_SearchVector",
                table: "Communities",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_SearchVector",
                table: "Comments",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
