using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Data;

public class LogopedicContext(DbContextOptions<LogopedicContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId);
    }
}