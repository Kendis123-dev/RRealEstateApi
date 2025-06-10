using RRealEstateApi.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace RRealEstateApi.Services
{
    public interface IPropertyService
    {
        Task<IEnumerable<PropertyDto>> SearchByLocationAsync(string location);
    }
}
