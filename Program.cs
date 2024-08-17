using Microsoft.EntityFrameworkCore;
using Stripe;
using CarSales.Models;
using CarSales.DataAccess.Data;
using CarSales.DataAccess.Repository;
using CarSales.DataAccess.Repository.IRepository;
using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Identity;
using CarSales.Utility;
using Google.Apis.Auth.OAuth2;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
// Add Stripe configuration
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Add session for admin user management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add Razor pages support
builder.Services.AddRazorPages();

// Configure DbContext
async Task ConfigureDbContext(IServiceProvider services)
{
    var secretService = services.GetRequiredService<SecretManagerServiceClient>();
    string projectId = "ksortreeservice-414322"; // Replace with your actual project ID
    string secretId = "AIVEN"; // The name of your secret

    try
    {
        // Build the secret version name
        var secretVersionName = new SecretVersionName(projectId, secretId, "latest");

        // Retrieve the secret value
        var secretResponse = await secretService.AccessSecretVersionAsync(new AccessSecretVersionRequest
        {
            SecretVersionName = secretVersionName
        });

        // Get the secret value as a string
        string connectionString = secretResponse.Payload.Data.ToStringUtf8();

        // Use the secret value to configure the database connection
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
    }
    catch (Exception ex)
    {
        // Handle exceptions when retrieving secrets
        Console.WriteLine($"Error retrieving secret: {ex.Message}");
    }
}

// First configure DbContext asynchronously
await ConfigureDbContext(builder.Services.BuildServiceProvider());

// Identity and User Management
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cookies and path
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Admin/User/Login";
    options.LogoutPath = $"/Admin/User/Logout";
    options.AccessDeniedPath = $"/Admin/User/AccessDenied";
});

// Stripe configuration
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Map Razor pages and default route
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();
