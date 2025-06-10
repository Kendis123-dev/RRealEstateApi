namespace RRealEstateApi.DTOs
{
    public class UploadImageDto
    {
        public int PropertyId { get; set; }
        public IFormFile File { get; set; }
    }
}
