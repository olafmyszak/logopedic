using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogopedicBackend.Data;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", "public");

        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Therapist)
            .WithMany(t => t.Appointments)
            .HasForeignKey(a => a.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.TherapistId);
        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.StartTime);
        builder.HasIndex(a => a.DurationInMinutes);
        builder.HasIndex(a => a.Type);
        builder.HasIndex(a => a.Status);
    }
}
