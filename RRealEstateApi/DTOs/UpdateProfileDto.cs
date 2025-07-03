namespace RRealEstateApi.DTOs
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; }
        public IFormFile ProfilePicture { get; set; }
    }
}
