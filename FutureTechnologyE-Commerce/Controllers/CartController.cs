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
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize]
	[EnableRateLimiting("fixed")]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly Paymob _paymob;
		private readonly ILogger<CartController> _logger;
		private readonly IAntiforgery _antiforgery;

		[BindProperty]
		public CartViewModel CartVM { get; set; } = new CartViewModel();

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

		public async Task<IActionResult> Index()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				CartVM.CartList = (await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product")).ToList();
				CartVM.OrderHeader = new OrderHeader();

				CartVM.OrderHeader.OrderTotal = CartVM.CartList.Sum(cart => cart.price * cart.Count);
				return View(CartVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Index");
				TempData["Error"] = "Failed to load cart. Please try again.";
				return View(new CartViewModel());
			}
		}

		#region Cart Operations

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddToCart(int productId, int count = 1)
		{
			var userId = GetValidatedUserId();
			if (userId == null) return Unauthorized();

			var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
			if (product == null || product.StockQuantity < count)
			{
				TempData["Error"] = "Product is unavailable or insufficient stock";
				return RedirectToAction("Index", "Home");
			}

			var cartItem = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

			if (cartItem != null)
			{
				cartItem.Count += count;
				if (product.StockQuantity < cartItem.Count)
				{
					TempData["Error"] = "Requested quantity exceeds available stock";
					return RedirectToAction("Index");
				}
				await _unitOfWork.CartRepositery.UpdateAsync(cartItem);
			}
			else
			{
				await _unitOfWork.CartRepositery.AddAsync(new ShopingCart
				{
					ApplicationUserId = userId,
					ProductId = productId,
					Count = count
				});
			}

			await _unitOfWork.SaveAsync();
			TempData["Success"] = "Item added to cart successfully";
			return RedirectToAction("Index");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateCart(int cartId, int count)
		{
			var userId = GetValidatedUserId();
			if (userId == null) return Unauthorized();

			var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId);
			if (cart == null)
			{
				TempData["Error"] = "Cart item not found";
				return RedirectToAction("Index");
			}

			var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == cart.ProductId);
			if (product == null || product.StockQuantity < count)
			{
				TempData["Error"] = "Requested quantity exceeds available stock";
				return RedirectToAction("Index");
			}

			cart.Count = count;
			await _unitOfWork.CartRepositery.UpdateAsync(cart);
			await _unitOfWork.SaveAsync();

			TempData["Success"] = "Cart updated successfully";
			return RedirectToAction("Index");
		}

		#endregion

		public async Task<IActionResult> Checkout()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				CartVM.CartList = (await _unitOfWork.CartRepositery.GetAllAsync(u => u.ApplicationUserId == userId, "Product")).ToList();
				CartVM.OrderHeader = new OrderHeader();

				var user = await _unitOfWork.applciationUserRepository.GetAsync(u => u.Id == userId);
				if (user == null)
				{
					_logger.LogWarning("User not found in Cart/Summary for userId: {UserId}", userId);
					return NotFound("User not found");
				}

				CartVM.OrderHeader.ApplicationUser = user;
				CartVM.OrderHeader.first_name = SanitizeInput(user.first_name);
				CartVM.OrderHeader.last_name = SanitizeInput(user.last_name);
				CartVM.OrderHeader.street = SanitizeInput(user.street);
				CartVM.OrderHeader.building = SanitizeInput(user.building);
				CartVM.OrderHeader.phone_number = SanitizePhoneNumber(user.PhoneNumber);
				CartVM.OrderHeader.email = SanitizeInput(user.Email);
				CartVM.OrderHeader.state = SanitizeInput(user.state);
				CartVM.OrderHeader.floor = SanitizeInput(user.floor);

				CartVM.OrderHeader.OrderTotal = CartVM.CartList.Sum(cart => cart.price * cart.Count);
				return View(CartVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Summary");
				TempData["Error"] = "Failed to load order summary. Please try again.";
				return RedirectToAction("Index");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[ActionName("Checkout")]
		public async Task<IActionResult> CheckoutPost()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				var cartItems = (await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product")).ToList();

				if (!cartItems.Any())
				{
					TempData["Error"] = "Your cart is empty";
					return RedirectToAction("Index");
				}

				using (IDbContextTransaction transaction = _unitOfWork.BeginTransaction())
				{
					foreach (var cart in cartItems)
					{
						var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == cart.ProductId);
						if (product == null || product.StockQuantity < cart.Count)
						{
							TempData["Error"] = $"Item {cart.Product?.Name ?? "unknown"} is out of stock";
							return RedirectToAction("Index");
						}
						product.StockQuantity -= cart.Count;
						await _unitOfWork.ProductRepository.UpdateAsync(product); // Assuming UbdateAsync exists or should be UpdateAsync
					}

					var user = await _unitOfWork.applciationUserRepository.GetAsync(u => u.Id == userId);
					if (user == null) return NotFound("User not found");

					CartVM.OrderHeader.ApplicationUserId = userId;
					SetOrderAndPaymentStatus(CartVM.OrderHeader);
					PopulateOrderHeaderFromUser(CartVM.OrderHeader, user);

					await _unitOfWork.OrderHeader.AddAsync(CartVM.OrderHeader);
					await _unitOfWork.SaveAsync(); // Save the OrderHeader to get the Id for OrderDetails

					foreach (var cart in cartItems)
					{
						OrderDetail orderDetail = new()
						{
							OrderId = CartVM.OrderHeader.Id,
							ProductId = cart.ProductId,
							Price = cart.price,
							Count = cart.Count
						};
						await _unitOfWork.OrderDetail.AddAsync(orderDetail);
					}
					await _unitOfWork.CartRepositery.RemoveRangeAsync(cartItems);
					await _unitOfWork.SaveAsync();

					transaction.Commit();
				}

				var currentUser = await _unitOfWork.applciationUserRepository.GetAsync(u => u.Id == userId);
				if (currentUser == null) return NotFound("User not found");

				return await ProcessPaymentAsync(currentUser);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Order processing failed for userId: {UserId}", GetValidatedUserId());
				TempData["Error"] = "Order processing failed. Please try again.";
				return RedirectToAction("Checkout");
			}
		}

		[HttpPost]
		public async Task<IActionResult> PaymentCallback()
		{
			try
			{
				var transactionId = Request.Form["transaction_id"];
				var order = Request.Form["order"];
				var success = Request.Form["success"] == "true";
				var hmac = Request.Form["hmac"];

				if (string.IsNullOrEmpty(order) || !int.TryParse(order, out int paymobOrderId))
				{
					_logger.LogWarning("Invalid order ID in payment callback: Order={Order}", order);
					return BadRequest("Invalid order ID");
				}

				if (!ValidateCallback(order, paymobOrderId, transactionId, hmac))
				{
					_logger.LogWarning("Invalid payment callback HMAC: OrderId={OrderId}, TransactionId={TransactionId}", order, transactionId);
					return BadRequest("Invalid callback signature");
				}

				var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.PaymobOrderId == paymobOrderId);
				if (orderHeader == null)
				{
					_logger.LogWarning("Order not found for PaymobOrderId={PaymobOrderId} in PaymentCallback", paymobOrderId);
					return NotFound("Order not found");
				}

				UpdateOrderStatus(orderHeader, success, transactionId);
				await _unitOfWork.SaveAsync();

				return RedirectToAction("OrderConfirmation", new { orderId = orderHeader.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing payment callback");
				return StatusCode(500, "Callback processing failed");
			}
		}

		#region Helper Methods
		private string? GetValidatedUserId()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return string.IsNullOrEmpty(userId) ? null : userId;
		}

		private string SanitizeInput(string input) =>
			string.IsNullOrWhiteSpace(input) ? "NA" : Regex.Replace(input.Trim(), @"[<>&'""\\/]", "").Substring(0, Math.Min(255, input.Trim().Length));

		private string SanitizePhoneNumber(string phoneNumber) =>
			string.IsNullOrWhiteSpace(phoneNumber) ? "+201234567890" :
			Regex.Replace(phoneNumber.Trim(), @"[^0-9+]", "").StartsWith("+") ?
			phoneNumber.Substring(0, Math.Min(20, phoneNumber.Length)) :
			("+20" + phoneNumber).Substring(0, Math.Min(20, phoneNumber.Length + 3));

		private void SetOrderAndPaymentStatus(OrderHeader orderHeader)
		{
			orderHeader.PaymentStatus = SD.Payment_Status_Pending;
			orderHeader.OrderStatus = SD.Status_Pending;
			orderHeader.OrderDate = DateTime.UtcNow;
		}

		private void PopulateOrderHeaderFromUser(OrderHeader orderHeader, ApplicationUser user)
		{
			orderHeader.first_name = SanitizeInput(user.first_name);
			orderHeader.last_name = SanitizeInput(user.last_name);
			orderHeader.street = SanitizeInput(user.street);
			orderHeader.building = SanitizeInput(user.building);
			orderHeader.phone_number = SanitizePhoneNumber(user.PhoneNumber);
			orderHeader.email = SanitizeInput(user.Email);
			orderHeader.state = SanitizeInput(user.state);
			orderHeader.floor = SanitizeInput(user.floor);
		}

		private async Task<IActionResult> ProcessPaymentAsync(ApplicationUser user)
		{
			try
			{
				_logger.LogInformation("Starting payment process for userId: {UserId}", user.Id);
				string authToken = await GetAuthTokenAsync();
				int paymobOrderId = await CreateOrderAsync(authToken, CartVM.OrderHeader.OrderTotal);
				string paymentKey = await GetPaymentKeyAsync(authToken, paymobOrderId, CartVM.OrderHeader.OrderTotal, user);

				CartVM.OrderHeader.PaymobOrderId = paymobOrderId;
				await _unitOfWork.OrderHeader.UpdateAsync(CartVM.OrderHeader);
				await _unitOfWork.SaveAsync();

				string paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKey}";
				_logger.LogInformation("Redirecting to Paymob payment URL for orderId: {OrderId}", CartVM.OrderHeader.Id);
				return Redirect(paymentUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Payment processing failed for orderId: {OrderId}", CartVM.OrderHeader?.Id);
				TempData["Error"] = "Payment processing failed. Please try again.";
				return RedirectToAction("Checkout");
			}
		}

		private async Task<string> GetAuthTokenAsync()
		{
			using var client = new RestClient(_paymob.BaseUrl + "auth/tokens");
			var request = new RestRequest()
				.AddHeader("Content-Type", "application/json")
				.AddJsonBody(new { api_key = _paymob.ApiKey });
			var response = await client.PostAsync<PaymobAuthResponse>(request);
			if (response?.Token == null) throw new Exception("Failed to authenticate with Paymob");
			_logger.LogInformation("Successfully retrieved Paymob auth token");
			return response.Token;
		}

		private async Task<int> CreateOrderAsync(string authToken, double amount)
		{
			using var client = new RestClient(_paymob.BaseUrl + "ecommerce/orders");
			var request = new RestRequest()
				.AddHeader("Authorization", $"Bearer {authToken}")
				.AddHeader("Content-Type", "application/json")
				.AddJsonBody(new
				{
					amount_cents = (int)(amount * 100),
					currency = "EGP",
					merchant_id = _paymob.MerchantId,
					delivery_needed = false,
					items = CartVM.CartList.Select(c => new { name = c.Product.Name, amount_cents = (int)(c.price * 100), quantity = c.Count }).ToArray()
				});
			var response = await client.PostAsync<PaymobOrderResponse>(request);
			if (response?.Id == null) throw new Exception("Failed to create Paymob order");
			_logger.LogInformation("Paymob order created with ID: {PaymobOrderId}", response.Id);
			return response.Id;
		}

		private async Task<string> GetPaymentKeyAsync(string authToken, int orderId, double amount, ApplicationUser user)
		{
			using var client = new RestClient(_paymob.BaseUrl + "acceptance/payment_keys");
			var billingData = new
			{
				email = SanitizeInput(user.Email ?? "unknown@example.com"),
				first_name = SanitizeInput(user.first_name ?? "Unknown"),
				last_name = SanitizeInput(user.last_name ?? "Unknown"),
				phone_number = SanitizePhoneNumber(user.PhoneNumber),
				street = SanitizeInput(user.street ?? "NA"),
				building = SanitizeInput(user.building ?? "NA"),
				city = SanitizeInput(user.state ?? "NA"),
				country = "EG",
				apartment = SanitizeInput(user.floor ?? "NA"),
				floor = SanitizeInput(user.floor ?? "NA"),
				state = SanitizeInput(user.state ?? "NA")
			};

			var request = new RestRequest()
				.AddHeader("Authorization", $"Bearer {authToken}")
				.AddHeader("Content-Type", "application/json")
				.AddJsonBody(new
				{
					amount_cents = (int)(amount * 100),
					currency = "EGP",
					order_id = orderId,
					billing_data = billingData,
					integration_id = _paymob.IntegrationId,
					lock_order_when_paid = "false"
				});
			var response = await client.PostAsync<PaymobPaymentKeyResponse>(request);
			if (response?.Token == null) throw new Exception("Failed to get payment key");
			_logger.LogInformation("Payment key retrieved for orderId: {OrderId}", orderId);
			return response.Token;
		}

		private bool ValidateCallback(string orderId, int? paymobOrderId, string transactionId, string receivedHmac)
		{
			if (paymobOrderId == null || orderId != paymobOrderId.ToString() || string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(receivedHmac))
				return false;

			var concatenatedData = string.Join("", Request.Form.OrderBy(k => k.Key).Select(k => k.Value.ToString()));
			var calculatedHmac = HMACSha512(concatenatedData, _paymob.HmacSecret);
			bool isValid = receivedHmac.Equals(calculatedHmac, StringComparison.OrdinalIgnoreCase);

			if (!isValid)
				_logger.LogWarning("HMAC validation failed. Received: {ReceivedHmac}, Calculated: {CalculatedHmac}", receivedHmac, calculatedHmac);

			return isValid;
		}

		private string HMACSha512(string data, string key)
		{
			using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
			{
				byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
				return BitConverter.ToString(hash).Replace("-", "").ToLower();
			}
		}

		private void UpdateOrderStatus(OrderHeader orderHeader, bool success, string transactionId)
		{
			orderHeader.PaymentStatus = success ? SD.Payment_Status_Approved : SD.Payment_Status_Rejected;
			orderHeader.OrderStatus = success ? SD.Status_Approved : SD.Status_Cancelled;
			orderHeader.TransactionId = transactionId;
			orderHeader.PaymentDate = DateTime.UtcNow;
			// Assuming OrderHeader also has an async Update method
			_unitOfWork.OrderHeader.UpdateAsync(orderHeader);
			_logger.LogInformation("Order status updated to {OrderStatus} for orderId: {OrderId}", orderHeader.OrderStatus, orderHeader.Id);
		}

		public async Task<IActionResult> Remove(int cartId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId);
				if (cart == null)
				{
					TempData["Error"] = "Cart item not found";
					return RedirectToAction("Index");
				}

				await _unitOfWork.CartRepositery.RemoveAsync(cart);
				await _unitOfWork.SaveAsync();
				TempData["Success"] = "Product removed successfully.";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Remove for cartId: {CartId}", cartId);
				TempData["Error"] = "Failed to remove item from cart. Please try again.";
				return RedirectToAction("Index");
			}
		}

		public async Task<IActionResult> OrderConfirmation(int orderId)
		{
			try
			{
				var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == orderId);
				if (orderHeader == null)
				{
					_logger.LogWarning("Order with id {OrderId} not found in Cart/OrderConfirmation", orderId);
					TempData["Error"] = "Order not found.";
					return RedirectToAction("Index");
				}
				_logger.LogInformation("Order confirmation displayed for orderId: {OrderId}", orderId);
				return View(orderId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/OrderConfirmation for orderId: {OrderId}", orderId);
				TempData["Error"] = "Failed to load order confirmation.";
				return RedirectToAction("Index");
			}
		}
		#endregion
	}
}