using RRealEstateApi.Models;

namespace RRealEstateApi.Controllers
{
    public class WatchlistItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int PropertyId { get; set; }
       // public string? Note { get;set; }
        public Property Property { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
      //  public DateTime DateAdded { get; set;} = DateTime.UtcNow;

    }
}
