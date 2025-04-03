using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectList
using System.Collections.Generic;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize(Roles = SD.Role_Admin)]
	public class CategoryController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CategoryController> _logger;
		private readonly List<string> _predefinedCategories = new List<string> { "Mouse", "Keyboard", "Mousepad", "Printer" };

		public CategoryController(IUnitOfWork unitOfWork, ILogger<CategoryController> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<IActionResult> Index()
		{
			try
			{
				var categories = (await _unitOfWork.CategoryRepository.GetAllAsync()).ToList();
				return View(categories);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching categories in Index action.");
				TempData["Error"] = "An error occurred while loading categories. Please try again.";
				return View(new List<Category>());
			}
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			try
			{
				var parentCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
				ViewBag.ParentCategoryID = new SelectList(parentCategories, "CategoryID", "Name");
				
				// Add predefined category suggestions
				ViewBag.SuggestedCategories = new SelectList(_predefinedCategories);
				
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while preparing Create view.");
				TempData["Error"] = "An error occurred while preparing the create form. Please try again.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Category category)
		{
			try
			{
				if (ModelState.IsValid)
				{
					await _unitOfWork.CategoryRepository.AddAsync(category);
					await _unitOfWork.SaveAsync();
					TempData["Success"] = "Category created successfully";
					return RedirectToAction(nameof(Index));
				}
				var parentCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
				ViewBag.ParentCategoryID = new SelectList(parentCategories, "CategoryID", "Name");
				
				// Add predefined category suggestions
				ViewBag.SuggestedCategories = new SelectList(_predefinedCategories);
				
				return View(category);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while creating a category.");
				TempData["Error"] = "An error occurred while creating the category. Please try again.";
				var parentCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
				ViewBag.ParentCategoryID = new SelectList(parentCategories, "CategoryID", "Name");
				
				// Add predefined category suggestions
				ViewBag.SuggestedCategories = new SelectList(_predefinedCategories);
				
				return View(category);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null || id <= 0)
			{
				_logger.LogWarning("Edit action called with invalid id: {Id}", id);
				return NotFound();
			}

			try
			{
				var category = await _unitOfWork.CategoryRepository.GetAsync(e => e.CategoryID == id);
				if (category == null)
				{
					_logger.LogWarning("Category with id {Id} not found.", id);
					return NotFound();
				}

				var parentCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
				ViewBag.ParentCategoryID = new SelectList(parentCategories, "CategoryID", "Name", category.ParentCategoryID);
				
				// Add predefined category suggestions
				ViewBag.SuggestedCategories = new SelectList(_predefinedCategories);

				return View(category);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while fetching category with id {Id} for editing.", id);
				TempData["Error"] = "An error occurred while retrieving the category. Please try again.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Category category)
		{
			try
			{
				if (ModelState.IsValid)
				{
					await _unitOfWork.CategoryRepository.UpdateAsync(category);
					await _unitOfWork.SaveAsync();
					TempData["Success"] = "Category updated successfully";
					return RedirectToAction(nameof(Index));
				}
				var parentCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
				ViewBag.ParentCategoryID = new SelectList(parentCategories, "CategoryID", "Name", category.ParentCategoryID);
				
				// Add predefined category suggestions
				ViewBag.SuggestedCategories = new SelectList(_predefinedCategories);
				
				return View(category);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while updating category with id {Id}.", category.CategoryID);
				TempData["Error"] = "An error occurred while updating the category. Please try again.";
				var parentCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
				ViewBag.ParentCategoryID = new SelectList(parentCategories, "CategoryID", "Name", category.ParentCategoryID);
				
				// Add predefined category suggestions
				ViewBag.SuggestedCategories = new SelectList(_predefinedCategories);
				
				return View(category);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null || id <= 0)
			{
				_logger.LogWarning("Delete action called with invalid id: {Id}", id);
				return NotFound();
			}
			try
			{
				var category = await _unitOfWork.CategoryRepository.GetAsync(c => c.CategoryID == id);
				if (category == null)
				{
					_logger.LogWarning("Category with id {Id} not found for deletion.", id);
					return NotFound();
				}
				return View(category);
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
		public async Task<IActionResult> DeleteConfirmed(int? id)
		{
			if (id == null || id <= 0)
			{
				_logger.LogWarning("DeleteConfirmed action called with invalid id: {Id}", id);
				return NotFound();
			}

			try
			{
				var categoryIndb = await _unitOfWork.CategoryRepository.GetAsync(c => c.CategoryID == id);
				if (categoryIndb != null)
				{
					await _unitOfWork.CategoryRepository.RemoveAsync(categoryIndb);
					await _unitOfWork.SaveAsync();
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
				return RedirectToAction(nameof(Index));
			}
		}
	}
}