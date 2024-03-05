using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.Models
{
    public class ShoppingCart
    {
        // This is used for when the users chick on details 
        public int Id { get; set; }

        // Wewant to display the product in the shopping cart
        // So we have to create a foreignkey for it
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        [ValidateNever] // We don't want any validation
        public Product Product { get; set; }

        // We also want to know who is buying the product/car
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        [ValidateNever] // We don't want any validation 
        public ApplicationUser User { get; set; }


        public double Price { get; set; }
    }
}
