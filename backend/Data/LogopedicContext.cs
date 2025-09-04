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

        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<Therapist>(entityBuilder =>
        {
            entityBuilder.ToTable("Therapists", "public")
                .HasMany(t => t.Patients)
                .WithOne(p => p.Therapist)
                .HasForeignKey(p => p.TherapistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Patient>(entityBuilder =>
        {
            entityBuilder.ToTable("Patients", "public")
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entityBuilder.Property(p => p.SearchText)
                .HasComputedColumnSql(
                    "lower(unaccent(coalesce(FullName,'') || ' ' || coalesce(ContactInfo,''))",
                    stored: true);

            entityBuilder.HasIndex(p => p.FullName);
            entityBuilder.HasIndex(p => p.ContactInfo);
        });

        modelBuilder.Entity<Appointment>(entityBuilder =>
        {
            entityBuilder.ToTable("Appointments", "public")
                .HasOne(a => a.Therapist)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TherapistId)
                .OnDelete(DeleteBehavior.Cascade);

            entityBuilder.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entityBuilder.HasIndex(a => a.StartTime);
            entityBuilder.HasIndex(a => a.DurationInMinutes);
            entityBuilder.HasIndex(a => a.Type);
            entityBuilder.HasIndex(a => a.Status);
        });
    }
}