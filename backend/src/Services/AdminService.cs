using LogopedicBackend.Data;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Services;

public class AdminService(LogopedicContext context, UserManager<User> userManager)
{
    public async Task<bool> DeleteUserAndDomainDataAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return false;
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var therapist = await context.Therapists.FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (therapist is not null)
            {
                context.Therapists.Remove(therapist);
            }

            await context.SaveChangesAsync();

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await transaction.RollbackAsync();
            throw;
        }
    }
}