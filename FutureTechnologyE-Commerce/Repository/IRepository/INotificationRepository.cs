using FutureTechnologyE_Commerce.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId, int count = 5);
        Task<IEnumerable<Notification>> GetAllNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteOldNotificationsAsync(int daysOld = 90);
    }
} 