namespace RRealEstateApi.DTOs
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string PropertyType { get; set; }
        public string PropertyValue { get; set; } = string.Empty;
        public DateTime DatePosted { get; set; }
        public int? AgentId { get; set; }

    }
}
