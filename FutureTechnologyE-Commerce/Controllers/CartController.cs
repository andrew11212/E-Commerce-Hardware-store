using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize]
	[EnableRateLimiting("fixed")] // Apply rate limiting policy (configure in Program.cs)
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly Paymob _paymob;
		private readonly ILogger<CartController> _logger;
		private readonly IAntiforgery _antiforgery;

		[BindProperty]
		public CartViewModel CartVM { get; set; } = default!;

		public CartController(
			IUnitOfWork unitOfWork,
			IOptions<Paymob> paymob,
			ILogger<CartController> logger,
			IAntiforgery antiforgery)
		{
			_unitOfWork = unitOfWork;
			_paymob = paymob.Value;
			_logger = logger;
			_antiforgery = antiforgery;
		}

		public IActionResult Index()
		{
			var userId = GetValidatedUserId();
			if (userId == null) return Unauthorized();

			CartVM = new()
			{
				CartList = _unitOfWork.CartRepositery.GetAll(c => c.ApplicationUserId == userId, "Product"),
				OrderHeader = new()
			};
			foreach (var cart in CartVM.CartList)
			{
				CartVM.OrderHeader.OrderTotal += cart.price * cart.Count;
			}
			return View(CartVM);
		}

		public IActionResult Summary()
		{
			var userId = GetValidatedUserId();
			if (userId == null) return Unauthorized();

			CartVM = new()
			{
				CartList = _unitOfWork.CartRepositery.GetAll(u => u.ApplicationUserId == userId, "Product"),
				OrderHeader = new()
			};
			var user = _unitOfWork.applciationUserRepository.Get(u => u.Id == userId);
			if (user == null) return NotFound("User not found");

			CartVM.OrderHeader.ApplicationUser = user;
			CartVM.OrderHeader.Name = SanitizeInput(user.Name);
			CartVM.OrderHeader.PhoneNumber = SanitizePhoneNumber(user.PhoneNumber);
			CartVM.OrderHeader.Address = SanitizeInput(user.StreetAddress);
			CartVM.OrderHeader.City = SanitizeInput(user.City);
			CartVM.OrderHeader.State = SanitizeInput(user.State);
			CartVM.OrderHeader.PostalCode = SanitizeInput(user.PostalCode);

			foreach (var cart in CartVM.CartList)
			{
				CartVM.OrderHeader.OrderTotal += cart.price * cart.Count;
			}
			return View(CartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken] // Add CSRF protection
		public async Task<IActionResult> SummaryPOST()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				CartVM.CartList = _unitOfWork.CartRepositery.GetAll(u => u.ApplicationUserId == userId, "Product");
				if (!CartVM.CartList.Any())
				{
					_logger.LogWarning("Cart is empty for user {UserId}", userId);
					TempData["Error"] = "Your cart is empty.";
					return RedirectToAction("Index");
				}

				CartVM.OrderHeader.OrderDate = DateTime.UtcNow; // Use UTC for consistency
				CartVM.OrderHeader.ApplicationUserId = userId;

				var applicationUser = _unitOfWork.applciationUserRepository.Get(u => u.Id == userId);
				if (applicationUser == null) return NotFound("User not found");

				foreach (var cart in CartVM.CartList)
				{
					CartVM.OrderHeader.OrderTotal += cart.price * cart.Count;
				}

				if (!ValidateOrderTotal(CartVM.OrderHeader.OrderTotal))
				{
					_logger.LogWarning("Invalid order total for user {UserId}", userId);
					TempData["Error"] = "Invalid order total.";
					return RedirectToAction("Summary");
				}

				SetOrderAndPaymentStatus(applicationUser);

				_unitOfWork.OrderHeader.Add(CartVM.OrderHeader);
				_unitOfWork.Save();

				AddOrderDetails(CartVM.OrderHeader.Id);

				
					return await ProcessPaymentAsync(applicationUser);

				return RedirectToAction("OrderConfirmation", new { orderId = CartVM.OrderHeader.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing order for user {UserId}", GetValidatedUserId());
				TempData["Error"] = "An error occurred while processing your order. Please try again later.";
				return RedirectToAction("Summary");
			}
		}

		[HttpPost]
		public IActionResult PaymentCallback()
		{
			try
			{
				var transactionId = Request.Form["transaction_id"];
				var orderId = Request.Form["order"];
				var success = Request.Form["success"] == "true";
				var paymobOrderId = HttpContext.Session.GetInt32("PaymobOrderId");

				if (!ValidateCallback(orderId, paymobOrderId, transactionId))
				{
					_logger.LogWarning("Invalid payment callback data: OrderId={OrderId}, PaymobOrderId={PaymobOrderId}", orderId, paymobOrderId);
					return BadRequest("Invalid callback data");
				}

				var orderHeader = _unitOfWork.OrderHeader.Get(o => o.PaymobOrderId == int.Parse(orderId));
				if (orderHeader == null)
				{
					_logger.LogWarning("Order not found for PaymobOrderId={PaymobOrderId}", orderId);
					return NotFound("Order not found");
				}

				UpdateOrderStatus(orderHeader, success, transactionId);
				_unitOfWork.Save();
				HttpContext.Session.Remove("PaymobOrderId");

				return RedirectToAction("OrderConfirmation", new { orderId = orderHeader.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing payment callback");
				return StatusCode(500, "Callback processing failed");
			}
		}

		// Helper Methods
		private string? GetValidatedUserId()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out _) ? null : userId;
		}

		private string SanitizeInput(string input)
		{
			return string.IsNullOrWhiteSpace(input) ? "NA" : Regex.Replace(input.Trim(), @"[<>&'""\\/]", "");
		}

		private string SanitizePhoneNumber(string phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(phoneNumber)) return "+201234567890";
			var sanitized = Regex.Replace(phoneNumber.Trim(), @"[^0-9+]", "");
			return sanitized.StartsWith("+") ? sanitized : $"+20{sanitized}";
		}

		private bool ValidateOrderTotal(double total)
		{
			return total > 0 && total < 1_000_000; // Arbitrary upper limit for sanity check
		}

		private void SetOrderAndPaymentStatus(ApplicationUser user)
		{
			
				CartVM.OrderHeader.PaymentStatus = SD.Payment_Status_Pending;
				CartVM.OrderHeader.OrderStatus = SD.Status_Pending;
			
		}

		private void AddOrderDetails(int orderId)
		{
			foreach (var cart in CartVM.CartList)
			{
				var orderDetail = new OrderDetail
				{
					ProductId = cart.ProductId,
					OrderId = orderId,
					Price = cart.price,
					Count = cart.Count
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
			}
			_unitOfWork.Save();
		}

		private async Task<IActionResult> ProcessPaymentAsync(ApplicationUser user)
		{
			string authToken = await GetAuthTokenAsync();
			int paymobOrderId = await CreateOrderAsync(authToken, CartVM.OrderHeader.OrderTotal);
			string paymentKey = await GetPaymentKeyAsync(authToken, paymobOrderId, CartVM.OrderHeader.OrderTotal, user);

			CartVM.OrderHeader.PaymobOrderId = paymobOrderId; // Store Paymob order ID
			_unitOfWork.OrderHeader.Update(CartVM.OrderHeader);
			_unitOfWork.Save();

			HttpContext.Session.SetInt32("PaymobOrderId", paymobOrderId);
			string paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKey}";

			if (!Uri.TryCreate(paymentUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
			{
				throw new Exception("Invalid payment URL");
			}

			return Redirect(uri.ToString());
		}

		private async Task<string> GetAuthTokenAsync()
		{
			var options = new RestClientOptions(_paymob.BaseUrl + "auth/tokens")
			{
				ThrowOnAnyError = true
			};
			using var client = new RestClient(options);
			var request = new RestRequest()
				.AddHeader("Content-Type", "application/json")
				.AddJsonBody(new { api_key = _paymob.ApiKey });

			var response = await client.PostAsync<PaymobAuthResponse>(request);
			return response?.Token ?? throw new Exception("Failed to authenticate with Paymob");
		}

		private async Task<int> CreateOrderAsync(string authToken, double amount)
		{
			var options = new RestClientOptions(_paymob.BaseUrl + "ecommerce/orders")
			{
				ThrowOnAnyError = true
			};
			using var client = new RestClient(options);
			var request = new RestRequest()
				.AddHeader("Authorization", $"Bearer {authToken}")
				.AddHeader("Content-Type", "application/json")
				.AddJsonBody(new
				{
					amount_cents = (int)(amount * 100), // Ensure accurate conversion
					currency = "EGP",
					merchant_id = _paymob.MerchantId
				});

			var response = await client.PostAsync<PaymobOrderResponse>(request);
			return response?.Id ?? throw new Exception("Failed to create Paymob order");
		}

		private async Task<string> GetPaymentKeyAsync(string authToken, int orderId, double amount, ApplicationUser user)
		{
			var options = new RestClientOptions(_paymob.BaseUrl + "acceptance/payment_keys")
			{
				ThrowOnAnyError = true
			};
			using var client = new RestClient(options);
			var request = new RestRequest()
				.AddHeader("Authorization", $"Bearer {authToken}")
				.AddHeader("Content-Type", "application/json")
				.AddJsonBody(new
				{
					amount_cents = (int)(amount * 100),
					currency = "EGP",
					order_id = orderId,
					billing_data = new
					{
						email = user.Email ?? "unknown@example.com",
						first_name = SanitizeInput(user.Name.Split(' ').FirstOrDefault() ?? "Unknown"),
						last_name = SanitizeInput(user.Name.Contains(" ") ? user.Name.Split(' ')[1] : "Unknown"),
						phone_number = SanitizePhoneNumber(user.PhoneNumber),
						street = SanitizeInput(user.StreetAddress ?? "NA"),
						building = "NA",
						city = SanitizeInput(user.City ?? "NA"),
						state = SanitizeInput(user.State ?? "NA"),
						postal_code = SanitizeInput(user.PostalCode ?? "NA"),
						country = "EG",
						apartment = "NA",
						floor = "NA"
					},
					integration_id = _paymob.IntegrationId
				});

			var response = await client.PostAsync<PaymobPaymentKeyResponse>(request);
			return response?.Token ?? throw new Exception("Failed to get payment key");
		}

		private bool ValidateCallback(string orderId, int? paymobOrderId, string transactionId)
		{
			// Add HMAC verification here based on Paymob's documentation
			return paymobOrderId.HasValue && orderId == paymobOrderId.ToString() && !string.IsNullOrEmpty(transactionId);
		}

		private void UpdateOrderStatus(OrderHeader orderHeader, bool success, string transactionId)
		{
			if (success)
			{
				orderHeader.PaymentStatus = SD.Payment_Status_Approved;
				orderHeader.OrderStatus = SD.Status_Approved;
				orderHeader.TransactionId = transactionId;
			}
			else
			{
				orderHeader.PaymentStatus = SD.Payment_Status_Rejected;
				orderHeader.OrderStatus = SD.Status_Cancelled;
			}
			_unitOfWork.OrderHeader.Update(orderHeader);
		}


		public IActionResult Plus(int cartId)
		{
			var cart = _unitOfWork.CartRepositery.Get(c => c.Id == cartId);
			if (cart != null)
			{
				cart.Count++;
				_unitOfWork.CartRepositery.Update(cart);
				_unitOfWork.Save();
			}
			return RedirectToAction("Index");
		}

		public IActionResult Minus(int cartId)
		{
			var cart = _unitOfWork.CartRepositery.Get(c => c.Id == cartId);
			if (cart != null)
			{
				if (cart.Count > 1)
				{
					cart.Count--;
					_unitOfWork.CartRepositery.Update(cart);
				}
				else
				{
					_unitOfWork.CartRepositery.Remove(cart);
				}
				_unitOfWork.Save();
			}
			return RedirectToAction("Index");
		}

		public IActionResult Remove(int cartId)
		{
			var cart = _unitOfWork.CartRepositery.Get(c => c.Id == cartId);
			if (cart != null)
			{
				_unitOfWork.CartRepositery.Remove(cart);
				_unitOfWork.Save();
			}
			return RedirectToAction("Index");
		}
		public IActionResult OrderConfirmation()
		{
			var orderId = (int?)TempData["OrderId"];
			if (orderId == null)
			{
				// Handle case where OrderId is not available (e.g., redirect to error)
				return RedirectToAction("Index");
			}
			return View(orderId);
		}

	}
}
