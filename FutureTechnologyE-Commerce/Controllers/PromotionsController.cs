using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromotionsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PromotionsController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            var promotionVM = new PromotionViewModel
            {
                Promotion = new(),
                ProductList = (await _unitOfWork.ProductRepository.GetAllAsync())
                    .Select(p => new SelectListItem(p.Name, p.ProductID.ToString()))
            };

            if (id == null || id == 0)
            {
                // Create new promotion
                return View(promotionVM);
            }
            else
            {
                // Update existing promotion
                promotionVM.Promotion = await _unitOfWork.PromotionRepository.GetAsync(p => p.PromotionId == id);
                if (promotionVM.Promotion == null)
                {
                    return NotFound();
                }
                return View(promotionVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(PromotionViewModel promotionVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string promotionPath = Path.Combine(wwwRootPath, "images", "promotions");

                    if (!Directory.Exists(promotionPath))
                    {
                        Directory.CreateDirectory(promotionPath);
                    }

                    if (!string.IsNullOrEmpty(promotionVM.Promotion.ImageUrl))
                    {
                        var oldImage = Path.Combine(wwwRootPath, promotionVM.Promotion.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImage))
                        {
                            System.IO.File.Delete(oldImage);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(promotionPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    promotionVM.Promotion.ImageUrl = $"/images/promotions/{fileName}";
                }

                if (promotionVM.Promotion.PromotionId == 0)
                {
                    await _unitOfWork.PromotionRepository.AddAsync(promotionVM.Promotion);
                    TempData["success"] = "Promotion created successfully";
                }
                else
                {
                    await _unitOfWork.PromotionRepository.UpdateAsync(promotionVM.Promotion);
                    TempData["success"] = "Promotion updated successfully";
                }

                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }

            // If we get here, something failed, redisplay form
            promotionVM.ProductList = (await _unitOfWork.ProductRepository.GetAllAsync())
                .Select(p => new SelectListItem(p.Name, p.ProductID.ToString()));
            return View(promotionVM);
        }

        #region API CALLS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var promotions = await _unitOfWork.PromotionRepository.GetAllAsync(includeProperties: "Product");
            return Json(new { data = promotions });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _unitOfWork.PromotionRepository.GetAsync(p => p.PromotionId == id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Promotion not found" });
            }

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, promotion.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            await _unitOfWork.PromotionRepository.RemoveAsync(promotion);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Promotion deleted successfully" });
        }
        #endregion
    }
} 