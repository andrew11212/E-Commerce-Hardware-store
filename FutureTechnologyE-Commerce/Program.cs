using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Threading.RateLimiting;
using System.Globalization;
using FutureTechnologyE_Commerce.Services;
using StackExchange.Redis;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace FutureTechnologyE_Commerce
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			// Configure Serilog
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
				.Enrich.FromLogContext()
				.Enrich.WithEnvironmentName()
				.CreateLogger();

			try
			{
				Log.Information("Starting web application");
				var builder = WebApplication.CreateBuilder(args);

				// Add Serilog to the application
				builder.Host.UseSerilog();

				// Configure logging
				builder.Logging.ClearProviders();
				builder.Logging.AddConsole();
				builder.Logging.AddDebug();
				builder.Logging.SetMinimumLevel(LogLevel.Information);

				// Add Response Compression
				builder.Services.AddResponseCompression(options =>
				{
					options.EnableForHttps = true;
					options.Providers.Add<BrotliCompressionProvider>();
					options.Providers.Add<GzipCompressionProvider>();
					options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
					{
						"application/json",
						"application/javascript",
						"text/css",
						"text/html",
						"text/plain",
						"image/svg+xml"
					});
				});

				builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
				{
					options.Level = CompressionLevel.Fastest;
				});

				builder.Services.Configure<GzipCompressionProviderOptions>(options =>
				{
					options.Level = CompressionLevel.Optimal;
				});

				builder.Services.AddControllersWithViews();
				builder.Services.AddDbContext<ApplicationDbContext>(options => options
					.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
					.EnableSensitiveDataLogging(false)
					.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
				builder.Services.Configure<Paymob>(builder.Configuration.GetSection("PayMob"));
				builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
				{
					//options.SignIn.RequireConfirmedAccount = true; // Enforce account confirmation
				}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

				// Configure cookie policy for secure sessions
				builder.Services.ConfigureApplicationCookie(options =>
				{
					options.LoginPath = "/Identity/Account/Login";
					options.LogoutPath = "/Identity/Account/Logout";
					options.AccessDeniedPath = "/Identity/Account/AccessDenied";
					options.Cookie.HttpOnly = true;
					options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
					options.Cookie.SameSite = SameSiteMode.Strict;
					options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
					options.SlidingExpiration = true;
				});

				builder.Services.AddRazorPages();
				builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
				builder.Services.AddScoped<IEmailSender, EmailSender>();
				builder.Services.AddScoped<ISMSSender, SMSSender>();
				builder.Services.AddScoped<INotificationService, NotificationService>();
				builder.Services.AddScoped<PaymentHealthMonitor>();
				builder.Services.AddScoped<FutureTechnologyE_Commerce.Services.PaymentService>();
				
				// Configure Redis Cache
				var redisConnection = builder.Configuration.GetConnectionString("Redis");
				if (!string.IsNullOrEmpty(redisConnection))
				{
					builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
					{
						var configuration = ConfigurationOptions.Parse(redisConnection, true);
						configuration.AbortOnConnectFail = false;
						configuration.ConnectTimeout = 5000;
						configuration.SyncTimeout = 5000;
						return ConnectionMultiplexer.Connect(configuration);
					});
					
					builder.Services.AddStackExchangeRedisCache(options =>
					{
						options.Configuration = redisConnection;
						options.InstanceName = "FutureTech_";
					});
					
					builder.Services.AddScoped<ICacheService, RedisCacheService>();
				}
				else
				{
					// Fallback to in-memory cache if Redis is not configured
					builder.Services.AddDistributedMemoryCache();
					builder.Services.AddScoped<ICacheService, RedisCacheService>();
				}
				
				builder.Services.AddSession(options =>
				{
					options.IdleTimeout = TimeSpan.FromMinutes(30);
					options.Cookie.HttpOnly = true;
					options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
					options.Cookie.SameSite = SameSiteMode.Strict;
					options.Cookie.IsEssential = true;
				});

				// Register HttpClient services
				builder.Services.AddHttpClient();
				builder.Services.AddHttpClient("PaymobClient", client => {
					client.BaseAddress = new Uri("https://accept.paymob.com/api/");
					client.DefaultRequestHeaders.Add("Accept", "application/json");
				});

				// Configure rate limiting
				builder.Services.AddRateLimiter(options =>
				{
					options.AddFixedWindowLimiter("fixed", limiterOptions =>
					{
						limiterOptions.PermitLimit = 100;
						limiterOptions.Window = TimeSpan.FromMinutes(1);
						limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
						limiterOptions.QueueLimit = 0;
					});
					options.OnRejected = (context, token) =>
					{
						context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
						return ValueTask.CompletedTask;
					};
				});

				builder.Services.AddAuthentication()
	.AddGoogle(googleOptions =>
	{
		googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
		googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
	})
	// Add Facebook Authentication
	.AddFacebook(facebookOptions =>
	{
		facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
		facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
		facebookOptions.AccessDeniedPath = "/Account/AccessDenied"; // Optional: Redirect path on access denied
	});

				var app = builder.Build();
				var supportedCultures = new[] { "en-US", "ar-EG" };
				
				// Configure custom culture for EGP currency
				var customCulture = new System.Globalization.CultureInfo("en-US");
				customCulture.NumberFormat.CurrencySymbol = "EGP";
				customCulture.NumberFormat.CurrencyPositivePattern = 2; // Format as "EGP 123.45"
				
				var localizationOptions = new RequestLocalizationOptions()
					.SetDefaultCulture("en-US")  // Back to en-US with custom provider
					.AddSupportedCultures(supportedCultures)
					.AddSupportedUICultures(supportedCultures);

				// Add custom culture provider for EGP currency
				localizationOptions.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CustomRequestCultureProvider(context =>
				{
					// Apply custom culture with EGP currency symbol
					System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
					return Task.FromResult(new ProviderCultureResult("en-US"));
				}));

				app.UseRequestLocalization(localizationOptions);
				
				// Enable response compression
				app.UseResponseCompression();
				
				// Configure the HTTP request pipeline.
				if (!app.Environment.IsDevelopment())
				{
					app.UseExceptionHandler("/Home/Error");
					app.UseHsts();
				}

				//app.UseHttpsRedirection();
				
				// Configure static files with caching
				app.UseStaticFiles(new StaticFileOptions
				{
					OnPrepareResponse = ctx =>
					{
						const int durationInSeconds = 60 * 60 * 24 * 365; // 1 year
						ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
						ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
					}
				});
				app.UseRouting();
				app.UseAuthentication();
				app.UseAuthorization();
				app.UseSession();
				app.UseRateLimiter(); // Enable rate limiting
				app.MapRazorPages();
				app.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");

				// Seed database with admin account
				await SeedDatabaseAsync(app.Services);

				app.Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Application terminated unexpectedly");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static async Task SeedDatabaseAsync(IServiceProvider services)
		{
			using (var scope = services.CreateScope())
			{
				var scopedServices = scope.ServiceProvider;
				var context = scopedServices.GetRequiredService<ApplicationDbContext>();
				var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
				var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();

				try
				{
					// Apply pending migrations
					await context.Database.MigrateAsync();
					Log.Information("Database migrations applied successfully");

					// Seed roles
					await SeedRolesAsync(roleManager);

					// Seed admin user
					await SeedAdminUserAsync(userManager, roleManager);

					Log.Information("Database seeding completed successfully");
				}
				catch (Exception ex)
				{
					Log.Error(ex, "An error occurred while seeding the database");
				}
			}
		}

		private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
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
						Log.Information($"Role '{role}' created successfully");
					}
					else
					{
						Log.Error($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
					}
				}
			}
		}

		private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			// Check if admin user already exists
			var adminEmail = "admin@futuretech.com";
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
				const string adminPassword = "Admin@123456";

				var result = await userManager.CreateAsync(adminUser, adminPassword);

				if (result.Succeeded)
				{
					Log.Information($"Admin user '{adminEmail}' created successfully");

					// Assign admin role
					if (await roleManager.RoleExistsAsync(SD.Role_Admin))
					{
						await userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
						Log.Information($"Admin role assigned to user '{adminEmail}'");
					}
					else
					{
						Log.Error($"Admin role '{SD.Role_Admin}' does not exist");
					}
				}
				else
				{
					Log.Error($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
				}
			}
			else
			{
				Log.Information($"Admin user '{adminEmail}' already exists");
			}
		}
	}
}
