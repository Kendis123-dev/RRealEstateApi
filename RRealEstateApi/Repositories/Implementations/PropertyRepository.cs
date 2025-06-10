using Microsoft.EntityFrameworkCore;
using OpenXmlPowerTools;
using RRealEstateApi.Data;
using RRealEstateApi.Models;
using RRealEstateApi.Repositories;

namespace RRealEstateApi.Repositories.Implementations
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly RealEstateDbContext _context;

        public PropertyRepository(RealEstateDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Property>> GetPropertiesByLocationAsync(string location)
        {
            return await _context.Properties
                .Where(p => p.Location.ToLower().Contains(location.ToLower())||  p.State.ToLower().Contains(location.ToLower()))
                .ToListAsync();
        }
    }
}