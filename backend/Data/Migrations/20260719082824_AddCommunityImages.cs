using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Communities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconImageUrl",
                table: "Communities",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "IconImageUrl",
                table: "Communities");
        }
    }
}
