using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Extensions;

public static class IdentityResultExtensions
{
    public static ValidationProblemDetails ToValidationProblemDetails(this IdentityResult identityResult,
        string instance)
    {
        ValidationProblemDetails problemDetails = new()
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = instance
        };

        foreach (IdentityError error in identityResult.Errors)
        {
            string key = error.Code;

            if (!problemDetails.Errors.TryGetValue(key, out string[]? value))
            {
                problemDetails.Errors[key] = [error.Description];
            }
            else
            {
                List<string> existingErrors = value.ToList();
                existingErrors.Add(error.Description);
                problemDetails.Errors[key] = existingErrors.ToArray();
            }
        }

        return problemDetails;
    }
}
