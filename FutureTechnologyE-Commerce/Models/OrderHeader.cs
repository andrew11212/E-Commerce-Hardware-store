using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureTechnologyE_Commerce.Models
{
	public class OrderHeader
	{
		public int Id { get; set; }

		[Required]
		public string ApplicationUserId { get; set; } = string.Empty;
		[ForeignKey(nameof(ApplicationUserId))]
		public ApplicationUser ApplicationUser { get; set; } = default!;

		public DateTime OrderDate { get; set; }

		public DateTime ShippingDate { get; set; }

		[Range(0.01, double.MaxValue, ErrorMessage = "Order total must be greater than 0")]
		public double OrderTotal { get; set; }

		public string? OrderStatus { get; set; }
		public string? PaymentStatus { get; set; }
		public string? PaymentMethod { get; set; }
		public int? PaymobOrderId { get; set; }

		public string? TransactionId { get; set; }
		public DateTime? PaymentDate { get; set; }
		public DateOnly? PaymentDueDate { get; set; }

		[Required(ErrorMessage = "First name is required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "First name can only contain letters, spaces, and hyphens")]
		public string first_name { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Last name is required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "Last name can only contain letters, spaces, and hyphens")]
		public string last_name { get; set; } = string.Empty;
		
		[StringLength(30, ErrorMessage = "Apartment cannot exceed 30 characters")]
		public string? apartment { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Street address is required")]
		[StringLength(100, MinimumLength = 3, ErrorMessage = "Street must be between 3 and 100 characters")]
		public string? street { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Building number/name is required")]
		[StringLength(30, ErrorMessage = "Building cannot exceed 30 characters")]
		public string? building { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Phone number is required")]
		[Phone(ErrorMessage = "Invalid phone number format")]
		[StringLength(20, MinimumLength = 8, ErrorMessage = "Phone number must be between 8 and 20 characters")]
		public string? phone_number { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Country is required")]
		[StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter code")]
		[RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "Country must be a 2-letter code (e.g., EG)")]
		public string? country { get; set; } = "EG"; // Default to Egypt
		
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email format")]
		[StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
		public string? email { get; set; } = string.Empty;

		[StringLength(10, ErrorMessage = "Floor cannot exceed 10 characters")]
		public string? floor { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "State/City is required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "State/City must be between 2 and 50 characters")]
		public string? state { get; set; } = string.Empty;
	}
}