using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationSecurityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordChangeFieldstoMembersModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordChange",
                table: "Members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresPasswordChange",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Members");
        }
    }
}
