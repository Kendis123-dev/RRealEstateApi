namespace RRealEstateApi.Models;

public class Property
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public string PropertyType { get; set; }
    public string PropertyValue { get; set; } = string.Empty;
    public DateTime DatePosted { get; set; }
    public int ListingId { get; set; }
    public  int? AgentId { get; set; }
    public Agent Agent { get; set; }
    public string State { get; set; }
    // public Listing Listing { get; set; }
}
