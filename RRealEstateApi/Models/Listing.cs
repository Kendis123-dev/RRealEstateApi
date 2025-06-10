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
        public Property Property { get; internal set; }
        public string Title { get; internal set; }
        public string Description { get; internal set; }
        public decimal Price { get; internal set; }
        public bool IsAvailable { get; internal set; }
    }
}
