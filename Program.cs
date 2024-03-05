using Microsoft.EntityFrameworkCore;
using CarSales.Utility;
using Stripe;
using CarSales.Data.DbInitializer;
using CarSales.Models;
using Microsoft.AspNetCore.Identity;
using CarSales.DataAccess.DbInitializer;
using CarSales.DataAccess.Data;
using CarSales.DataAccess.Repository.IRepository;
using CarSales.DataAccess.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// Add stripe
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Identity and User Management
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = $"/Admin/User/Login";
    options.LogoutPath = $"/Admin/User/Logout";
    options.AccessDeniedPath = $"/Admin/User/AccessDenied";
});

// Connect to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnections")));

// Add session so that you can edit user info for admins #1
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Whenever an I<Something> is required, use <Something>
// So make sure to add this
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// For DbInitializer
// builder.Services.AddScoped<IDbInitializer, DbInitializer>();

// Use Razer pages for login and register and allow for razor pages #1
builder.Services.AddRazorPages();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Allows for use of razor pages #2
app.MapRazorPages();

// Stripe Payment
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

// Allow for adding sessions
app.UseSession();

// For DbInitializer
// SeedDatabase();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}"); // add {area=exists} after adding area scaffold

app.Run();

//// For DbInitializer to work
//void SeedDatabase()
//{
//    using (var scope = app.Services.CreateScope())
//        {
//            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
//            dbInitializer.Initialize();
//        }
//}
