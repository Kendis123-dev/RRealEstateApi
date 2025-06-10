using RRealEstateApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RRealEstateApi.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string message);
        Task<List<NotificationsController>> GetUserNotificationsAsync(string userId);
    }
}
