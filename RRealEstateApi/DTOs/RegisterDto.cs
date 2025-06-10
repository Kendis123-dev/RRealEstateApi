using System.ComponentModel.DataAnnotations;

namespace RRealEstateApi.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string FullName { get; set; }  


        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string PhoneNumber { get; set; }
    }
}
