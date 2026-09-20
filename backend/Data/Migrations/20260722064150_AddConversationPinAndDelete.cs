using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnonyMeow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationPinAndDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedByAAtUtc",
                table: "Conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedByBAtUtc",
                table: "Conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PinnedByAAtUtc",
                table: "Conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PinnedByBAtUtc",
                table: "Conversations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedByAAtUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "DeletedByBAtUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "PinnedByAAtUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "PinnedByBAtUtc",
                table: "Conversations");
        }
    }
}
