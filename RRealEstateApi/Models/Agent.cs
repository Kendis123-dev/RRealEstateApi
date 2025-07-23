using System.ComponentModel.DataAnnotations.Schema;

namespace RRealEstateApi.Models
{
    public class Agent
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        public string Aspuserid{get;set;}
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        //public string AgencyName { get; set; }
        public DateTime RegisteredAt { get; set; }
       // public string Password { get; set; }
        public bool IsVerified { get; set; } = false;
        //public object User { get; internal set; }
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public List<Property> Properties { get; set; }
        //  public bool IsVerified { get; set; } = true;
    }
}
