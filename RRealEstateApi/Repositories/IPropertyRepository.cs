using RRealEstateApi.Models;
namespace RRealEstateApi.Repositories
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<Property>> GetPropertiesByLocationAsync(string location);
    }
}
