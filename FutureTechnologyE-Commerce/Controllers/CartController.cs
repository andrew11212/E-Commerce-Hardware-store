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

				CartVM.CartList = (await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product")).ToList();
				CartVM.OrderHeader = new OrderHeader();

				// Set the price for each cart item from its Product
				foreach (var cart in CartVM.CartList)
				{
					cart.price = Math.Round((double)cart.Product.Price, 2);
				}

				// Calculate total with proper decimal handling for EGP
				CartVM.OrderHeader.OrderTotal = Math.Round(CartVM.CartList.Sum(cart => cart.price * cart.Count), 2);
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

		// JSON method to return current cart count for AJAX updates
		[HttpGet]
		public async Task<IActionResult> GetCartCount()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId);
				int count = cartItems.Sum(c => c.Count);
				return Json(new { success = true, count });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting cart count");
				return Json(new { success = false, count = 0 });
			}
		}

		private async Task<(bool Success, string ErrorMessage)> AddItemToCartInternal(string userId, int productId, int count)
		{
			if (string.IsNullOrEmpty(userId))
			{
				// This shouldn't happen if called after [Authorize]
				return (false, "User not identified.");
			}

			var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
			if (product == null)
			{
				return (false, "Product not found.");
			}
			if (product.StockQuantity < count)
			{
				return (false, "Insufficient stock for initial add.");
			}

			var cartItem = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

			if (cartItem != null)
			{
				// Check stock before adding to existing count
				if (product.StockQuantity < cartItem.Count + count)
				{
					return (false, "Requested quantity exceeds available stock.");
				}
				cartItem.Count += count;
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
			return (true, null); // Success
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize]
		public async Task<IActionResult> AddToCart(int productId, int count = 1)
		{
			var userId = GetValidatedUserId();
			var (success, errorMessage) = await AddItemToCartInternal(userId, productId, count);

			if (success)
			{
				TempData["Success"] = "Item added to cart successfully";
			}
			else
			{
				TempData["Error"] = errorMessage ?? "Could not add item to cart.";
			}
			return RedirectToAction("Index"); 
		}

		[HttpGet]
		public async Task<IActionResult> RequestAddToCartAfterLogin(int productId, int count = 1)
		{
			var userId = GetValidatedUserId();
			var (success, errorMessage) = await AddItemToCartInternal(userId, productId, count);

			if (success)
			{
				TempData["Success"] = "Item added to cart successfully after login.";
			}
			else
			{
				TempData["Error"] = errorMessage ?? "Could not add item to cart after login.";
				// Optional: Redirect back to product page if add fails?
				// return RedirectToAction("Details", "Product", new { id = productId });
			}

			// Always redirect to Cart Index from this flow
			return RedirectToAction("Index");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Plus(int cartId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId, "Product");
				if (cart == null)
				{
					TempData["Error"] = "Cart item not found";
					return RedirectToAction("Index");
				}

				var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == cart.ProductId);
				if (product == null)
				{
					TempData["Error"] = "Product not found";
					return RedirectToAction("Index");
				}

				// Check if stock allows increment
				if (product.StockQuantity <= cart.Count)
				{
					TempData["Error"] = "Cannot add more items. Maximum stock reached.";
					return RedirectToAction("Index");
				}

				// Increment count
				cart.Count++;
				await _unitOfWork.CartRepositery.UpdateAsync(cart);
				await _unitOfWork.SaveAsync();

				TempData["Success"] = "Quantity increased";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error incrementing cart item quantity for cartId: {CartId}", cartId);
				TempData["Error"] = "Failed to update quantity. Please try again.";
			}

			// Get the referrer to determine where to return
			string referer = Request.Headers["Referer"].ToString();
			if (!string.IsNullOrEmpty(referer) && referer.Contains("/Product/Details/"))
			{
				// Extract the product ID from the referrer URL
				var match = Regex.Match(referer, @"/Product/Details/(\d+)");
				if (match.Success && int.TryParse(match.Groups[1].Value, out int productId))
				{
					return RedirectToAction("Details", "Product", new { id = productId });
				}
			}

			return RedirectToAction("Index");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Minus(int cartId)
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

				// If count is 1, remove the item
				if (cart.Count <= 1)
				{
					await _unitOfWork.CartRepositery.RemoveAsync(cart);
					TempData["Success"] = "Item removed from cart";
				}
				else
				{
					// Decrement count
					cart.Count--;
					await _unitOfWork.CartRepositery.UpdateAsync(cart);
					TempData["Success"] = "Quantity decreased";
				}

				await _unitOfWork.SaveAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error decrementing cart item quantity for cartId: {CartId}", cartId);
				TempData["Error"] = "Failed to update quantity. Please try again.";
			}

			// Get the referrer to determine where to return
			string referer = Request.Headers["Referer"].ToString();
			if (!string.IsNullOrEmpty(referer) && referer.Contains("/Product/Details/"))
			{
				// Extract the product ID from the referrer URL
				var match = Regex.Match(referer, @"/Product/Details/(\d+)");
				if (match.Success && int.TryParse(match.Groups[1].Value, out int productId))
				{
					return RedirectToAction("Details", "Product", new { id = productId });
				}
			}

			return RedirectToAction("Index");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateCart(int cartId, int count)
		{
			var userId = GetValidatedUserId();

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

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> PlusByProduct(int productId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				// Find existing cart item for this product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);
				var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
				
				if (product == null)
				{
					TempData["Error"] = "Product not found";
					return RedirectToAction("Index", "Home");
				}

				if (cart == null)
				{
					// Product not in cart, add it
					if (product.StockQuantity < 1)
					{
						TempData["Error"] = "Product is out of stock";
						return RedirectToAction("Details", "Product", new { id = productId });
					}

					await _unitOfWork.CartRepositery.AddAsync(new ShopingCart
					{
						ApplicationUserId = userId,
						ProductId = productId,
						Count = 1
					});
					TempData["Success"] = "Product added to cart";
				}
				else
				{
					// Check if stock allows increment
					if (product.StockQuantity <= cart.Count)
					{
						TempData["Error"] = "Cannot add more items. Maximum stock reached.";
						return RedirectToAction("Details", "Product", new { id = productId });
					}

					// Increment count
					cart.Count++;
					await _unitOfWork.CartRepositery.UpdateAsync(cart);
					TempData["Success"] = "Quantity increased";
				}

				await _unitOfWork.SaveAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error incrementing cart item quantity for productId: {ProductId}", productId);
				TempData["Error"] = "Failed to update quantity. Please try again.";
			}

			return RedirectToAction("Details", "Product", new { id = productId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MinusByProduct(int productId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				// Find existing cart item for this product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);
				
				if (cart == null)
				{
					// Nothing to decrement
					return RedirectToAction("Details", "Product", new { id = productId });
				}

				// If count is 1, remove the item
				if (cart.Count <= 1)
				{
					await _unitOfWork.CartRepositery.RemoveAsync(cart);
					TempData["Success"] = "Item removed from cart";
				}
				else
				{
					// Decrement count
					cart.Count--;
					await _unitOfWork.CartRepositery.UpdateAsync(cart);
					TempData["Success"] = "Quantity decreased";
				}

				await _unitOfWork.SaveAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error decrementing cart item quantity for productId: {ProductId}", productId);
				TempData["Error"] = "Failed to update quantity. Please try again.";
			}

			return RedirectToAction("Details", "Product", new { id = productId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> PlusAjax(int cartId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Json(new { success = false, error = "Not authorized" });

				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId, "Product");
				if (cart == null)
				{
					return Json(new { success = false, error = "Cart item not found" });
				}

				var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == cart.ProductId);
				if (product == null)
				{
					return Json(new { success = false, error = "Product not found" });
				}

				// Check if stock allows increment
				if (product.StockQuantity <= cart.Count)
				{
					return Json(new { success = false, error = "Cannot add more items. Maximum stock reached." });
				}

				// Increment count
				cart.Count++;
				await _unitOfWork.CartRepositery.UpdateAsync(cart);
				await _unitOfWork.SaveAsync();

				// Return updated count and total price
				double price = Math.Round((double)cart.Product.Price, 2);
				double lineTotal = Math.Round(price * cart.Count, 2);
				
				// Get updated cart total
				var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product");
				double cartTotal = Math.Round(cartItems.Sum(c => Math.Round((double)c.Product.Price, 2) * c.Count), 2);
				
				return Json(new { 
					success = true, 
					count = cart.Count, 
					lineTotal,
					cartTotal,
					message = "Quantity increased" 
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error incrementing cart item quantity for cartId: {CartId}", cartId);
				return Json(new { success = false, error = "Failed to update quantity. Please try again." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MinusAjax(int cartId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Json(new { success = false, error = "Not authorized" });

				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId, "Product");
				if (cart == null)
				{
					return Json(new { success = false, error = "Cart item not found" });
				}

				bool removed = false;
				// If count is 1, remove the item
				if (cart.Count <= 1)
				{
					await _unitOfWork.CartRepositery.RemoveAsync(cart);
					removed = true;
				}
				else
				{
					// Decrement count
					cart.Count--;
					await _unitOfWork.CartRepositery.UpdateAsync(cart);
				}

				await _unitOfWork.SaveAsync();

				// Get updated cart total
				var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product");
				double cartTotal = Math.Round(cartItems.Sum(c => Math.Round((double)c.Product.Price, 2) * c.Count), 2);

				if (removed)
				{
					return Json(new { 
						success = true, 
						removed = true,
						cartTotal,
						message = "Item removed from cart" 
					});
				}
				else
				{
					// Return updated count and total price
					double price = Math.Round((double)cart.Product.Price, 2);
					double lineTotal = Math.Round(price * cart.Count, 2);
					
					return Json(new { 
						success = true, 
						removed = false,
						count = cart.Count, 
						lineTotal,
						cartTotal,
						message = "Quantity decreased" 
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error decrementing cart item quantity for cartId: {CartId}", cartId);
				return Json(new { success = false, error = "Failed to update quantity. Please try again." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> PlusByProductAjax(int productId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Json(new { success = false, error = "Not authorized" });

				// Find existing cart item for this product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);
				var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
				
				if (product == null)
				{
					return Json(new { success = false, error = "Product not found" });
				}

				bool isNew = false;
				if (cart == null)
				{
					// Product not in cart, add it
					if (product.StockQuantity < 1)
					{
						return Json(new { success = false, error = "Product is out of stock" });
					}

					cart = new ShopingCart
					{
						ApplicationUserId = userId,
						ProductId = productId,
						Count = 1,
						Product = product // Set the product for later use
					};
					await _unitOfWork.CartRepositery.AddAsync(cart);
					isNew = true;
				}
				else
				{
					// Check if stock allows increment
					if (product.StockQuantity <= cart.Count)
					{
						return Json(new { success = false, error = "Cannot add more items. Maximum stock reached." });
					}

					// Increment count
					cart.Count++;
					// Set product if not already set
					cart.Product = cart.Product ?? product;
					await _unitOfWork.CartRepositery.UpdateAsync(cart);
				}

				await _unitOfWork.SaveAsync();

				// Get updated cart total and count
				var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product");
				double cartTotal = Math.Round(cartItems.Sum(c => Math.Round((double)c.Product.Price, 2) * c.Count), 2);
				int cartCount = cartItems.Sum(c => c.Count);

				// Return price and quantity info
				double price = Math.Round((double)product.Price, 2);
				double lineTotal = Math.Round(price * cart.Count, 2);
				
				return Json(new { 
					success = true, 
					count = cart.Count,
					isNew,
					cartCount,
					cartTotal,
					lineTotal,
					message = isNew ? "Product added to cart" : "Quantity increased" 
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error incrementing cart item quantity for productId: {ProductId}", productId);
				return Json(new { success = false, error = "Failed to update quantity. Please try again." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MinusByProductAjax(int productId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Json(new { success = false, error = "Not authorized" });

				// Find existing cart item for this product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId, "Product");
				
				if (cart == null)
				{
					// Nothing to decrement
					return Json(new { success = false, error = "Product not in cart" });
				}

				bool removed = false;
				// If count is 1, remove the item
				if (cart.Count <= 1)
				{
					await _unitOfWork.CartRepositery.RemoveAsync(cart);
					removed = true;
				}
				else
				{
					// Decrement count
					cart.Count--;
					await _unitOfWork.CartRepositery.UpdateAsync(cart);
				}

				await _unitOfWork.SaveAsync();

				// Get updated cart total and count
				var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product");
				double cartTotal = Math.Round(cartItems.Sum(c => Math.Round((double)c.Product.Price, 2) * c.Count), 2);
				int cartCount = cartItems.Sum(c => c.Count);

				if (removed)
				{
					return Json(new { 
						success = true, 
						removed = true,
						cartCount,
						cartTotal,
						message = "Item removed from cart" 
					});
				}
				else
				{
					// Return updated count and total price
					double price = Math.Round((double)cart.Product.Price, 2);
					double lineTotal = Math.Round(price * cart.Count, 2);
					
					return Json(new { 
						success = true, 
						removed = false,
						count = cart.Count,
						cartCount,
						cartTotal,
						lineTotal,
						message = "Quantity decreased" 
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error decrementing cart item quantity for productId: {ProductId}", productId);
				return Json(new { success = false, error = "Failed to update quantity. Please try again." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetProductCartInfo(int productId)
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Json(new { success = false, error = "Not authorized" });

				var cartItem = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);
				
				if (cartItem == null)
				{
					return Json(new { success = true, inCart = false });
				}
				
				return Json(new { 
					success = true, 
					inCart = true, 
					count = cartItem.Count
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking if product {ProductId} is in cart", productId);
				return Json(new { success = false, error = "Failed to check cart" });
			}
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

				// Set the price for each cart item
				foreach (var cart in CartVM.CartList)
				{
					cart.price = Math.Round((double)cart.Product.Price, 2);
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

				// Calculate total with proper decimal handling for EGP
				CartVM.OrderHeader.OrderTotal = Math.Round(CartVM.CartList.Sum(cart => cart.price * cart.Count), 2);
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

				// Set the price for each cart item
				foreach (var cart in cartItems)
				{
					cart.price = Math.Round((double)cart.Product.Price, 2);
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
			string.IsNullOrWhiteSpace(input) ? " " : Regex.Replace(input.Trim(), @"[<>&'""\\/]", "").Substring(0, Math.Min(255, input.Trim().Length));

		private string SanitizePhoneNumber(string phoneNumber) =>
			string.IsNullOrWhiteSpace(phoneNumber) ? " " :
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