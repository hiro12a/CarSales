using CarSales.DataAccess.Data;
using CarSales.DataAccess.DbInitializer;
using CarSales.Models;
using CarSales.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.Data.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        // We want to be able to get the user and their role
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(UserManager<IdentityUser> userManager, 
            RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }

        public void Initialize()
        {
            // Start migration process if it has not already started
            try
            {
                if(_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex) { }

            // Create roles if they are not created 
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                // Create the roles
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();


                // Since the roles are not created, create a default admin user
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@carsales.com",
                    Email = "admin@carsales.com",
                    Name = "Admin User",
                    PhoneNumber = "1234567890",
                    StreetAddress = "523 Default Admin Address",
                    City = "Charlotte",
                    State = "NC",
                    PostalCode = "28242"

                }, "Adminuser@123").GetAwaiter().GetResult(); // The password

                // This code checks for the user
                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@carsales.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult(); 
            }

            return;
        }
    }
}
