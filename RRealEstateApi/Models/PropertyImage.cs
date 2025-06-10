namespace RRealEstateApi.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PropertyId  { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public Property Property { get; set; }
        public string FileType { get; set; }
        public byte[] ImageData { get; set; }
        public string? ImagePath { get; internal set; }
    }
}
