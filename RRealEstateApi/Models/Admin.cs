namespace RRealEstateApi.Models
{
    public class Admin
    {
        public string name { get; set; }
        public int Id { get; set; }
        public string Email { get; set; }
        public string Phonenumber { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string Password { get; set; }
    }
}
