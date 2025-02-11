using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationSecurityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordHistories_Members_UserId",
                table: "PasswordHistories");

            migrationBuilder.DropIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordHistories_Members_UserId",
                table: "PasswordHistories",
                column: "UserId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
