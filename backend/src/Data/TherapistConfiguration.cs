using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogopedicBackend.Data;

public class TherapistConfiguration : IEntityTypeConfiguration<Therapist>
{
    public void Configure(EntityTypeBuilder<Therapist> builder)
    {
        builder.ToTable("Therapists", "public");
        // .HasMany(t => t.Patients)
        // .WithOne(p => p.Therapist)
        // .HasForeignKey(p => p.TherapistId)
        // .OnDelete(DeleteBehavior.Cascade);

        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.User)
            .WithOne() // no navigation property on IdentityUser
            .HasForeignKey<Therapist>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Patients)
            .WithOne(p => p.Therapist)
            .HasForeignKey(p => p.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Appointments)
            .WithOne(a => a.Therapist)
            .HasForeignKey(a => a.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.UserId)
            .IsUnique();
    }
}