using CarSales.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CarSales.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Company> Companys { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Image> Images { get; set; }

        // Cart and Payment stuff
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<OrderHeader> OrderHeader { get; set; }

        // Create items into database in code instead of manually addding in website
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Create default categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Sedan", DisplayOrder = 1 },
                new Category { Id = 2, Name = "SUV", DisplayOrder = 2 },
                new Category { Id = 3, Name = "HATCHBACK", DisplayOrder = 3 },
                new Category { Id = 4, Name = "MINIVAN", DisplayOrder = 4 },
                new Category { Id = 6, Name = "TRUCK", DisplayOrder = 6 }
                );

            // Create Default Companies
            modelBuilder.Entity<Company>().HasData(
                new Company { 
                    Id = 1,
                    Name = "Best Buy", 
                    StreetAddress = "342 Best Street", 
                    City = "Charlotte",
                    State = "NC",
                    PostalCode = "28293",
                    PhoneNumber = "854485652"
                },
                new Company
                {
                    Id = 2,
                    Name = "Amazon",
                    StreetAddress = "489 Amazon Drive",
                    City = "Raleigh",
                    State = "NC",
                    PostalCode = "45852",
                    PhoneNumber = "4895234854"
                },
                new Company
                {
                    Id = 3,
                    Name = "Ebay",
                    StreetAddress = "652 MyStreet CT",
                    City = "Washington",
                    State = "DC",
                    PostalCode = "89425",
                    PhoneNumber = "8489564234"
                }
                );

            // Create Default Products
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Year = 2022,
                    Model = "Civic Sport",
                    Make = "Honda",
                    Miles = 27287,
                    Price = 29955,
                    CategoryId = 1
                },
                new Product
                {
                    Id = 2,
                    Year = 2021,
                    Model = "Accord 1.5T Sport SE",
                    Make = "Honda",
                    Miles = 23469,
                    Price = 28200,
                    CategoryId = 1
                },
                new Product
                {
                    Id = 3,
                    Year = 2023,
                    Model = "Odyssey Sport",
                    Make = "Honda",
                    Miles = 16226,
                    Price = 40809,
                    CategoryId = 4
                },
                new Product
                {
                    Id = 4,
                    Year = 2019,
                    Model = "Odyssey EX",
                    Make = "Honda",
                    Miles = 43889,
                    Price = 31995,
                    CategoryId = 4
                },
                new Product
                {
                    Id = 5,
                    Year = 2023,
                    Model = "Ridgeline AWD RTL",
                    Make = "Honda",
                    Miles = 4509,
                    Price = 41981,
                    CategoryId = 6
                },
                new Product
                {
                    Id = 6,
                    Year = 2022,
                    Model = "Ridgeline AWD RTL-E",
                    Make = "Honda",
                    Miles = 21766,
                    Price = 38300,
                    CategoryId = 6
                }, 
                new Product
                {
                    Id = 7,
                    Year = 2022,
                    Model = "HR-V 2WD LX",
                    Make = "Honda",
                    Miles = 14357,
                    Price = 25143,
                    CategoryId = 2
                },
                new Product
                {
                    Id = 8,
                    Year = 2022,
                    Model = "HR-V 2WD EX",
                    Make = "Honda",
                    Miles = 36919,
                    Price = 25740,
                    CategoryId = 2
                }
                );
        }
    }
}
