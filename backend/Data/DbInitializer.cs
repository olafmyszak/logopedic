using LogopedicBackend.Models;
using LogopedicBackend.Models.Enums;

namespace LogopedicBackend.Data;

public class DbInitializer
{
    public static void Seed(LogopedicContext context)
    {
        if (context.Patients.Any())
        {
            return;
        }

        var patients = new List<Patient>
        {
            new()
            {
                FullName = "Jan Kowalski", DateOfBirth = new DateTime(2015, 1, 12, 0 ,0, 0, DateTimeKind.Utc), ContactInfo = "jankowal@o2.pl",
                Notes = "jshfjsfjksjf"
            },
            new()
            {
                FullName = "Artur Nowak", DateOfBirth = new DateTime(2020, 6, 7, 0, 0,0, DateTimeKind.Utc), ContactInfo = "arkow@gmail.com",
                Notes = "lorem ipsum"
            }
        };

        context.Patients.AddRange(patients);
        context.SaveChanges();

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                PatientId = patients[0].Id, StartTime = DateTime.UtcNow.AddDays(1).AddHours(15),
                DurationInMinutes = 90,
                Type = AppointmentType.Consultation, Status = AppointmentStatus.Scheduled
            },
            new Appointment
            {
                PatientId = patients[1].Id, StartTime = DateTime.UtcNow.AddDays(3).AddHours(8), DurationInMinutes = 60,
                Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Cancelled
            },
        };

        context.Appointments.AddRange(appointments);
        context.SaveChanges();
    }
}