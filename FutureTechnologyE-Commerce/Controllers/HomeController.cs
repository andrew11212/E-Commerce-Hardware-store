using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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

		public async Task<IActionResult> Index(string searchString = "")
		{
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}

			// Get top reviews (with high ratings)
			var topReviews = await _unitOfWork.ReviewRepository
				.GetAllAsync(
					r => r.Rating >= 4, 
					includeProperties: "User,Product"
				);

			var viewModel = new HomeIndexViewModel
			{
				Products = (await _unitOfWork.ProductRepository.GetAllAsync(c => c.IsBestseller, includeProperties: "Category,Brand")), // Use the paginated list
				SearchString = searchString,
				Accessories = (await _unitOfWork.ProductRepository.GetAllAsync(c => c.Category.Name == "Accessories", includeProperties: "Category,Brand")),
				Laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand"))
					.Take(5)
					.ToList(),
				TopReviews = topReviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.ReviewDate).Take(3).ToList()
			};

			return View(viewModel);
		}

		public async Task<IActionResult> Details(int id)
		{
			var product = await _unitOfWork.ProductRepository.GetAsync(
				p => p.ProductID == id,
				"Category",
				"Brand"
			);

			if (product == null)
			{
				return NotFound();
			}

			var relatedProducts = (await _unitOfWork.ProductRepository
				.GetAllAsync(p => p.CategoryID == product.CategoryID &&
									 p.ProductID != product.ProductID,
									 includeProperties: "Category"))
				.Take(4)
				.ToList();

			// Get reviews for this product
			var reviews = _unitOfWork.ReviewRepository.GetReviewsByProductId(id);
			var averageRating = _unitOfWork.ReviewRepository.GetAverageRatingByProductId(id);

			ViewBag.RelatedProducts = relatedProducts;
			ViewBag.Reviews = reviews;
			ViewBag.AverageRating = averageRating;

			return View(product);
		}

		public async Task<IActionResult> GetAllProducts(int pageNumber = 1, string searchString = "", string category = "")
		{
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(c => c.Brand.Name.ToLower().Contains(searchString) || c.Name.ToLower().Contains(searchString));
			}

			if (!string.IsNullOrEmpty(category))
			{
				category = category.Trim();
				query = query.Where(p => p.Category.Name.Contains(category));
			}

			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var products = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Define the predefined category options
			var categoryOptions = new List<string> { "Mouse", "Keyboard", "Mousepad", "Printer" };

			var viewModel = new HomeIndexViewModel
			{
				SearchString = searchString,
				Category = category,
				Products = products,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
				CategoryOptions = categoryOptions
			};
			return View(viewModel);
		}

		public async Task<IActionResult> GetFilteredProducts(int pageNumber = 1, string searchString = "", string categoryFilter = "")
		{
			// Define the predefined category options
			var categoryOptions = new List<string> { "Mouse", "Keyboard", "Mousepad", "Printer" };
			
			// Prepare the query with includes
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand");

			// Apply search filter if provided
			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}

			// Apply category filter if it's valid
			if (!string.IsNullOrEmpty(categoryFilter) && categoryOptions.Any(c => c.ToLower() == categoryFilter.ToLower()))
			{
				categoryFilter = categoryFilter.Trim();
				
				// Filter based on category name - case insensitive comparison
				query = query.Where(p => p.Category.Name.ToLower().Contains(categoryFilter.ToLower()));
			}

			// Pagination setup
			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var filteredProducts = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Prepare view model
			var viewModel = new HomeIndexViewModel
			{
				SearchString = searchString,
				Category = categoryFilter,
				Products = filteredProducts,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
				CategoryOptions = categoryOptions
			};
			
			return View("GetAllProducts", viewModel); // Reuse the GetAllProducts view
		}

		public async Task<IActionResult> GetAllAccessories(int pageNumber = 1, string searchString = "")
		{
			// Get all products that belong to the Accessories category
			var query = _unitOfWork.ProductRepository.GetQueryable(p => p.Category.Name == "Accessories", includeProperties: "Category,Brand");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}
			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var accessoriesProducts = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var viewModel = new HomeIndexViewModel
			{
				SearchString = searchString,
				Accessories = accessoriesProducts, // Assign the paginated list to Accessories
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
			};
			return View(viewModel);
		}

		public async Task<IActionResult> GetAllLaptops(int pageNumber = 1, string searchString = "")
		{
			var query = _unitOfWork.LaptopRepository.GetQueryable(includeProperties: "Category,Brand");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(c => c.Brand.Name.ToLower().Contains(searchString) || c.Name.ToLower().Contains(searchString));
			}

			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var laptops = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var viewModel = new HomeIndexViewModel
			{
				SearchString = searchString,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
				Laptops = laptops,
			};
			return View(viewModel);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> GetProductReviews(int productId)
		{
			// Get reviews for the specific product
			var reviews = _unitOfWork.ReviewRepository.GetReviewsByProductId(productId);
			var averageRating = _unitOfWork.ReviewRepository.GetAverageRatingByProductId(productId);
			
			var result = new
			{
				Reviews = reviews,
				AverageRating = averageRating
			};
			
			return Json(result);
		}

		[AllowAnonymous]
		public IActionResult Error()
		{
			return View();
		}
	}
}