using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureTechnologyE_Commerce.Models
{
    public class ReturnRequest
    {
        [Key]
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public OrderHeader Order { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }
        
        public DateTime RequestDate { get; set; }
        
        public string Status { get; set; } = "Pending";
        
        public DateTime? ProcessedDate { get; set; }
        
        public string? AdminResponse { get; set; }
        
        public bool IsRefundIssued { get; set; }
        
        public DateTime? RefundDate { get; set; }
    }
} 