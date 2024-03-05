using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        public string Make { get; set; }
        [Required]
        public string Model { get; set; }
        [Required]
        public double Miles { get; set; }
        [Required]
        public double Price { get; set; }

        // Connect to Category so that they can select the type of category
        // That aligns with the product
        [DisplayName("Body Type")]
        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        [ValidateNever] // We don't want any validation for this
        public Category Category { get; set; }

        // For Image
        [ValidateNever] // We don't want any validation for image
        public List<Image> Images { get; set; }
    }
}
