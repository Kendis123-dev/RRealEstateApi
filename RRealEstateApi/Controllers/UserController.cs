using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.Models;

namespace RRealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // restrict to Admins only
    public class UserController : ControllerBase
    {
        private readonly RealEstateDbContext _context;

        public UserController(RealEstateDbContext context)
        {
            _context = context;
        }

        // GET: api/User/paginated?page=1&pageSize=10
        [HttpGet("paginated")]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and PageSize must be greater than 0.");

            var query = _context.Users.AsQueryable();

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await query
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                TotalPages = totalPages,
                Users = users
            });
        }
    }
}