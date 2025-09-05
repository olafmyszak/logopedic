using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogopedicBackend.Data;

public class TherapistConfiguration : IEntityTypeConfiguration<Therapist>
{
    public void Configure(EntityTypeBuilder<Therapist> builder)
    {
        builder.ToTable("Therapists", "public")
            .HasMany(t => t.Patients)
            .WithOne(p => p.Therapist)
            .HasForeignKey(p => p.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}