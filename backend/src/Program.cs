using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LogopedicBackend;
using LogopedicBackend.Data;
using LogopedicBackend.Exceptions;
using LogopedicBackend.Extensions;
using LogopedicBackend.Filters;
using LogopedicBackend.Models;
using LogopedicBackend.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication()
    .AddCookie(IdentityConstants.ApplicationScheme,
        options =>
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

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 8;
    })
    .AddSignInManager()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LogopedicContext>();

if (!builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.Services.AddDbContext<LogopedicContext>(options => options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSeeding(DatabaseSeeder.Seed)
        .UseAsyncSeeding(DatabaseSeeder.SeedAsync));
}

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

if (builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.Services
        // Ignore antiforgery for integration tests
        .AddControllersWithViews(options =>
        {
            options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
            options.Filters.Add<TrimModelStringsFilter>();
        })
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
}
else
{
    builder.Services
        .AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.Filters.Add<TrimModelStringsFilter>();
        })
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

    builder.Services.AddRateLimiter(options =>
    {
        int permitLimit = 10;

        if (builder.Environment.IsDevelopment())
        {
            permitLimit = 999;
        }

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = permitLimit,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.OnRejected = async (httpContext, ct) =>
        {
            httpContext.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            httpContext.HttpContext.Response.Headers.RetryAfter = "60";
            // httpContext.HttpContext.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "You have exceeded the allowed 10 requests per minute limit. Try again later.",
                Type = "https://httpstatuses.com/429",
                Instance = httpContext.HttpContext.Request.Path
            };

            await httpContext.HttpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        };
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev",
        policy =>
        {
            policy.WithOrigins("https://localhost")
                .WithOrigins("https://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


builder.Services.AddScoped<AdminService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITherapistService, TherapistService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPatientService, PatientService>();

DirectoryInfo keysFolder = new("/keys");
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(keysFolder)
    .SetApplicationName("Logopedic");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();

    options.AddSecurityDefinition("X-XSRF-TOKEN",
        new OpenApiSecurityScheme
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-XSRF-TOKEN" },
                Scheme = "X-XSRF-TOKEN",
                Name = "X-XSRF-TOKEN",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(5000,
        listenOptions =>
        {
            listenOptions.UseHttps(
                Environment.GetEnvironmentVariable("PFX_PATH") ??
                throw new InvalidOperationException("PFX file not found"),
                Environment.GetEnvironmentVariable("PFX_PASS"));
        });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
    app.UseRateLimiter();
}
else if (app.Environment.IsEnvironment("IntegrationTests"))
{
    app.ApplyMigrations();
}

await app.SeedRolesAsync();
await app.SeedAdminAsync();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("LocalDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
