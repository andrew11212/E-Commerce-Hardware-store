namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class PaymentResultViewModel
    {
        public bool Success { get; set; }
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = "N/A";
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
    }
} 