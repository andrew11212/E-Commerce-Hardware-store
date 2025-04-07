using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Services;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                const int pageSize = 10;

                // Get notifications for the current user with pagination
                var notifications = await _unitOfWork.NotificationRepository.GetAllNotificationsForUserAsync(userId, page, pageSize);

                // Calculate total for pagination
                var totalNotifications = await _unitOfWork.NotificationRepository.GetAllAsync(n => n.UserId == userId);
                var totalCount = totalNotifications.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return View(new List<Notification>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get the notification to verify ownership
                var notification = await _unitOfWork.NotificationRepository.GetAsync(n => n.Id == id);
                
                if (notification == null)
                {
                    return NotFound();
                }

                // Verify that this notification belongs to the current user
                if (notification.UserId != userId)
                {
                    return Forbid();
                }

                // Mark as read
                await _unitOfWork.NotificationRepository.MarkAsReadAsync(id);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return Json(new { success = false, message = "Error marking notification as read" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Mark all notifications as read for this user
                await _unitOfWork.NotificationRepository.MarkAllAsReadAsync(userId);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "Error marking all notifications as read" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get unread notification count
                int count = await _unitOfWork.NotificationRepository.GetUnreadCountAsync(userId);
                
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count");
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsPartial()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get a few recent unread notifications to display in the dropdown
                var notifications = await _unitOfWork.NotificationRepository.GetUnreadNotificationsAsync(userId, 5);
                
                return PartialView("_NotificationsPartial", notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications partial");
                return PartialView("_NotificationsPartial", new List<Notification>());
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _unitOfWork.applciationUserRepository.GetAsync(u => u.Id == userId);
                
                if (user == null)
                {
                    return NotFound();
                }

                // Create a test notification
                Notification notification = new Notification
                {
                    Title = "Test Notification",
                    Message = "This is a test notification from the system.",
                    Type = SD.Notification_Type_System,
                    UserId = userId,
                    IconClass = SD.Notification_Icon_System,
                    Priority = SD.Notification_Priority_Medium,
                    ActionUrl = "/Home/Index"
                };

                await _unitOfWork.NotificationRepository.AddAsync(notification);
                await _unitOfWork.SaveAsync();
                
                // Send email/SMS test notification
                await _notificationService.SendPromotionAsync(
                    user.Email,
                    user.PhoneNumber,
                    $"{user.first_name} {user.last_name}",
                    "Test Promotion",
                    "This is a test promotion message from the system."
                );
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification");
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 