namespace RRealEstateApi.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string BuyerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public Property Property { get; set; }
        public ApplicationUser Buyer { get; set; }
    }
}
