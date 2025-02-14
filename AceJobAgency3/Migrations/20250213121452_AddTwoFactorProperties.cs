using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AceJobAgency3.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFactorCode",
                table: "Members",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorCodeExpiry",
                table: "Members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorCode",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeExpiry",
                table: "Members");
        }
    }
}
