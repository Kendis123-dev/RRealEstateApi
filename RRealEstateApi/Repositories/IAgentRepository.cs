using RRealEstateApi.Models;

public interface IAgentRepository
{
    Task<List<Agent>> GetAllAsync();
    Task<Agent> GetByIdAsync(int id);
    Task<Agent> CreateAsync(Agent agent);
    Task UpdateAsync(Agent agent);
    Task DeleteAsync(int id);
}