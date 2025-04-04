using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize]
	[EnableRateLimiting("fixed")]
	public class PaymentController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly Paymob _paymob;
		private readonly ILogger<PaymentController> _logger;
		private readonly FutureTechnologyE_Commerce.Services.PaymentService _paymentService;
		private readonly HttpClient _httpClient;

		public PaymentController(
			IUnitOfWork unitOfWork,
			IOptions<Paymob> paymob,
			ILogger<PaymentController> logger,
			FutureTechnologyE_Commerce.Services.PaymentService paymentService,
			IHttpClientFactory httpClientFactory)
		{
			_unitOfWork = unitOfWork;
			_paymob = paymob.Value;
			_logger = logger;
			_paymentService = paymentService;
			_httpClient = httpClientFactory.CreateClient("PaymobClient");

			// Validate that required Paymob settings are configured
			if (string.IsNullOrEmpty(_paymob.ApiKey))
				throw new ArgumentNullException("Paymob ApiKey is not configured");
			
			if (string.IsNullOrEmpty(_paymob.IntegrationId))
				throw new ArgumentNullException("Paymob IntegrationId is not configured");
			
			if (string.IsNullOrEmpty(_paymob.IframeId))
				throw new ArgumentNullException("Paymob IframeId is not configured");
			
			if (string.IsNullOrEmpty(_paymob.HmacSecret))
				throw new ArgumentNullException("Paymob HmacSecret is not configured");
		}

		[Authorize]
		public async Task<IActionResult> PaymentInit(int orderId)
		{
			// 1. Get order details
			var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == orderId);
			if (orderHeader == null)
			{
				_logger.LogError("Order not found: {OrderId}", orderId);
				return NotFound();
			}

			// 2. Authenticate with Paymob to get auth token
			var authResponse = await GetPaymobAuthToken();
			if (authResponse?.Token == null)
			{
				_logger.LogError("Failed to authenticate with Paymob");
				return RedirectToAction("Error", "Home", new { message = "Payment service unavailable" });
			}

			// 3. Register order with Paymob
			var paymobOrderId = await RegisterOrderWithPaymob(authResponse.Token, orderHeader);
			if (paymobOrderId == null)
			{
				_logger.LogError("Failed to register order with Paymob");
				return RedirectToAction("Error", "Home", new { message = "Payment service unavailable" });
			}

			// 4. Save Paymob order ID to our order
			orderHeader.PaymobOrderId = paymobOrderId.Value;
			await _unitOfWork.SaveAsync();

			// 5. Get payment key
			var paymentKeyToken = await GetPaymentKey(authResponse.Token, paymobOrderId.Value, orderHeader);
			if (paymentKeyToken == null)
			{
				_logger.LogError("Failed to get payment key from Paymob");
				return RedirectToAction("Error", "Home", new { message = "Payment service unavailable" });
			}

			// 6. Redirect to payment iframe
			return Redirect($"https://accept.paymob.com/api/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKeyToken}");
		}

		[HttpGet]
		public async Task<IActionResult> Success(string order_id)
		{
			_logger.LogInformation("Payment success callback received for order: {OrderId}", order_id);
			
			// Parse the Paymob order ID 
			if (!int.TryParse(order_id, out int paymobOrderId))
			{
				_logger.LogError("Invalid order ID in success callback: {OrderId}", order_id);
				return RedirectToAction("Error", "Home", new { message = "Invalid payment information" });
			}

			// Find our order by Paymob order ID
			var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.PaymobOrderId == paymobOrderId);
			if (orderHeader == null)
			{
				_logger.LogError("Order not found for Paymob order ID: {PaymobOrderId}", paymobOrderId);
				return RedirectToAction("Error", "Home", new { message = "Order not found" });
			}

			// Update order status
			orderHeader.PaymentStatus = SD.Payment_Status_Approved;
			orderHeader.OrderStatus = SD.Status_Approved;
			orderHeader.PaymentDate = DateTime.Now;
			await _unitOfWork.SaveAsync();

			_logger.LogInformation("Payment successful for order: {OrderId}", orderHeader.Id);
			return RedirectToAction("OrderConfirmation", "Checkout", new { id = orderHeader.Id });
		}

		[HttpGet]
		public IActionResult Failure()
		{
			_logger.LogWarning("Payment failure callback received");
			return RedirectToAction("Index", "Cart", new { error = "Payment failed. Please try again." });
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Webhook([FromBody] JsonElement payload)
		{
			try
			{
				_logger.LogInformation("Payment webhook received");

				// Extract transaction data
				var type = payload.GetProperty("type").GetString();
				if (type != "transaction.response")
				{
					_logger.LogWarning("Ignoring non-transaction webhook: {Type}", type);
					return Ok();
				}

				// Verify HMAC security if provided in headers
				var hmacHeader = Request.Headers["HMAC"].FirstOrDefault();
				if (!string.IsNullOrEmpty(hmacHeader) && !string.IsNullOrEmpty(_paymob.HmacSecret))
				{
					var payloadJson = payload.GetRawText();
					var calculatedHmac = CalculateHmac(payloadJson, _paymob.HmacSecret);
					
					if (calculatedHmac != hmacHeader)
					{
						_logger.LogWarning("HMAC verification failed for webhook");
						return BadRequest("Invalid HMAC");
					}
				}

				// Extract order data
				var obj = payload.GetProperty("obj");
				var success = obj.GetProperty("success").GetBoolean();
				var orderId = obj.GetProperty("order").GetProperty("id").GetInt32();
				var txnId = obj.GetProperty("id").GetString();

				// Find order by Paymob order ID
				var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.PaymobOrderId == orderId);
				if (orderHeader == null)
				{
					_logger.LogError("Order not found for Paymob order ID: {PaymobOrderId}", orderId);
					return NotFound("Order not found");
				}

				if (success)
				{
					// Update order status for successful transaction
					orderHeader.PaymentStatus = SD.Payment_Status_Approved;
					orderHeader.OrderStatus = SD.Status_Approved;
					orderHeader.PaymentDate = DateTime.Now;
					orderHeader.TransactionId = txnId;
					
					_logger.LogInformation("Payment webhook confirmed successful payment for order: {OrderId}", orderHeader.Id);
				}
				else
				{
					// Mark payment as rejected
					orderHeader.PaymentStatus = SD.Payment_Status_Rejected;
					_logger.LogWarning("Payment webhook received failed payment for order: {OrderId}", orderHeader.Id);
				}

				await _unitOfWork.SaveAsync();
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing payment webhook");
				return StatusCode(500, "Internal error processing webhook");
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Callback(
			[FromQuery] string hmac,
			[FromQuery] string amount_cents,
			[FromQuery] string success,
			[FromQuery] string order_id,
			[FromQuery] string transaction_id)
		{
			try
			{
				_logger.LogInformation("Payment callback received for order: {PaymobOrderId}", order_id);

				// Validate parameters
				if (string.IsNullOrEmpty(order_id) || string.IsNullOrEmpty(success))
				{
					_logger.LogError("Missing required parameters in callback");
					return RedirectToAction("Error", "Home", new { message = "Invalid payment information" });
				}

				// Parse parameters
				var isSuccess = success.ToLower() == "true";
				
				if (!int.TryParse(order_id, out int paymobOrderId))
				{
					_logger.LogError("Invalid order ID in callback: {OrderId}", order_id);
					return RedirectToAction("Error", "Home", new { message = "Invalid payment information" });
				}

				// Find order by Paymob order ID
				var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.PaymobOrderId == paymobOrderId);
				if (orderHeader == null)
				{
					_logger.LogError("Order not found for Paymob order ID: {PaymobOrderId}", paymobOrderId);
					return RedirectToAction("Error", "Home", new { message = "Order not found" });
				}

				// For successful payments, redirect to success page
				if (isSuccess)
				{
					return RedirectToAction("Success", new { order_id = paymobOrderId });
				}
				
				// For failed payments, redirect to failure page
				return RedirectToAction("Failure");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing payment callback");
				return RedirectToAction("Error", "Home", new { message = "Error processing payment" });
			}
		}

		#region Paymob API Methods

		private async Task<PaymobAuthResponse?> GetPaymobAuthToken()
		{
			try
			{
				var requestData = new
				{
					api_key = _paymob.ApiKey
				};

				var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens", requestData);
				if (!response.IsSuccessStatusCode)
				{
					_logger.LogError("Paymob auth failed with status: {StatusCode}", response.StatusCode);
					return null;
				}

				var result = await response.Content.ReadFromJsonAsync<PaymobAuthResponse>();
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting Paymob auth token");
				return null;
			}
		}

		private async Task<int?> RegisterOrderWithPaymob(string authToken, OrderHeader orderHeader)
		{
			try
			{
				var requestData = new
				{
					auth_token = authToken,
					delivery_needed = false,
					amount_cents = (int)(orderHeader.OrderTotal * 100),
					currency = "EGP",
					items = new List<object>()
				};

				var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/ecommerce/orders", requestData);
				if (!response.IsSuccessStatusCode)
				{
					_logger.LogError("Paymob order creation failed with status: {StatusCode}", response.StatusCode);
					return null;
				}

				var result = await response.Content.ReadFromJsonAsync<PaymobOrderResponse>();
				return result?.Id;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error registering order with Paymob");
				return null;
			}
		}

		private async Task<string?> GetPaymentKey(string authToken, int paymobOrderId, OrderHeader orderHeader)
		{
			try
			{
				var requestData = new
				{
					auth_token = authToken,
					amount_cents = (int)(orderHeader.OrderTotal * 100),
					expiration = 3600,
					order_id = paymobOrderId,
					billing_data = new
					{
						apartment = orderHeader.apartment ?? "NA",
						email = orderHeader.email,
						floor = orderHeader.floor ?? "NA",
						first_name = orderHeader.first_name,
						street = orderHeader.street,
						building = orderHeader.building,
						phone_number = orderHeader.phone_number,
						shipping_method = "NA",
						postal_code = "NA",
						city = orderHeader.state,
						country = orderHeader.country,
						last_name = orderHeader.last_name,
						state = orderHeader.state
					},
					currency = "EGP",
					integration_id = int.Parse(_paymob.IntegrationId),
					lock_order_when_paid = true
				};

				var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/payment_keys", requestData);
				if (!response.IsSuccessStatusCode)
				{
					_logger.LogError("Paymob payment key creation failed with status: {StatusCode}", response.StatusCode);
					return null;
				}

				var result = await response.Content.ReadFromJsonAsync<PaymobPaymentKeyResponse>();
				return result?.Token;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting payment key from Paymob");
				return null;
			}
		}

		private string CalculateHmac(string data, string secret)
		{
			using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
			byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
			
			return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		}

		#endregion
	}
}