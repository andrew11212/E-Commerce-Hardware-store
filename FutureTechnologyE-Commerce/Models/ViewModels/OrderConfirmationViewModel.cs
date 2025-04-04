using System;
using System.Collections.Generic;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class OrderConfirmationViewModel
	{
		public int OrderId { get; set; }
		public DateTime OrderDate { get; set; }
		public double OrderTotal { get; set; }
		public string? PaymentMethod { get; set; }
		public string? PaymentStatus { get; set; }
		public string? OrderStatus { get; set; }
		public DateTime EstimatedDeliveryDate { get; set; }
		public ShippingAddressViewModel ShippingAddress { get; set; } = default!;
		public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
	}

	public class ShippingAddressViewModel
	{
		public string FullName { get; set; } = string.Empty;
		public string Street { get; set; } = string.Empty;
		public string Building { get; set; } = string.Empty;
		public string Floor { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string State { get; set; } = string.Empty;
		public string PostalCode { get; set; } = string.Empty;
	}

	public class OrderItemViewModel
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; } = string.Empty;
		public int Quantity { get; set; }
		public double Price { get; set; }
		public double Total { get; set; }
		public string ImageUrl { get; set; } = string.Empty;
	}
}
