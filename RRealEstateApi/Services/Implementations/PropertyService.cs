using RRealEstateApi.DTOs;
using RRealEstateApi.Repositories;

namespace RRealEstateApi.Services.Implementations
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _propertyRepository;

        public PropertyService(IPropertyRepository propertyRepository)
        {
            _propertyRepository = propertyRepository;
        }

        public async Task<IEnumerable<PropertyDto>> SearchByLocationAsync(string location)
        {
            if (_propertyRepository == null)
                throw new Exception("PropertRepository is null");
            var properties = await _propertyRepository.GetPropertiesByLocationAsync(location);
            if (properties == null)
                throw new Exception("No properties were returned from the repository");

            return properties.Select(p => new PropertyDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Location = p.Location,
                Price = p.Price,
                
            });
        }
    }
}