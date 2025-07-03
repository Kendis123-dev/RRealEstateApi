namespace RRealEstateApi.Models
{
    public class Message
    {

        public int Id { get; set; }
        public Property Property { get; set; }
        public string UserId { get; set; }
        public string RecieverId { get; set; }
        public string RecieverEmail { get; set; }
        public int PropertyId { get; set; }
        public ApplicationUser User { get; set; }
        public string content {  get; set; }
        public DateTime DateSent { get; set; }
        public int Agent { get;  set; }
    }
}
