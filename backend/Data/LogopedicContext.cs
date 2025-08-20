using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Data;

public class LogopedicContext(DbContextOptions<LogopedicContext> options) : DbContext(options)
{
    public DbSet<Therapist> Therapists { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Therapist>()
            .HasIndex(t => t.Email)
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Therapist)
            .WithMany(t => t.Appointments)
            .HasForeignKey(a => a.TherapistId);

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.Therapist)
            .WithMany(t => t.Patients)
            .HasForeignKey(p => p.TherapistId);
    }
}