using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using RRealEstateApi.DTOs;
using RRealEstateApi.Data;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly RealEstateDbContext _context;

    public AdminController(RealEstateDbContext context)
    {
        _context = context;
    }
    //[Authorize(Roles = "Admin")]
    [HttpGet("test-admin")]
    [Authorize]
    public IActionResult Testadmin()
    {
        var tokenGetter = Request.Headers.Authorization.Count;
        var tokenGetter2 = Request.Headers.Authorization.FirstOrDefault();
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        if (identity == null || !identity.Claims.Any())
                {
            return BadRequest("No claims found. Are you sure the token is attached and valid? ");
        }
        
        var claims = identity?.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(claims);
    }


    
    [HttpPut("verify-agent/{agentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifyAgent(int agentId, [FromBody] VerifyAgentDto dto)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null)
            return NotFound("Agent not found");

        agent.IsVerified = dto.IsVerified;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Agent {(dto.IsVerified ? "verified" : "unverified")} successfully." });
    }
}