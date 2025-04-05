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

		// Method for session-based payment flow (no order created until payment success)
		[Authorize]
		public async Task<IActionResult> InitializePaymentOnly()
		{
			try
			{
				// 1. Retrieve checkout data from session
				var checkoutDataJson = HttpContext.Session.GetString("CheckoutData");
				if (string.IsNullOrEmpty(checkoutDataJson))
				{
					_logger.LogError("Checkout data not found in session");
					return RedirectToAction("Error", "Home", new { message = "Checkout information not found. Please try again." });
				}
				
				// 2. Deserialize checkout data
				var checkoutData = JsonSerializer.Deserialize<JsonElement>(checkoutDataJson);
				var orderTotal = checkoutData.GetProperty("OrderTotal").GetDouble();
				var userInfo = checkoutData.GetProperty("UserInfo");
				
				// 3. Authenticate with Paymob
				var authResponse = await GetPaymobAuthToken();
				if (authResponse?.Token == null)
				{
					_logger.LogError("Failed to authenticate with Paymob");
					return RedirectToAction("Error", "Home", new { message = "Payment service unavailable" });
				}
				
				// 4. Create temporary billing data for Paymob
				var billingData = new
				{
					first_name = userInfo.GetProperty("FirstName").GetString(),
					last_name = userInfo.GetProperty("LastName").GetString(),
					email = userInfo.GetProperty("Email").GetString(),
					phone_number = userInfo.GetProperty("PhoneNumber").GetString(),
					street = userInfo.GetProperty("Street").GetString(),
					building = userInfo.GetProperty("Building").GetString(),
					apartment = userInfo.GetProperty("Apartment").GetString() ?? "NA",
					floor = userInfo.GetProperty("Floor").GetString() ?? "NA",
					state = userInfo.GetProperty("State").GetString(),
					country = userInfo.GetProperty("Country").GetString(),
					shipping_method = "NA",
					postal_code = "NA",
					city = userInfo.GetProperty("State").GetString()
				};
				
				// 5. Register with Paymob
				var paymobOrderId = await RegisterOrderWithPaymob(authResponse.Token, orderTotal);
				if (paymobOrderId == null)
				{
					_logger.LogError("Failed to register with Paymob");
					return RedirectToAction("Error", "Home", new { message = "Payment service unavailable" });
				}
				
				// 6. Get payment key
				var paymentKeyToken = await GetPaymentKey(authResponse.Token, paymobOrderId.Value, orderTotal, billingData);
				if (paymentKeyToken == null)
				{
					_logger.LogError("Failed to get payment key from Paymob");
					return RedirectToAction("Error", "Home", new { message = "Payment service unavailable" });
				}
				
				// 7. Store paymob order ID in TempData for retrieval in callback
				TempData["PaymobOrderId"] = paymobOrderId.Value;
				
				// 8. Redirect to Paymob iframe
				return Redirect($"https://accept.paymob.com/api/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKeyToken}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error initializing direct payment");
				return RedirectToAction("Error", "Home", new { message = "Payment initialization failed. Please try again." });
			}
		}

		[HttpGet]
		public IActionResult Success(string order_id)
		{
			_logger.LogInformation("Payment success callback received for order: {OrderId}", order_id);
			
			// Parse the Paymob order ID 
			if (!int.TryParse(order_id, out int paymobOrderId))
			{
				_logger.LogError("Invalid order ID in success callback: {OrderId}", order_id);
				return RedirectToAction("Error", "Home", new { message = "Invalid payment information" });
			}

			// Get the checkout data from session
			var checkoutDataJson = HttpContext.Session.GetString("CheckoutData");
			if (string.IsNullOrEmpty(checkoutDataJson))
			{
				_logger.LogError("Checkout data not found in session");
				return RedirectToAction("Error", "Home", new { message = "Payment session expired. Please try again." });
			}

			// Create the order now after successful payment
			var txnId = "pm_" + Guid.NewGuid().ToString("N").Substring(0, 16);
			return RedirectToAction("CreateOrderAfterPayment", "Checkout", new { 
				transactionId = txnId, 
				paymobOrderId = paymobOrderId 
			});
		}

		[HttpGet]
		public IActionResult Failure(string order_id)
		{
			_logger.LogWarning("Payment failure callback received for order: {OrderId}", order_id);
			
			// Clear checkout data from session
			HttpContext.Session.Remove("CheckoutData");
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

				// Extract payment data
				var obj = payload.GetProperty("obj");
				var success = obj.GetProperty("success").GetBoolean();
				var paymobOrderId = obj.GetProperty("order").GetProperty("id").GetInt32();
				var txnId = obj.GetProperty("id").GetString();

				// For webhook, we don't need to do anything since orders are created only after
				// successful payment via the success callback/redirect
				_logger.LogInformation("Payment webhook received for Paymob order ID: {PaymobOrderId}, Success: {Success}", 
					paymobOrderId, success);
				
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
		public IActionResult Callback(
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

				// Handle success/failure
				if (isSuccess)
				{
					return RedirectToAction("Success", new { order_id = paymobOrderId });
				}
				else
				{
					return RedirectToAction("Failure", new { order_id = paymobOrderId });
				}
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

		private async Task<int?> RegisterOrderWithPaymob(string authToken, double orderTotal)
		{
			try
			{
				var requestData = new
				{
					auth_token = authToken,
					delivery_needed = false,
					amount_cents = (int)(orderTotal * 100),
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

		private async Task<string?> GetPaymentKey(string authToken, int paymobOrderId, double orderTotal, object billingData)
		{
			try
			{
				var requestData = new
				{
					auth_token = authToken,
					amount_cents = (int)(orderTotal * 100),
					expiration = 3600,
					order_id = paymobOrderId,
					billing_data = billingData,
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