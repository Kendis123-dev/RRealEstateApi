namespace RRealEstateApi.Models
{
    public class LoginActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime LoginTime { get; set; } = DateTime.Now;

    }
}
