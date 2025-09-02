using System.ComponentModel.DataAnnotations;

namespace LogopedicBackend.Dtos;

public class RegisterDto
{
    [EmailAddress] public required string Email { get; set; }

    public required string Password { get; set; }

    public required string FullName { get; set; }
}