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
using FutureTechnologyE_Commerce.Models.ViewModels;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize]
	[EnableRateLimiting("fixed")]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CartController> _logger;

		[BindProperty]
		public CartViewModel CartVM { get; set; } = new CartViewModel();

		public CartController(
			IUnitOfWork unitOfWork,
			ILogger<CartController> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
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

				// Get the cart item without including the product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId);
				if (cart == null)
				{
					return Json(new { success = false, error = "Cart item not found" });
				}

				// Get the product separately
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
				double price = Math.Round((double)product.Price, 2);
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

				// Get the cart item without including the product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId && c.ApplicationUserId == userId);
				if (cart == null)
				{
					return Json(new { success = false, error = "Cart item not found" });
				}

				double price = 0;
				bool removed = false;
				
				// If count is greater than 1, we'll need the product price
				if (cart.Count > 1)
				{
					var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == cart.ProductId);
					if (product != null)
					{
						price = Math.Round((double)product.Price, 2);
					}
				}

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

				// Get the product first
				var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
				if (product == null)
				{
					return Json(new { success = false, error = "Product not found" });
				}
				
				// Find existing cart item for this product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

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
						Count = 1
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

				// Find existing cart item for this product - without including the Product
				var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);
				
				if (cart == null)
				{
					// Nothing to decrement
					return Json(new { success = false, error = "Product not in cart" });
				}

				bool removed = false;
				double price = 0;
				
				// Get the product price if we need it
				if (cart.Count > 1)
				{
					var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
					if (product != null)
					{
						price = Math.Round((double)product.Price, 2);
					}
				}
				
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

		public async Task<IActionResult> Checkout()
		{
			try
			{
				var userId = GetValidatedUserId();
				if (userId == null) return Unauthorized();

				// Redirect to the CheckoutController
				return RedirectToAction("Index", "Checkout");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Cart/Checkout");
				TempData["Error"] = "Failed to proceed to checkout. Please try again.";
				return RedirectToAction("Index");
			}
		}

		#endregion

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
		#endregion
	}
}