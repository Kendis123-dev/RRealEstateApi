namespace RRealEstateApi.DTOs
{
    public class AgentDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string AgencyName { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string Password { get; set; }
        public bool IsVerified { get; set; }
    }
}
