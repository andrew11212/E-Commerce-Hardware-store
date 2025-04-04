using System.ComponentModel.DataAnnotations.Schema;

namespace FutureTechnologyE_Commerce.Models
{
	public class OrderHeader
	{
		public int Id { get; set; }

		public string ApplicationUserId { get; set; } = string.Empty;
		[ForeignKey(nameof(ApplicationUserId))]
		public ApplicationUser ApplicationUser { get; set; } = default!;

		public DateTime OrderDate { get; set; }

		public DateTime ShippingDate { get; set; }

		public double OrderTotal { get; set; }

		public string? OrderStatus { get; set; }
		public string? PaymentStatus { get; set; }
		public string? PaymentMethod { get; set; }
		public int? PaymobOrderId { get; set; }

		public string? TransactionId { get; set; }
		public DateTime? PaymentDate { get; set; }
		public DateOnly? PaymentDueDate { get; set; }

		public string first_name { get; set; } = string.Empty;
		public string last_name { get; set; } = string.Empty;
		public string? apartment { get; set; } = string.Empty;
		public string? street { get; set; } = string.Empty;
		public string? building { get; set; } = string.Empty;
		public string? phone_number { get; set; } = string.Empty;
		public string? country { get; set; } = string.Empty;
		public string? email { get; set; } = string.Empty;

		public string? floor { get; set; } = string.Empty;
		public string? state { get; set; } = string.Empty;

	}
}