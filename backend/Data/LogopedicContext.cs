using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Data;

public class LogopedicContext(DbContextOptions<LogopedicContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<Therapist> Therapists { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<Therapist>()
            .ToTable("Therapists", "public")
            .HasMany(t => t.Patients)
            .WithOne(p => p.Therapist)
            .HasForeignKey(p => p.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Patient>()
            .ToTable("Patients", "public")
            .HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .ToTable("Appointments", "public")
            .HasOne(a => a.Therapist)
            .WithMany(t => t.Appointments)
            .HasForeignKey(a => a.TherapistId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}