using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.Models;

public class AgentRepository : IAgentRepository
{
    private readonly RealEstateDbContext _context;

    public AgentRepository(RealEstateDbContext context)
    {
        _context = context;
    }

    public async Task<List<Agent>> GetAllAsync() => await _context.Agents.ToListAsync();

    public async Task<Agent> GetByIdAsync(int id) => await _context.Agents.FindAsync(id);

    public async Task<Agent> CreateAsync(Agent agent)
    {
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task UpdateAsync(Agent agent)
    {
        _context.Entry(agent).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var agent = await _context.Agents.FindAsync(id);
        if (agent != null)
        {
            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
        }
    }
}