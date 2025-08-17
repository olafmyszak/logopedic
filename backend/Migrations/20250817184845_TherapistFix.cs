using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogopedicBackend.Migrations
{
    /// <inheritdoc />
    public partial class TherapistFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Therapist_TherapistId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Therapist_TherapistId",
                table: "Patients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Therapist",
                table: "Therapist");

            migrationBuilder.RenameTable(
                name: "Therapist",
                newName: "Therapists");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Therapists",
                table: "Therapists",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Therapists_TherapistId",
                table: "Appointments",
                column: "TherapistId",
                principalTable: "Therapists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Therapists_TherapistId",
                table: "Patients",
                column: "TherapistId",
                principalTable: "Therapists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Therapists_TherapistId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Therapists_TherapistId",
                table: "Patients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Therapists",
                table: "Therapists");

            migrationBuilder.RenameTable(
                name: "Therapists",
                newName: "Therapist");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Therapist",
                table: "Therapist",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Therapist_TherapistId",
                table: "Appointments",
                column: "TherapistId",
                principalTable: "Therapist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Therapist_TherapistId",
                table: "Patients",
                column: "TherapistId",
                principalTable: "Therapist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
