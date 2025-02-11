using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationSecurityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldstoMemberModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoggedIn",
                table: "Members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoggedIn",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Members");
        }
    }
}
