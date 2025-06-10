using RRealEstateApi.DTOs;
using RRealEstateApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RRealEstateApi.Services;
public interface IAgentService
{
    Task<IEnumerable<Agent>> GetAllAgentsAsync();
    Task<Agent> GetByIdAsync(int id);
    Task<Agent> CreateAsync(Agent agent);
    Task <bool> UpdateAsync( Agent agent);
    Task <bool> DeleteAsync(int id);
    Task CreateAsync(AgentDto agentDto);
    //defines wht the services will do
}