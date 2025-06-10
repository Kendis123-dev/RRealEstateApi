namespace RRealEstateApi.DTOs
{
    public class CreateTransactionDto
    {
        public int PropertyId { get; set; }
        public decimal Amount { get; set; }
    }
}
