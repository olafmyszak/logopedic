using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogopedicBackend.Migrations
{
    /// <inheritdoc />
    public partial class PatientContactInfoSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.Sql("ALTER TABLE \"Patients\" ADD COLUMN IF NOT EXISTS \"SearchText\" text");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION patients_search_trigger() RETURNS trigger AS $$
                BEGIN
                    NEW.""SearchText"" :=
                        lower(
                            public.unaccent(coalesce(NEW.""FullName"",'') || ' ' || coalesce(NEW.""ContactInfo"",''))
                        );
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS patients_search_text_bu ON ""Patients"";
                CREATE TRIGGER patients_search_text_bu
                BEFORE INSERT OR UPDATE ON ""Patients""
                FOR EACH ROW EXECUTE FUNCTION patients_search_trigger();
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS patients_search_trgm_idx
                ON ""Patients"" USING gin (""SearchText"" gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchText",
                schema: "public",
                table: "Patients");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS patients_search_text_bu ON ""PATIENTS"";
                DROP FUNCTION IF EXISTS patients_search_trigger();
            ");
            
            migrationBuilder.Sql("DROP INDEX IF EXISTS patients_search_trgm_idx;");
        }
    }
}
