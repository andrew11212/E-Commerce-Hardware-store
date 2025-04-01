using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging; // Make sure this is included
using System.Threading.Tasks; // Added for Task
using System.Linq;
using Microsoft.EntityFrameworkCore; // Make sure this is included

namespace FutureTechnologyE_Commerce.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IUnitOfWork _unitOfWork;

		public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
		{
			_logger = logger;
			_unitOfWork = unitOfWork;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, string searchString = "")
		{
			// Get queryable products with related entities
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand,ProductType");

			// Apply search filter if searchString is provided
			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}

			// Pagination settings
			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var products = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Populate view model
			var viewModel = new HomeIndexViewModel
			{
				Products = products, // Paginated list only
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
				SearchString = searchString,
				Laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand,ProductType"))
					.Take(5) // Optional: limit to 5 laptops for performance
					.ToList()
			};

			return View(viewModel);
		}

		public async Task<IActionResult> Details(int id)
		{
			var product = await _unitOfWork.ProductRepository.GetAsync(
				p => p.ProductID == id,
				"Category",   // Separate navigation properties
				"Brand",
				"ProductType"
			);

			if (product == null)
			{
				return NotFound();
			}

			// Get related products
			var relatedProducts = (await _unitOfWork.ProductRepository
				.GetAllAsync(p => p.CategoryID == product.CategoryID &&
									 p.ProductID != product.ProductID,
									 includeProperties: "Category"))
				.Take(4)
				.ToList();

			ViewBag.RelatedProducts = relatedProducts;

			return View(product);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddToCart(int productId, int quantity)
		{
			// Input validation
			if (quantity <= 0)
			{
				TempData["error"] = "Quantity must be at least 1";
				return RedirectToAction(nameof(Details), new { id = productId });
			}

			var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId);
			if (product == null)
			{
				TempData["error"] = "Product not found";
				return RedirectToAction(nameof(Index));
			}

			// Authentication check
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				TempData["error"] = "Please login to add items to cart";
				return RedirectToAction("Login", "Account");
			}

			// Cart operations
			var cartItem = await _unitOfWork.CartRepositery
				.GetAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

			if (cartItem != null)
			{
				cartItem.Count += quantity;
				await _unitOfWork.CartRepositery.UpdateAsync(cartItem);
			}
			else
			{
				var newCartItem = new ShopingCart
				{
					ApplicationUserId = userId,
					ProductId = productId,
					Count = quantity
				};
				await _unitOfWork.CartRepositery.AddAsync(newCartItem);
			}

			await _unitOfWork.SaveAsync();
			TempData["success"] = "Item added to cart successfully";
			return RedirectToAction(nameof(Details), new { id = productId });
		}
		public async Task<IActionResult> GetAllProducts()
		{
			var viewModel = new HomeIndexViewModel
			{
				// Get all products, including laptops
				Products = (await _unitOfWork.ProductRepository.GetAllAsync(null, includeProperties: "Category,Brand,ProductType")).ToList(),
			};
			return View(viewModel);
		}
		public async Task<IActionResult> GetAllLaptops ()
		{
			var viewModel = new HomeIndexViewModel
			{
				Laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand,ProductType")).ToList(),
			};
			return View(viewModel);
		}
		public IActionResult Privacy()
		{
			return View();
		}
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}