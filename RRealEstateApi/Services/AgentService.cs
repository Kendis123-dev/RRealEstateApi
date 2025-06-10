using Microsoft.EntityFrameworkCore;
using RRealEstateApi.Data;
using RRealEstateApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RRealEstateApi.Services
{
    public class AgentService 
    {
        private readonly RealEstateDbContext _context;

        public AgentService(RealEstateDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Agent>> GetAllAsync()
        {
            return await _context.Agents.ToListAsync();
        }

        public async Task<Agent> GetByIdAsync(int id)
        {
            return await _context.Agents.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Agent> CreateAsync(Agent agent)
        {
            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();
            return agent;
        }

        public async Task<bool> UpdateAsync(Agent updatedAgent)
        {
            var existingAgent = await _context.Agents.FindAsync(updatedAgent.Id);
            if (existingAgent == null) return false;

            existingAgent.FullName = updatedAgent.FullName;
            existingAgent.Email = updatedAgent.Email;
           // existingAgent.Phone = updatedAgent.Phone;//

            _context.Agents.Update(existingAgent);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent == null) return false;

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}