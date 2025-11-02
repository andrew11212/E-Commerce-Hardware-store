using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Services;
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
		private readonly ICacheService _cacheService;

		public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ICacheService cacheService)
		{
			_logger = logger;
			_unitOfWork = unitOfWork;
			_cacheService = cacheService;
		}

		public async Task<IActionResult> Index(string searchString = "")
		{
			// If search is provided, skip cache
			if (!string.IsNullOrEmpty(searchString))
			{
				var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand");
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));

				var topReviews = await _unitOfWork.ReviewRepository
					.GetAllAsync(r => r.Rating >= 4, includeProperties: "User,Product");

				var activePromotions = _unitOfWork.PromotionRepository.GetActivePromotions();

				var viewModel = new HomeIndexViewModel
				{
					Products = (await _unitOfWork.ProductRepository.GetAllAsync(c => c.IsBestseller, includeProperties: "Category,Brand")),
					SearchString = searchString,
					Accessories = (await _unitOfWork.ProductRepository.GetAllAsync(c => 
						c.Category.Name.ToLower() == "mouse" || 
						c.Category.Name.ToLower() == "keyboard" || 
						c.Category.Name.ToLower() == "mousepad" || 
						c.Category.Name.ToLower() == "storage" ||
						c.Category.Name.ToLower() == "notebook" ||
						c.Category.Name.ToLower() == "accessories", 
						includeProperties: "Category,Brand")),
					Laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand"))
						.Take(5)
						.ToList(),
					TopReviews = topReviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.ReviewDate).Take(3).ToList(),
					Promotions = activePromotions
				};

				return View(viewModel);
			}

			// Try to get from cache
			var cacheKey = "home_index_data";
			var cachedViewModel = await _cacheService.GetAsync<HomeIndexViewModel>(cacheKey);

			if (cachedViewModel != null)
			{
				_logger.LogInformation("Returning cached home page data");
				return View(cachedViewModel);
			}

			// Cache miss - fetch from database
			_logger.LogInformation("Cache miss - fetching home page data from database");

			var bestsellers = await _unitOfWork.ProductRepository.GetAllAsync(
				c => c.IsBestseller, 
				includeProperties: "Category,Brand");

			var accessories = await _unitOfWork.ProductRepository.GetAllAsync(c => 
				c.Category.Name.ToLower() == "mouse" || 
				c.Category.Name.ToLower() == "keyboard" || 
				c.Category.Name.ToLower() == "mousepad" || 
				c.Category.Name.ToLower() == "storage" ||
				c.Category.Name.ToLower() == "notebook" ||
				c.Category.Name.ToLower() == "accessories", 
				includeProperties: "Category,Brand");

			var laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand"))
				.Take(5)
				.ToList();

			var topReviewsData = await _unitOfWork.ReviewRepository
				.GetAllAsync(r => r.Rating >= 4, includeProperties: "User,Product");

			var promotions = _unitOfWork.PromotionRepository.GetActivePromotions();

			var homeViewModel = new HomeIndexViewModel
			{
				Products = bestsellers,
				SearchString = searchString,
				Accessories = accessories,
				Laptops = laptops,
				TopReviews = topReviewsData.OrderByDescending(r => r.Rating).ThenByDescending(r => r.ReviewDate).Take(3).ToList(),
				Promotions = promotions
			};

			// Cache for 10 minutes
			await _cacheService.SetAsync(cacheKey, homeViewModel, TimeSpan.FromMinutes(10));

			return View(homeViewModel);
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

			int pageSize = 9;
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
			var categoryOptions = new List<string> { 
				"Mouse", "Keyboard", "Mousepad", "Printer", "Storage", "Notebook", 
				"Headphones", "Webcam", "Accessories"
			};
			
			// Prepare the query with includes
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand");

			// Apply search filter if provided
			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}

			// Apply category filter if provided
			if (!string.IsNullOrEmpty(categoryFilter))
			{
				categoryFilter = categoryFilter.Trim();
				
				// Special case for "Bestseller" filter
				if (categoryFilter.ToLower() == "bestseller")
				{
					query = query.Where(p => p.IsBestseller);
				}
				// Special case for "Accessories" filter - get all accessory categories
				else if (categoryFilter.ToLower() == "accessories")
				{
					query = query.Where(p => 
						p.Category.Name.ToLower() == "mouse" || 
						p.Category.Name.ToLower() == "keyboard" || 
						p.Category.Name.ToLower() == "headphones" || 
						p.Category.Name.ToLower() == "webcam" ||
						p.Category.Name.ToLower() == "accessories"
					);
				}
				// Regular category filter
				else
				{
					// Filter based on category name - case insensitive comparison
					query = query.Where(p => p.Category.Name.ToLower().Contains(categoryFilter.ToLower()));
				}
			}

			// Pagination setup
			int pageSize = 9;
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
			var query = _unitOfWork.ProductRepository.GetQueryable(p => p.Category.ParentCategory.Name == "Accessories", includeProperties: "Category,Brand");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}
			int pageSize = 9;
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

			int pageSize = 9;
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