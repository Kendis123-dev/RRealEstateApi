using RRealEstateApi.Data;
using RRealEstateApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RRealEstateApi.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly RealEstateDbContext _context;

        public NotificationService(RealEstateDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        Task<List<NotificationsController>> INotificationService.GetUserNotificationsAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
