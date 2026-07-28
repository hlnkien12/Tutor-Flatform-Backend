using TutorPlatform.API;
using TutorPlatform.Application;
using TutorPlatform.Infrastructure;
using TutorPlatform.API.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

// Add Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed default Admin account on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TutorPlatform.Infrastructure.Persistence.ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<TutorPlatform.Application.Common.Interfaces.IPasswordHasher>();

    // Ensure database is created and all migrations are applied
    await dbContext.Database.MigrateAsync();

    // Check if Admin account already exists
    var adminExists = await dbContext.Users.AnyAsync(u => u.Role == 0);
    if (!adminExists)
    {
        var adminUser = new TutorPlatform.Infrastructure.Models.UserDataModel
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Email = "admin@tutorplatform.com",
            PasswordHash = passwordHasher.HashPassword("Admin@123"),
            FullName = "System Admin",
            Role = 0, // Admin
            IsActive = true,
            CreditBalance = 0,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        Console.WriteLine("==> Default Admin account seeded: admin@tutorplatform.com / Admin@123");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use Exception Handler
app.UseExceptionHandler();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TutorPlatform.API.Hubs.NotificationHub>("/hubs/notifications");

app.Run();

