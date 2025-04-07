namespace FutureTechnologyE_Commerce.Utility
{
	public static class SD
	{
		public const string Role_Cust = "Customer";
		public const string Role_Admin = "Admin";
		public const string Role_Comp = "Company";
		public const string Role_Employee = "Employee";
		public const string Status_Approved = "Approved";
		public const string Status_Pending = "Pending";
		public const string Status_Processing = "Processing";
		public const string Status_Shipped = "Shipped";
		public const string Status_Delivered = "Delivered";
		public const string Status_Cancelled = "Cancelled";

		public const string Payment_Status_Approved = "Approved";
		public const string Payment_Status_Pending = "Pending";
		public const string Payment_Status_Delayed_Payment = "ApprovedForDelayedPayment";
		public const string Payment_Status_Rejected = "Rejected";
		public const string Payment_Method_COD = "CashOnDelivery";
		public const string Payment_Method_Online = "OnlinePayment";
		
		// Notification Types
		public const string Notification_Type_Order = "Order";
		public const string Notification_Type_Promotion = "Promotion";
		public const string Notification_Type_System = "System";
		public const string Notification_Type_Payment = "Payment";
		public const string Notification_Type_Shipping = "Shipping";
		
		// Notification Priorities
		public const string Notification_Priority_Low = "low";
		public const string Notification_Priority_Medium = "medium";
		public const string Notification_Priority_High = "high";
		
		// Notification Icons
		public const string Notification_Icon_Order = "bi-box-seam";
		public const string Notification_Icon_Promotion = "bi-tag";
		public const string Notification_Icon_System = "bi-gear";
		public const string Notification_Icon_Payment = "bi-credit-card";
		public const string Notification_Icon_Shipping = "bi-truck";
	}
}
