using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FutureTechnologyE_Commerce.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET: Review/GetReviews/5
        public IActionResult GetReviews(int productId)
        {
            var reviews = _unitOfWork.ReviewRepository.GetReviewsByProductId(productId);
            var averageRating = _unitOfWork.ReviewRepository.GetAverageRatingByProductId(productId);
            
            var model = new
            {
                Reviews = reviews,
                AverageRating = averageRating
            };
            
            return Json(model);
        }

        // GET: Review/Create/5
        [Authorize]
        public IActionResult Create(int productId)
        {
            var product = _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == productId).Result;
            if (product == null)
            {
                return NotFound();
            }

            var review = new Review
            {
                ProductID = productId,
                UserID = User.FindFirstValue(ClaimTypes.NameIdentifier),
                ReviewDate = DateTime.Now
            };

            return View(review);
        }

        // POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Review review)
        {
            if (ModelState.IsValid)
            {
                // Set the current date and user ID
                review.ReviewDate = DateTime.Now;
                review.UserID = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Check if the user has already reviewed this product
                var existingReview = await _unitOfWork.ReviewRepository.GetAsync(
                    r => r.ProductID == review.ProductID && r.UserID == review.UserID);

                if (existingReview != null)
                {
                    // Update existing review
                    existingReview.Rating = review.Rating;
                    existingReview.Comment = review.Comment;
                    existingReview.ReviewDate = review.ReviewDate;
                    _unitOfWork.ReviewRepository.Update(existingReview);
                }
                else
                {
                    // Add new review
                    await _unitOfWork.ReviewRepository.AddAsync(review);
                }

                await _unitOfWork.SaveAsync();
                return RedirectToAction("Details", "Product", new { id = review.ProductID });
            }
            
            return View(review);
        }

        // GET: Review/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _unitOfWork.ReviewRepository.GetAsync(r => r.ReviewID == id);
            if (review == null)
            {
                return NotFound();
            }

            // Ensure the user is the owner of the review
            if (review.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Review review)
        {
            if (id != review.ReviewID)
            {
                return NotFound();
            }

            var existingReview = await _unitOfWork.ReviewRepository.GetAsync(r => r.ReviewID == id);
            if (existingReview == null)
            {
                return NotFound();
            }

            // Ensure the user is the owner of the review
            if (existingReview.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                existingReview.Rating = review.Rating;
                existingReview.Comment = review.Comment;
                existingReview.ReviewDate = DateTime.Now;
                
                _unitOfWork.ReviewRepository.Update(existingReview);
                await _unitOfWork.SaveAsync();
                
                return RedirectToAction("Details", "Product", new { id = existingReview.ProductID });
            }
            
            return View(review);
        }

        // POST: Review/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _unitOfWork.ReviewRepository.GetAsync(r => r.ReviewID == id);
            if (review == null)
            {
                return NotFound();
            }

            // Ensure the user is the owner of the review or an admin
            if (review.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            int productId = review.ProductID;
            await _unitOfWork.ReviewRepository.RemoveAsync(review);
            await _unitOfWork.SaveAsync();
            
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
} 