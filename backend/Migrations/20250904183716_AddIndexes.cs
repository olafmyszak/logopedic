using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogopedicBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Patients_ContactInfo",
                schema: "public",
                table: "Patients",
                column: "ContactInfo");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FullName",
                schema: "public",
                table: "Patients",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DurationInMinutes",
                schema: "public",
                table: "Appointments",
                column: "DurationInMinutes");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StartTime",
                schema: "public",
                table: "Appointments",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                schema: "public",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Type",
                schema: "public",
                table: "Appointments",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_ContactInfo",
                schema: "public",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_FullName",
                schema: "public",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DurationInMinutes",
                schema: "public",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_StartTime",
                schema: "public",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status",
                schema: "public",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Type",
                schema: "public",
                table: "Appointments");
        }
    }
}
