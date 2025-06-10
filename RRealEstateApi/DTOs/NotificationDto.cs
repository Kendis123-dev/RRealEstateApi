namespace RRealEstateApi.DTOs
{
    public class NotificationDto
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
