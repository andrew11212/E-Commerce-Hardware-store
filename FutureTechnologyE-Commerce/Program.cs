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

namespace FutureTechnologyE_Commerce
{
	public class Program
	{
		public static void Main(string[] args)
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

				builder.Services.AddControllersWithViews();
				builder.Services.AddDbContext<ApplicationDbContext>(options => options
					.UseSqlServer(builder.Configuration.GetConnectionString("DefualtConnection")));
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
				builder.Services.AddDistributedMemoryCache();
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
				// Configure the HTTP request pipeline.
				if (!app.Environment.IsDevelopment())
				{
					app.UseExceptionHandler("/Home/Error");
					app.UseHsts();
				}

				//app.UseHttpsRedirection();
				app.UseStaticFiles();
				app.UseRouting();
				app.UseAuthentication();
				app.UseAuthorization();
				app.UseSession();
				app.UseRateLimiter(); // Enable rate limiting
				app.MapRazorPages();
				app.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");

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
	}
}
