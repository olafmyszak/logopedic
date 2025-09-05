using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogopedicBackend.Data;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", "public")
            .HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.SearchText)
            .HasComputedColumnSql(
                "lower(unaccent(coalesce(FullName,'') || ' ' || coalesce(ContactInfo,''))",
                true);

        builder.HasIndex(p => p.FullName);
        builder.HasIndex(p => p.ContactInfo);
    }
}