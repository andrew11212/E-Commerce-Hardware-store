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
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Paymob _paymob;
        private readonly ILogger<CartController> _logger;
        private readonly IAntiforgery _antiforgery;

        [BindProperty]
        public CartViewModel CartVM { get; set; } = new CartViewModel(); // Initialize directly

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
                if (userId == null) return Unauthorized();

                CartVM.CartList = _unitOfWork.CartRepositery.GetAll(c => c.ApplicationUserId == userId, "Product").ToList();
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
        public IActionResult AddToCart(int productId, int count = 1)
        {
            var userId = GetValidatedUserId();
            if (userId == null) return Unauthorized();

            var product = _unitOfWork.ProductRepository.Get(p => p.ProductID == productId);
            if (product == null || product.StockQuantity < count)
            {
                TempData["Error"] = "Product is unavailable or insufficient stock";
                return RedirectToAction("Index", "Home");
            }

            var cartItem = _unitOfWork.CartRepositery.Get(c => c.ApplicationUserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Count += count;
                _unitOfWork.CartRepositery.Update(cartItem);
            }
            else
            {
                _unitOfWork.CartRepositery.Add(new ShopingCart
                {
                    ApplicationUserId = userId,
                    ProductId = productId,
                    Count = count
                });
            }

            _unitOfWork.Save();
            TempData["Success"] = "Item added to cart successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCart(int cartId, int count)
        {
            var cart = _unitOfWork.CartRepositery.Get(c => c.Id == cartId && c.ApplicationUserId == GetValidatedUserId());

            if (cart == null)
            {
                TempData["Error"] = "Cart item not found";
                return RedirectToAction("Index");
            }

            var product = _unitOfWork.ProductRepository.Get(p => p.ProductID == cart.ProductId);
            if (product == null || product.StockQuantity < count)
            {
                TempData["Error"] = "Requested quantity exceeds available stock";
                return RedirectToAction("Index");
            }

            cart.Count = count;
            _unitOfWork.CartRepositery.Update(cart);
            _unitOfWork.Save();

            TempData["Success"] = "Cart updated successfully";
            return RedirectToAction("Index");
        }

        #endregion

        public IActionResult Summary()
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                CartVM.CartList = _unitOfWork.CartRepositery.GetAll(u => u.ApplicationUserId == userId, "Product").ToList();
                CartVM.OrderHeader = new OrderHeader();

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
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPOST()
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                var cartItems = _unitOfWork.CartRepositery.GetAll(c => c.ApplicationUserId == userId, "Product").ToList();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                foreach (var cart in cartItems)
                {
                    var product = _unitOfWork.ProductRepository.Get(p => p.ProductID == cart.ProductId);
                    if (product == null || product.StockQuantity < cart.Count)
                    {
                        TempData["Error"] = $"Item {cart.Product.Name} is out of stock";
                        return RedirectToAction("Index");
                    }
                    product.StockQuantity -= cart.Count;
                    _unitOfWork.ProductRepository.Ubdate(product);
                }

                CartVM.OrderHeader.ApplicationUserId = userId;
                SetOrderAndPaymentStatus(CartVM.OrderHeader);

                _unitOfWork.OrderHeader.Add(CartVM.OrderHeader);
                _unitOfWork.Save();

                var user = _unitOfWork.applciationUserRepository.Get(u => u.Id == userId);
                if (user == null) return NotFound("User not found");

                return await ProcessPaymentAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order processing failed");
                TempData["Error"] = "Order processing failed. Please try again.";
                return RedirectToAction("Summary");
            }
        }

        [HttpPost]
        public IActionResult PaymentCallback()
        {
            try
            {
                var transactionId = Request.Form["transaction_id"];
                var order = Request.Form["order"];
                var success = Request.Form["success"] == "true";

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

                var orderHeader = _unitOfWork.OrderHeader.Get(o => o.PaymobOrderId == paymobOrderId);
                if (orderHeader == null)
                {
                    _logger.LogWarning("Order not found for PaymobOrderId={PaymobOrderId} in PaymentCallback", paymobOrderId);
                    return NotFound("Order not found");
                }

                UpdateOrderStatus(orderHeader, success, transactionId);
                _unitOfWork.Save();

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

        private string SanitizeInput(string input) => string.IsNullOrWhiteSpace(input) ? "NA" : Regex.Replace(input.Trim(), @"[<>&'""\\/]", "").Substring(0, Math.Min(255, input.Trim().Length));

        private string SanitizePhoneNumber(string phoneNumber) => string.IsNullOrWhiteSpace(phoneNumber) ? "+201234567890" : Regex.Replace(phoneNumber.Trim(), @"[^0-9+]", "").StartsWith("+") ? phoneNumber.Substring(0, Math.Min(20, phoneNumber.Length)) : ("+20" + phoneNumber).Substring(0, Math.Min(20, phoneNumber.Length + 3));

        private void SetOrderAndPaymentStatus(OrderHeader orderHeader)
        {
            orderHeader.PaymentStatus = SD.Payment_Status_Pending;
            orderHeader.OrderStatus = SD.Status_Pending;
        }

        private async Task<IActionResult> ProcessPaymentAsync(ApplicationUser user)
        {
            try
            {
                string authToken = await GetAuthTokenAsync();
                int paymobOrderId = await CreateOrderAsync(authToken, CartVM.OrderHeader.OrderTotal);
                string paymentKey = await GetPaymentKeyAsync(authToken, paymobOrderId, CartVM.OrderHeader.OrderTotal, user);

                CartVM.OrderHeader.PaymobOrderId = paymobOrderId;
                _unitOfWork.OrderHeader.Update(CartVM.OrderHeader);
                _unitOfWork.Save();

                string paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymob.IframeId}?payment_token={paymentKey}";
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed");
                throw;
            }
        }

        private async Task<string> GetAuthTokenAsync()
        {
            using var client = new RestClient(_paymob.BaseUrl + "auth/tokens");
            var request = new RestRequest().AddHeader("Content-Type", "application/json").AddJsonBody(new { api_key = _paymob.ApiKey });
            var response = await client.PostAsync<PaymobAuthResponse>(request);
            return response?.Token ?? throw new Exception("Failed to authenticate with Paymob");
        }

        private async Task<int> CreateOrderAsync(string authToken, double amount)
        {
            using var client = new RestClient(_paymob.BaseUrl + "ecommerce/orders");
            var request = new RestRequest().AddHeader("Authorization", $"Bearer {authToken}").AddHeader("Content-Type", "application/json").AddJsonBody(new { amount_cents = (int)(amount * 100), currency = "EGP", merchant_id = _paymob.MerchantId });
            var response = await client.PostAsync<PaymobOrderResponse>(request);
            return response?.Id ?? throw new Exception("Failed to create Paymob order");
        }

        private async Task<string> GetPaymentKeyAsync(string authToken, int orderId, double amount, ApplicationUser user)
        {
            using var client = new RestClient(_paymob.BaseUrl + "acceptance/payment_keys");
            var request = new RestRequest().AddHeader("Authorization", $"Bearer {authToken}").AddHeader("Content-Type", "application/json").AddJsonBody(new { amount_cents = (int)(amount * 100), currency = "EGP", order_id = orderId, billing_data = new { email = user.Email ?? "unknown@example.com", first_name = SanitizeInput(user.Name.Split(' ').FirstOrDefault() ?? "Unknown"), last_name = SanitizeInput(user.Name.Contains(" ") ? user.Name.Split(' ')[1] : "Unknown"), phone_number = SanitizePhoneNumber(user.PhoneNumber), street = SanitizeInput(user.StreetAddress ?? "NA"), building = "NA", city = SanitizeInput(user.City ?? "NA"), state = SanitizeInput(user.State ?? "NA"), postal_code = SanitizeInput(user.PostalCode ?? "NA"), country = "EG", apartment = "NA", floor = "NA" }, integration_id = _paymob.IntegrationId });
            var response = await client.PostAsync<PaymobPaymentKeyResponse>(request);
            return response?.Token ?? throw new Exception("Failed to get payment key");
        }

        private bool ValidateCallback(string orderId, int? paymobOrderId, string transactionId)
        {
            // IMPORTANT: Implement HMAC validation here for security.
            if (paymobOrderId == null || orderId != paymobOrderId.ToString() || string.IsNullOrEmpty(transactionId)) return false;
            return true;
        }

        private void UpdateOrderStatus(OrderHeader orderHeader, bool success, string transactionId)
        {
            orderHeader.PaymentStatus = success ? SD.Payment_Status_Approved : SD.Payment_Status_Rejected;
            orderHeader.OrderStatus = success ? SD.Status_Approved : SD.Status_Cancelled;
            orderHeader.TransactionId = transactionId;
            _unitOfWork.OrderHeader.Update(orderHeader);
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
        #endregion
    }
}