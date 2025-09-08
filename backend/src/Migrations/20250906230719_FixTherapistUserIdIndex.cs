using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogopedicBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixTherapistUserIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Therapists_UserId",
                schema: "public",
                table: "Therapists");

            migrationBuilder.CreateIndex(
                name: "IX_Therapists_UserId",
                schema: "public",
                table: "Therapists",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Therapists_UserId",
                schema: "public",
                table: "Therapists");

            migrationBuilder.CreateIndex(
                name: "IX_Therapists_UserId",
                schema: "public",
                table: "Therapists",
                column: "UserId");
        }
    }
}
