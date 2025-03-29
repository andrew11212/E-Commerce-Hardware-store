using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize] //  Apply authorization at the controller level
	[EnableRateLimiting("fixed")] // Apply rate limiting policy (configure in Program.cs)
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly Paymob _paymob;
		private readonly ILogger<CartController> _logger;
		private readonly IAntiforgery _antiforgery;

		[BindProperty]
		public CartViewModel CartVM { get; set; } = default!; //  Initialize to default!

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
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null)
				{
					_logger.LogWarning("Unauthorized access to Cart/Index");
					return Unauthorized();
				}

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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Index");
				TempData["Error"] = "Failed to load cart. Please try again.";
				return View(new CartViewModel()); // Return empty model, avoid null
			}
		}

		public IActionResult Summary()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null)
				{
					_logger.LogWarning("Unauthorized access to Cart/Summary");
					return Unauthorized();
				}

				CartVM = new()
				{
					CartList = _unitOfWork.CartRepositery.GetAll(u => u.ApplicationUserId == userId, "Product"),
					OrderHeader = new()
				};
				var user = _unitOfWork.applciationUserRepository.Get(u => u.Id == userId);
				if (user == null)
				{
					_logger.LogWarning("User not found in Cart/Summary for userId: {UserId}", userId);
					return NotFound("User not found");
				}

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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Summary");
				TempData["Error"] = "Failed to load order summary. Please try again.";
				return RedirectToAction("Index"); // Redirect to a safe page
			}
		}

		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SummaryPOST()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null)
				{
					_logger.LogWarning("Unauthorized access to Cart/SummaryPOST");
					return Unauthorized();
				}

				CartVM.CartList = _unitOfWork.CartRepositery.GetAll(u => u.ApplicationUserId == userId, "Product");
				if (!CartVM.CartList.Any())
				{
					_logger.LogWarning("Cart is empty for user {UserId} in Cart/SummaryPOST", userId);
					TempData["Error"] = "Your cart is empty.";
					return RedirectToAction("Index");
				}

				CartVM.OrderHeader.OrderDate = DateTime.UtcNow;
				CartVM.OrderHeader.ApplicationUserId = userId;

				var applicationUser = _unitOfWork.applciationUserRepository.Get(u => u.Id == userId);
				if (applicationUser == null)
				{
					_logger.LogError("User not found in Cart/SummaryPOST for user {UserId}", userId);
					return NotFound("User not found");
				}

				foreach (var cart in CartVM.CartList)
				{
					CartVM.OrderHeader.OrderTotal += cart.price * cart.Count;
				}

				if (!ValidateOrderTotal(CartVM.OrderHeader.OrderTotal))
				{
					_logger.LogWarning("Invalid order total for user {UserId} in Cart/SummaryPOST: {OrderTotal}", userId, CartVM.OrderHeader.OrderTotal);
					TempData["Error"] = "Invalid order total.";
					return RedirectToAction("Summary");
				}

				SetOrderAndPaymentStatus(applicationUser);

				_unitOfWork.OrderHeader.Add(CartVM.OrderHeader);
				_unitOfWork.Save(); // Save here to get the OrderHeader.Id

				AddOrderDetails(CartVM.OrderHeader.Id);

				//  Consider wrapping the payment processing in a transaction
				//  to ensure atomicity.  If payment fails, you might want to
				//  rollback the order.  This adds complexity, so weigh
				//  the trade-offs.

				return await ProcessPaymentAsync(applicationUser); // Await the async method
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing order for user {UserId} in Cart/SummaryPOST", GetValidatedUserId());
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
				var order = Request.Form["order"]; // Paymob order ID is in the "order" parameter
				var success = Request.Form["success"] == "true";
				//var paymobOrderId = HttpContext.Session.GetInt32("PaymobOrderId"); // No longer rely on session

				if (string.IsNullOrEmpty(order) || !int.TryParse(order, out int paymobOrderId))
				{
					_logger.LogWarning("Invalid order ID in payment callback: Order={Order}", order);
					return BadRequest("Invalid order ID");
				}
				if (!ValidateCallback(order, paymobOrderId, transactionId))
				{
					_logger.LogWarning("Invalid payment callback data: OrderId={OrderId}, TransactionId={TransactionId}", order, transactionId);
					return BadRequest("Invalid callback data");
				}

				var orderHeader = _unitOfWork.OrderHeader.Get(o => o.PaymobOrderId == paymobOrderId); // Use PaymobOrderId
				if (orderHeader == null)
				{
					_logger.LogWarning("Order not found for PaymobOrderId={PaymobOrderId} in PaymentCallback", paymobOrderId);
					return NotFound("Order not found");
				}

				UpdateOrderStatus(orderHeader, success, transactionId);
				_unitOfWork.Save();
				//HttpContext.Session.Remove("PaymobOrderId"); // Remove session data

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
			if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out _))
			{
				_logger.LogWarning("Invalid user ID.");
				return null;
			}
			return userId;
		}

		private string SanitizeInput(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return "NA";
			}
			string sanitized = Regex.Replace(input.Trim(), @"[<>&'""\\/]", "");
			return sanitized.Length > 255 ? sanitized.Substring(0, 255) : sanitized; // Limit length
		}

		private string SanitizePhoneNumber(string phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(phoneNumber))
			{
				return "+201234567890";
			}
			string sanitized = Regex.Replace(phoneNumber.Trim(), @"[^0-9+]", "");
			if (!sanitized.StartsWith("+"))
			{
				sanitized = "+20" + sanitized;
			}
			return sanitized.Length > 20 ? sanitized.Substring(0, 20) : sanitized;  // Limit length
		}

		private bool ValidateOrderTotal(double total)
		{
			return total > 0 && total < 1_000_000;
		}

		private void SetOrderAndPaymentStatus(ApplicationUser user)
		{
			CartVM.OrderHeader.PaymentStatus = SD.Payment_Status_Pending;
			CartVM.OrderHeader.OrderStatus = SD.Status_Pending;
		}

		private void AddOrderDetails(int orderId)
		{
			try
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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding order details for order {OrderId}", orderId);
				throw; // Re-throw the exception to be caught in SummaryPOST
			}
		}

		private async Task<IActionResult> ProcessPaymentAsync(ApplicationUser user)
		{
			try
			{
				string authToken = await GetAuthTokenAsync();
				int paymobOrderId = await CreateOrderAsync(authToken, CartVM.OrderHeader.OrderTotal);
				string paymentKey = await GetPaymentKeyAsync(authToken, paymobOrderId, CartVM.OrderHeader.OrderTotal, user);

				CartVM.OrderHeader.PaymobOrderId = paymobOrderId; // Store Paymob order ID
				_unitOfWork.OrderHeader.Update(CartVM.OrderHeader);
				_unitOfWork.Save();

				//HttpContext.Session.SetInt32("PaymobOrderId", paymobOrderId);  //remove session
				string paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKey}";

				if (!Uri.TryCreate(paymentUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
				{
					_logger.LogError("Invalid payment URL: {PaymentUrl}", paymentUrl);
					throw new Exception("Invalid payment URL");
				}

				return Redirect(uri.ToString()); // Use the safe URI
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Payment processing failed");
				throw; // Re-throw to be handled in SummaryPOST
			}
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
			if (response == null || string.IsNullOrEmpty(response.Token))
			{
				_logger.LogError($"Failed to authenticate with Paymob.  Response: {Response.StatusCode}" );
				throw new Exception("Failed to authenticate with Paymob");
			}
			return response.Token;
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
					amount_cents = (int)(amount * 100),
					currency = "EGP",
					merchant_id = _paymob.MerchantId
				});

			var response = await client.PostAsync<PaymobOrderResponse>(request);
			if (response == null)
			{
				_logger.LogError($"Failed to create Paymob order. Response: {Response.StatusCode}");
				throw new Exception("Failed to create Paymob order");
			}
			return response.Id;
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

			if (response == null || string.IsNullOrEmpty(response.Token))
			{
				_logger.LogError($"Failed to get payment key. Response: {Response.StatusCode}");
				throw new Exception("Failed to get payment key");
			}
			return response.Token;
		}

		private bool ValidateCallback(string orderId, int? paymobOrderId, string transactionId)
		{
			//  IMPORTANT:  Add HMAC validation here.  This is critical
			//  for security.  Paymob provides a way to validate the
			//  integrity of the callback data using a secure hash.
			//  You MUST implement this to prevent malicious users from
			//  tampering with the callback.
			//  See Paymob's documentation for details on how to do this.
			//
			//  The following is INSECURE and MUST be replaced:
			if (paymobOrderId == null || orderId != paymobOrderId.ToString() || string.IsNullOrEmpty(transactionId))
			{
				return false;
			}
			return true;
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
			try
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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Plus for cartId: {CartId}", cartId);
				TempData["Error"] = "Failed to update cart. Please try again.";
				return RedirectToAction("Index");
			}
		}

		public IActionResult Minus(int cartId)
		{
			try
			{
				var cart = _unitOfWork.CartRepositery.Get(c => c.Id == cartId);
				if (cart != null)
				{
					if (cart.Count > 1)
					{
						cart.Count--;
						_unitOfWork.CartRepositery.Update(cart);
						_unitOfWork.Save();
					}
					else
					{
						_unitOfWork.CartRepositery.Remove(cart);
						_unitOfWork.Save();
					}
				}
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Minus for cartId: {CartId}", cartId);
				TempData["Error"] = "Failed to update cart. Please try again.";
				return RedirectToAction("Index");
			}
		}

		public IActionResult Remove(int cartId)
		{
			try
			{
				var cart = _unitOfWork.CartRepositery.Get(c => c.Id == cartId);
				if (cart != null)
				{
					_unitOfWork.CartRepositery.Remove(cart);
					_unitOfWork.Save();
				}
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Remove for cartId: {CartId}", cartId);
				TempData["Error"] = "Failed to remove item from cart. Please try again.";
				return RedirectToAction("Index");
			}
		}

		public IActionResult OrderConfirmation()
		{
			var orderId = (int?)TempData["OrderId"];
			if (orderId == null)
			{
				_logger.LogWarning("OrderId is null in Cart/OrderConfirmation");
				TempData["Error"] = "Order confirmation details are missing.";
				return RedirectToAction("Index");
			}
			try
			{
				var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId);
				if (orderHeader == null)
				{
					_logger.LogWarning("Order with id {OrderId} not found in Cart/OrderConfirmation", orderId);
					TempData["Error"] = "Order not found.";
					return RedirectToAction("Index");
				}
				return View(orderId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/OrderConfirmation for orderId: {OrderId}", orderId);
				TempData["Error"] = "Failed to load order confirmation.";
				return RedirectToAction("Index");
			}
		}
	}
}

