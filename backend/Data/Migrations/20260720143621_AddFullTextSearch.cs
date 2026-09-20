using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
            migrationBuilder.DropIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Communities_SearchVector",
                table: "Communities");

            migrationBuilder.DropIndex(
                name: "IX_Comments_SearchVector",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Comments");
        }
    }
}
