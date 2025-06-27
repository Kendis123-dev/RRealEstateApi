using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;
using System.Security.Cryptography;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AgentController : ControllerBase
{
    private readonly RealEstateDbContext _context;

    public AgentController(RealEstateDbContext context)
    {
        _context = context;
    }

    private string GeneratePassword(string plainPassword)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(plainPassword);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    // GET: api/Agent?pageNumber=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Agent>>> GetAgents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var totalAgents = await _context.Agents.CountAsync();

        var agents = await _context.Agents
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            TotalCount = totalAgents,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = agents
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Agent>> GetAgent(int id)
    {
        var agent = await _context.Agents.FindAsync(id);
        if (agent == null) return NotFound();
        return agent;
    }

    //[HttpPost]
    //public async Task<ActionResult<Agent>> CreateAgent(AgentDto agent)
    //{
    //    var existing = await _context.Agents.FirstOrDefaultAsync(u => u.Email == agent.Email);
    //    if (existing != null) return BadRequest("Email already exists");

    //    var newAgent = new Agent
    //    {
    //        Id = agent.Id,
    //        FullName = agent.FullName,
    //        Email = agent.Email,
    //        AgencyName = agent.AgencyName,
    //        RegisteredAt = agent.RegisteredAt,
    //        PhoneNumber = agent.PhoneNumber
    //    };

    //    newAgent.Password = GeneratePassword(agent.Password);

    //    _context.Agents.Add(newAgent);
    //    await _context.SaveChangesAsync();

    //    return CreatedAtAction(nameof(GetAgent), new { id = newAgent.Id }, newAgent);
    //}

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAgent(int id, Agent agent)
    {
        if (id != agent.Id) return BadRequest();

        _context.Entry(agent).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAgent(int id)
    {
        var agent = await _context.Agents.FindAsync(id);
        if (agent == null) return NotFound();

        _context.Agents.Remove(agent);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{agentId}/properties")]
    [Authorize(Roles = "Agent")]
    public async Task<IActionResult> GetAgentProperties(int agentId)
    {
        if (agentId <= 0)
            return BadRequest(new { message = "Invalid Agent ID." });

        var properties = await _context.Properties
            .Where(p => p.AgentId == agentId)
            .ToListAsync();

        return Ok(properties);
    }

}