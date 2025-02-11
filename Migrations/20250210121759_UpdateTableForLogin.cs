using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationSecurityApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableForLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLockedOut",
                table: "Members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedLogin",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChange",
                table: "Members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "IsLockedOut",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LastFailedLogin",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LastPasswordChange",
                table: "Members");
        }
    }
}
