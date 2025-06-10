using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.Models;
using RRealEstateApi.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace RRealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WatchlistController : ControllerBase
    {
        private readonly RealEstateDbContext _context;

        public WatchlistController(RealEstateDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToWatchlist(string email, [FromBody] AddToWatchlistDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var property = await _context.Properties.FindAsync(dto.PropertyId);
            if (property == null)
                return NotFound("Property not found");

            var alreadyExists = await _context.WatchlistItems
                .AnyAsync(w => w.UserId == user.Id && w.PropertyId == property.Id);

            if (alreadyExists)
                return BadRequest("Property already in watchlist");

            var watchlistItem = new WatchlistItem
            {
                UserId = user.Id,
                PropertyId = property.Id
            };

            _context.WatchlistItems.Add(watchlistItem);
            await _context.SaveChangesAsync();

            return Ok("Added to watchlist");
        }

        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleWatchlist(string email, [FromBody] AddToWatchlistDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound("User not found");

            var existing = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.PropertyId == dto.PropertyId);

            if (existing != null)
            {
                _context.WatchlistItems.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok("Removed from watchlist");
            }

            var newItem = new WatchlistItem
            {
                UserId = user.Id,
                PropertyId = dto.PropertyId
            };

            _context.WatchlistItems.Add(newItem);
            await _context.SaveChangesAsync();
            return Ok("Added to watchlist");
        }

        [HttpGet("analytics/most-watched")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MostWatched()
        {
            var result = await _context.WatchlistItems
                .GroupBy(w => w.PropertyId)
                .Select(g => new {
                    PropertyId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            return Ok(result);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromWatchlist(string email, int propertyId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var watchlistItem = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.PropertyId == propertyId);

            if (watchlistItem == null)
                return NotFound("Item not found in watchlist");

            _context.WatchlistItems.Remove(watchlistItem);
            await _context.SaveChangesAsync();

            return Ok("Removed from watchlist");
        }

        //  Paginated Watchlist for Logged-in User
        [HttpGet("my")]
        public async Task<IActionResult> GetMyWatchlist(string email, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var totalCount = await _context.WatchlistItems
                .Where(w => w.UserId == user.Id)
                .CountAsync();

            var pagedWatchlist = await _context.WatchlistItems
                .Where(w => w.UserId == user.Id)
                .Include(w => w.Property)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(w => w.Property)
                .ToListAsync();

            var response = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = pagedWatchlist
            };

            return Ok(response);
        }
    }
}