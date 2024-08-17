using Microsoft.EntityFrameworkCore;
using Stripe;
using CarSales.Models;
using CarSales.DataAccess.Data;
using CarSales.DataAccess.Repository;
using CarSales.DataAccess.Repository.IRepository;
using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Identity;
using CarSales.Utility;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// Register the SecretManagerService
builder.Services.AddSingleton<SecretManagerService>();

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

var credential = GoogleCredential.FromFile("ksortreeservice-414322-4a6b6e064aa0");
var client = new SecretManagerServiceClientBuilder
{
    Credential = credential
}.Build();

// Configure DbContext
async Task ConfigureDbContext(IServiceProvider services)
{
    var secretService = services.GetRequiredService<SecretManagerService>();
    string projectId = "ksortreeservice-414322"; // Replace with your actual project ID
    string secretId = "AIVEN"; // The name of your secret

    try
    {
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnections");

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
