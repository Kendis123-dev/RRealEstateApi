using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace RRealEstateApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        //public ApplicationUser()
        //{
        //    Id = Guid.NewGuid().ToString();
        //}
        //internal readonly object Id;

       // public string UserName { get; set; }//
        public string UserEmail { get; set; }
        //public string PhoneNumber { get; set; }
        public string FullName { get; set; }
       // public string? userID { get; set; }
        public int? AgentId { get; set; }
        public Agent Agent { get; set; }
        [NotMapped]
        public object Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
