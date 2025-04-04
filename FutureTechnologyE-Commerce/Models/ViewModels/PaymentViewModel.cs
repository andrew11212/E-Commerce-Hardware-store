using System;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; }
        public double Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? PaymentReference { get; set; }
        public string? StatusMessage { get; set; }
        public bool IsSuccess { get; set; }
        public PaymentCustomerInfo CustomerInfo { get; set; } = new PaymentCustomerInfo();
    }

    public class PaymentCustomerInfo
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        public string Street { get; set; } = string.Empty;
        
        [Required]
        public string Building { get; set; } = string.Empty;
        
        public string? Floor { get; set; }
        
        [Required]
        public string City { get; set; } = string.Empty;
        
        [Required]
        public string State { get; set; } = string.Empty;
        
        public string Country { get; set; } = "EG";
    }
} 