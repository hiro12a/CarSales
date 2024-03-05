using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.Models
{
    public class OrderDetails
    {
        // This script is used for when viewing the product and its details after they click add to cart
        public int Id { get; set; }

        // We want to get details from the orderheader 
        public int OrderHeaderId { get; set; }
        [ForeignKey(nameof(OrderHeaderId))]
        [ValidateNever] // We Don't want any validation
        public OrderHeader OrderHeader { get; set; }

        // We want to get the product from product script
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        public double Price { get; set; }

    }
}
