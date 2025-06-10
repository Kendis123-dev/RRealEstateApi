using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Threading.Tasks;
using RRealEstateApi.Models;
using System.Collections.Generic;
using RRealEstateApi.Controllers;

namespace RRealEstateApi.Repositories
{
    public interface IWatchlistRepository
    {
        Task<IEnumerable<WatchlistItem>> GetUserWatchlistAsync(string userId);
        Task<WatchlistItem> GetWatchlistItemAsync(string userId, int PropertyId);
        Task AddToWatchlistAsync(string id, WatchlistItem item);
        Task RemoveFromWatchlist(WatchlistItem item);
        Task<bool> RemoveFromWatchlist(string id, int propertyId);
        Task<bool> AddToWatchlistAsync(string id, int propertyId);
    }
}
