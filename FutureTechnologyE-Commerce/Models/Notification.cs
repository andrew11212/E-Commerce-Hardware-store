using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureTechnologyE_Commerce.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? ReadDate { get; set; }
        
        [Required]
        public bool IsRead { get; set; } = false;
        
        // Link notification to a user
        [Required]
        public string UserId { get; set; }
        
        // Optional link to order
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public OrderHeader Order { get; set; }
        
        // Type of notification (order, promotion, system, etc.)
        [Required]
        public string Type { get; set; }
        
        // URL for action button (optional)
        public string ActionUrl { get; set; }
        
        // Icon class for the notification (e.g., Bootstrap icon)
        public string IconClass { get; set; } = "bi-bell";
        
        // Priority level (low, medium, high)
        public string Priority { get; set; } = "medium";
        
        // Expiration date (optional)
        public DateTime? ExpiresOn { get; set; }
    }
} 