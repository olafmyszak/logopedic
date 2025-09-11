using System.Text.Json.Serialization;
using LogopedicBackend.Data;
using LogopedicBackend.Exceptions;
using LogopedicBackend.Extensions;
using LogopedicBackend.Models;
using LogopedicBackend.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
}).AddSignInManager().AddRoles<IdentityRole>().AddEntityFrameworkStores<LogopedicContext>();

builder.Services.AddDbContext<LogopedicContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.HeaderName = "X-XSRF-TOKEN";
});

if (builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.Services
        // Ignore antiforgery for integration tests
        .AddControllersWithViews(options => { options.Filters.Add(new IgnoreAntiforgeryTokenAttribute()); })
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
}
else
{
    builder.Services
        .AddControllersWithViews(options => { options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); })
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        });
});

builder.Services.AddScoped<AdminService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITherapistService, TherapistService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPatientService, PatientService>();

var keysFolder = new DirectoryInfo("/keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(keysFolder).SetApplicationName("Logopedic");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();

    options.AddSecurityDefinition("X-XSRF-TOKEN", new OpenApiSecurityScheme
    {
        Description = "Anti-forgery token",
        Name = "X-XSRF-TOKEN",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "X-XSRF-TOKEN"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-XSRF-TOKEN"
                },
                Scheme = "X-XSRF-TOKEN",
                Name = "X-XSRF-TOKEN",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
}
else if (app.Environment.IsEnvironment("IntegrationTests"))
{
    app.ApplyMigrations();
}

await app.SeedRolesAsync();
await app.SeedAdminAsync();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseCors("LocalDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;