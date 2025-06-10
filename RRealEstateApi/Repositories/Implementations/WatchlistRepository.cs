using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Controllers;
using RRealEstateApi.Data;
using RRealEstateApi.Models;
using RRealEstateApi.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RRealEstateApi.Repositories.Implementations
{
    public class WatchlistRepository : IWatchlistRepository
    {
        private readonly RealEstateDbContext _context;

        public WatchlistRepository(RealEstateDbContext context)
        {
            _context = context;
        }

        // 1. Get all properties in user's watchlist
        public async Task<IEnumerable<WatchlistItem>> GetUserWatchlistAsync(string userId)
        {
            return await _context.WatchlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Property)
                .ToListAsync();
        }

        // 2. Get a specific watchlist item
        public async Task<WatchlistItem> GetWatchlistItemAsync(string userId, int propertyId)
        {
            return await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.PropertyId == propertyId);
        }

        // 3. Add a watchlist item using a WatchlistItem object
        public async Task AddToWatchlistAsync(string userId, WatchlistItem item)
        {
            item.UserId = userId;
            _context.WatchlistItems.Add(item);
            await _context.SaveChangesAsync();
        }

        // 4. Remove a watchlist item using a WatchlistItem object
        public async Task RemoveFromWatchlist(WatchlistItem item)
        {
            _context.WatchlistItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        // 5. Add a watchlist item using just userId and propertyId
        public async Task<bool> AddToWatchlistAsync(string userId, int propertyId)
        {
            bool exists = await _context.WatchlistItems
                .AnyAsync(w => w.UserId == userId && w.PropertyId == propertyId);

            if (exists) return false;

            var watchlistItem = new WatchlistItem
            {
                UserId = userId,
                PropertyId = propertyId
            };

            _context.WatchlistItems.Add(watchlistItem);
            return await _context.SaveChangesAsync() > 0;
        }

        // 6. Remove a watchlist item using just userId and propertyId
        public async Task<bool> RemoveFromWatchlist(string userId, int propertyId)
        {
            var item = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.PropertyId == propertyId);

            if (item == null) return false;

            _context.WatchlistItems.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}