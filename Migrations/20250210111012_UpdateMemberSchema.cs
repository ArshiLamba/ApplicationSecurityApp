using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationSecurityApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMemberSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmPassword",
                table: "Members");

            migrationBuilder.RenameColumn(
                name: "HashedPassword",
                table: "Members",
                newName: "Password");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Members",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Members");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Members",
                newName: "HashedPassword");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmPassword",
                table: "Members",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
