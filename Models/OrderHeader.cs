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
    public class OrderHeader
    {
        // This script is used after the user clicks buy
        // This is mainly for admins/employees to add shippingdetails and stuff
        public int Id { get; set; }

        // We want to know which user is buying
        public string ApplicationUserId { get; set; }
        [ForeignKey(nameof(ApplicationUserId))] // Better to use nameof so there won't be any spelling mistakes
        [ValidateNever] // We don't want any validation
        public ApplicationUser ApplicationUser { get; set; }

        // Order
        public DateTime OrderDate { get; set; }
        public DateTime ShippingDate { get; set; }
        public double OrderTotal { get; set; }

        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        // Payment
        public DateTime PaymentDate { get; set; }
        public DateTime PyamentDueDate { get; set; } // For users with company status

        // To Check whether to user has already placed an order or not
        // Also check if there's anything in the cart
        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }

        // User Info
        [Required]
        public string Name { get; set; }
        [Required]
        [DisplayName("Phone Number")]
        public string PhoneNumber { get; set; }
        [Required]
        [DisplayName("Street Address")]
        public string StreetAddress { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        [DisplayName("Postal Code")]
        public string PostalCode { get; set; }
    }
}
