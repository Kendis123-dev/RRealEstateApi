using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly RealEstateDbContext _context;

    public MessagesController(RealEstateDbContext context)
    {
        _context = context;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(string email, [FromBody] SendMessageDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        var property = await _context.Properties.Include(p => p.Agent)
                                                .FirstOrDefaultAsync(p => p.Id == dto.PropertyId);

        if (property == null) return NotFound("Property not found");

        var message = new Message
        {
            UserId = user.Id,
            PropertyId = property.Id,
            content = dto.Content,
            Agent= property.Agent.Id ?? 0, // Assuming Agent is an int ID

        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return Ok("Message sent successfully");
    }

    [HttpGet("received")]
    public async Task<IActionResult> GetAgentMessages(string email, int pageNumber = 1, int pageSize = 10)
    {
        var agent = await _context.Users.FirstOrDefaultAsync(a => a.Email == email);
        if (agent == null) return NotFound("Agent not found");

        var messagesQuery = _context.Messages
            .Where(m => m.RecieverEmail == agent.Email)
            .Include(m => m.User)
            .OrderByDescending(m => m.DateSent); 

        var totalCount = await messagesQuery.CountAsync();
        var messages = await messagesQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            totalCount,
            pageNumber,
            pageSize,
            data = messages
        });
    }

    [HttpGet("sent")]
    public async Task<IActionResult> GetUserMessages(string email, int pageNumber = 1, int pageSize = 10)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        var messagesQuery = _context.Messages
            .Where(m => m.UserId == user.Id)
            .Include(m => m.Property)
            .OrderByDescending(m => m.DateSent); // Optional: order by latest

        var totalCount = await messagesQuery.CountAsync();
        var messages = await messagesQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = messages
        });
    }
}