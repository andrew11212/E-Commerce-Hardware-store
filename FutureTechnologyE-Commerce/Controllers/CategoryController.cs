using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Logging; // Added for logging

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize(Roles = SD.Role_Admin)]
	public class CategoryController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CategoryController> _logger; // Added logger

		public CategoryController(IUnitOfWork unitOfWork, ILogger<CategoryController> logger) // Added logger to constructor
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public IActionResult Index()
		{
			try
			{
				var categories = _unitOfWork.CategoryRepository.GetAll().ToList();
				return View(categories);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching categories in Index action.");
				TempData["Error"] = "An error occurred while loading categories. Please try again."; // User-friendly error
				return View(new List<Category>()); // Return an empty list or redirect to an error page.  Important to return a view.
			}
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken] // Prevents cross-site request forgery
		public IActionResult Create(Category category)
		{
			try
			{
				if (ModelState.IsValid)
				{
					_unitOfWork.CategoryRepository.Add(category);
					_unitOfWork.Save();
					TempData["Success"] = "Category created successfully"; // Consistent message
					return RedirectToAction(nameof(Index));
				}
				return View(category); // Return view with model if validation fails
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while creating a category.");
				TempData["Error"] = "An error occurred while creating the category. Please try again.";
				return View(category); // Stay on the Create page and show the error
			}
		}

		[HttpGet]
		public IActionResult Edit(int? id) // Make id nullable
		{
			if (id == null || id <= 0)
			{
				_logger.LogWarning("Edit action called with invalid id: {Id}", id);
				return NotFound();
			}

			try
			{
				var category = _unitOfWork.CategoryRepository.Get(e => e.CategoryID == id);
				if (category == null)
				{
					_logger.LogWarning("Category with id {Id} not found.", id);
					return NotFound();
				}
				return View(category);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching category with id {Id} for editing.", id);
				TempData["Error"] = "An error occurred while retrieving the category. Please try again.";
				return RedirectToAction(nameof(Index)); // Redirect to index on error.
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(Category category)
		{
			try
			{
				if (ModelState.IsValid)
				{
					var categoryIndb = _unitOfWork.CategoryRepository.Get(c => c.CategoryID == category.CategoryID);
					if (categoryIndb != null)
					{
						categoryIndb.Name = category.Name;
						_unitOfWork.Save();
						TempData["Success"] = "Category updated successfully";
						return RedirectToAction(nameof(Index));
					}
					else
					{
						_logger.LogWarning("Category with id {Id} not found for updating.", category.CategoryID);
						return NotFound(); // Explicitly handle the case where the category doesn't exist
					}
				}
				return View(category);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while updating category with id {Id}.", category.CategoryID);
				TempData["Error"] = "An error occurred while updating the category. Please try again.";
				return View(category);
			}
		}

		[HttpGet]
		public IActionResult Delete(int? id) // Make id nullable
		{
			if (id == null || id <= 0)
			{
				_logger.LogWarning("Delete action called with invalid id: {Id}", id);
				return NotFound();
			}
			try
			{
				var category = _unitOfWork.CategoryRepository.Get(c => c.CategoryID == id);
				if (category == null)
				{
					_logger.LogWarning("Category with id {Id} not found for deletion.", id);
					return NotFound();
				}
				return View(category); // Return the confirmation view
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching category with id {Id} for deletion.", id);
				TempData["Error"] = "An error occurred while retrieving the category for deletion. Please try again.";
				return RedirectToAction(nameof(Index));
			}

		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteConfirmed(int? id) // Make id nullable
		{
			if (id == null || id <= 0)
			{
				_logger.LogWarning("DeleteConfirmed action called with invalid id: {Id}", id);
				return NotFound();
			}

			try
			{
				var categoryIndb = _unitOfWork.CategoryRepository.Get(c => c.CategoryID == id);
				if (categoryIndb != null)
				{
					_unitOfWork.CategoryRepository.Remove(categoryIndb);
					_unitOfWork.Save();
					TempData["Success"] = "Category deleted successfully";
					return RedirectToAction(nameof(Index));
				}
				else
				{
					_logger.LogWarning("Category with id {Id} not found for confirmed deletion.", id);
					return NotFound();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while deleting category with id {Id}.", id);
				TempData["Error"] = "An error occurred while deleting the category. Please try again.";
				return RedirectToAction(nameof(Index)); // Redirect, as the original view is gone.
			}
		}
	}
}

