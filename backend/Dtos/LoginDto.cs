using System.ComponentModel.DataAnnotations;

namespace LogopedicBackend.Dtos;

public class LoginDto
{
    [EmailAddress]
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required bool IsPersistent { get; set; }
}