using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging; // Make sure this is included
using System.Threading.Tasks; // Added for Task
using System.Linq; // Make sure this is included

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

		public async Task<IActionResult> Index()
		{
			var viewModel = new HomeIndexViewModel
			{
				// Get all products, including laptops
				Products = (await _unitOfWork.ProductRepository.GetAllAsync(null, includeProperties: "Category,Brand,ProductType")).ToList(),

				// Get only Laptops.  This assumes you have a way to filter for laptops, such as by ProductType or Category.
				Laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand,ProductType")).ToList(),
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
	}
}