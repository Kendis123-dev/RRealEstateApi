using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly RealEstateDbContext _context;

    public NotificationsController(RealEstateDbContext context)
    {
        _context = context;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return NotFound("User not found");

        var notification = new Notification
        {
            UserId = dto.UserId,
            Message = dto.Message
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return Ok("Notification created");
    }

    //  Paginated Notifications for a Specific User
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var totalCount = await _context.Notifications
            .Where(n => n.UserId == userId)
            .CountAsync();

        var pagedNotifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = pagedNotifications
        };

        return Ok(response);
    }

    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        if (notification == null) return NotFound("Notification not found");

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok("Marked as read");
    }
}