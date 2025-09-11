using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
//
// namespace LogopedicBackend.Data;
//
// public class PatientConfiguration : IEntityTypeConfiguration<Patient>
// {
//     public void Configure(EntityTypeBuilder<Patient> builder)
//     {
//         builder.ToTable("Patients", "public")
//             .HasMany(p => p.Appointments)
//             .WithOne(a => a.Patient)
//             .HasForeignKey(a => a.PatientId)
//             .OnDelete(DeleteBehavior.Cascade);
//
//         builder.Property(p => p.SearchText)
//             .HasComputedColumnSql(
//                 "lower(unaccent(coalesce(FullName,'') || ' ' || coalesce(ContactInfo,''))",
//                 true);
//
//         builder.HasIndex(p => p.FullName);
//         builder.HasIndex(p => p.ContactInfo);
//     }
// }

namespace LogopedicBackend.Data;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", "public");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SearchText)
            .HasComputedColumnSql("lower(unaccent(coalesce(FullName,'') || ' ' || coalesce(ContactInfo,''))", true);

        builder.HasOne(p => p.Therapist)
            .WithMany(t => t.Patients)
            .HasForeignKey(p => p.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.TherapistId);
        builder.HasIndex(p => p.FullName);
        builder.HasIndex(p => p.ContactInfo);
    }
}