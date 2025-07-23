using System.ComponentModel.DataAnnotations.Schema;

namespace RRealEstateApi.Models
{
    public class Listing
    {
        public int Id { get; set; }
        //public int AgentId {get; set;}
        public int PropertyId { get; set; }
        //public Agent Agent { get; set;}
        //public string ListingType { get; set;}
        //public bool IsActive { get; set;}
        public DateTime ListedAt { get; set; }

        //[NotMapped]
        public Property Property { get;  set; }
        public string Title { get;  set; }
        public string Description { get;  set; }
        public decimal? Price { get;  set; }
        public bool IsAvailable { get;  set; }
    }
}
