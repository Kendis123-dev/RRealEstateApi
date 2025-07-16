using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace RRealEstateApi.Models
{
    public class ApplicationUser : IdentityUser
    {
       
        public string UserEmail { get; set; }
        public bool IsDisabled { get; set; } = false;
        public string FullName { get; set; }
        public string ProfilePictureUrl { get; set; } = "{}";
        public string KnownDevicesJson { get; set; } = "{}";
        public int? AgentId { get; set; }

        public Agent Agent { get; set; }
        [NotMapped]
        public object Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
