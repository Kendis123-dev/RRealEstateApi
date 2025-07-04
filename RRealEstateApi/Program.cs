﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RRealEstateApi.Data;
using RRealEstateApi.Models;
using RRealEstateApi.Repositories;
using RRealEstateApi.Repositories.Implementations;
using RRealEstateApi.Services;
using RRealEstateApi.Services.Implementations;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    // 1. Add Database Context
    var constring = "Server=(localdb)\\MSSQllocalDB;Database=RealEstateDbs;Trusted_Connection=True;";
    builder.Services.AddDbContext<RealEstateDbContext>(options =>
        options.UseSqlServer(constring));

    // 2. Configure Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<RealEstateDbContext>()
        .AddDefaultTokenProviders();

    // 3. Configure JWT Authentication
    var jwtSettings = configuration.GetSection("Jwt");
    var key = Encoding.ASCII.GetBytes(jwtSettings["key"]);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.IncludeErrorDetails = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            TokenDecryptionKey = new SymmetricSecurityKey(key)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT Auth failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT validated. Claims:");
                foreach (var claim in context.Principal.Claims)
                {
                    Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
                }
                return Task.CompletedTask;
            }
        };
    });

    // 4. Configure Authorization
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
        options.AddPolicy("Agent", policy => policy.RequireRole("Agent"));
        options.AddPolicy("User", policy => policy.RequireRole("User"));
    });

    // 5. Register Services and Repositories
    builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
    builder.Services.AddScoped<IPropertyService, PropertyService>();
    builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    builder.Services.AddHttpClient<IPhoneService, HttpSmsService>();


    // 6. Add Controllers, Swagger, and Logging
    builder.Services.AddControllers();
    builder.Logging.AddConsole();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer' followed by your token (e.g., 'Bearer eyJhbGciOi...')"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // 7. Load appsettings.json
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    var app = builder.Build();

    // 8. Middleware
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // 9. Seed Roles
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<RealEstateDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();

        string[] roles = { "Admin", "Agent", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($" Program.cs failed: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($" Inner Exception: {ex.InnerException.Message}");

    throw;
}