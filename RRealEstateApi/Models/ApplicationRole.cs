using Microsoft.AspNetCore.Identity;

namespace RRealEstateApi.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; } = string.Empty;
    }
}