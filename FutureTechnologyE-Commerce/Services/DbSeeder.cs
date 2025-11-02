using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Services
{
    public class DbSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DbSeeder> _logger;

        public DbSeeder(IServiceProvider serviceProvider, ILogger<DbSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(System.Threading.CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                try
                {
                    // Apply pending migrations
                    await context.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Database migrations applied successfully");

                    // Seed roles
                    await SeedRolesAsync(roleManager);

                    // Seed admin user
                    await SeedAdminUserAsync(userManager, roleManager);

                    _logger.LogInformation("Database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while seeding the database");
                    throw;
                }
            }
        }

        public Task StopAsync(System.Threading.CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[]
            {
                SD.Role_Admin,
                SD.Role_Employee,
                SD.Role_Cust,
                SD.Role_Comp
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Role '{role}' created successfully");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Check if admin user already exists
            var adminEmail = "admin@Email.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                // Create new admin user
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    first_name = "Admin",
                    last_name = "User",
                    PhoneNumber = "01234567890",
                    PhoneNumberConfirmed = true,
                    street = "Admin Street",
                    building = "Building 1",
                    state = "Cairo",
                    country = "EG",
                    apartment = "A101",
                    floor = "1"
                };

                // Default admin password - should be changed after first login
                const string adminPassword = "Admin@123";

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Admin user '{adminEmail}' created successfully");

                    // Assign admin role
                    if (await roleManager.RoleExistsAsync(SD.Role_Admin))
                    {
                        await userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
                        _logger.LogInformation($"Admin role assigned to user '{adminEmail}'");
                    }
                    else
                    {
                        _logger.LogError($"Admin role '{SD.Role_Admin}' does not exist");
                    }
                }
                else
                {
                    _logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                _logger.LogInformation($"Admin user '{adminEmail}' already exists");
            }
        }
    }

    // Extension method to register the seeder
    public static class DbSeederExtensions
    {
        public static IHostBuilder UseDbSeeder(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddHostedService<DbSeeder>();
            });

            return hostBuilder;
        }
    }
}
