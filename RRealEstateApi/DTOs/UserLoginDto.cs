using System.ComponentModel.DataAnnotations;

namespace RRealEstateApi.DTOs
{
    public class UserLoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
    public class LoginDto
    {
        //[Required(ErrorMessage = "Username is required.")]
        //[EmailAddress(ErrorMessage = "Invalid email address.")]
       // [StringLength(60, ErrorMessage = "Username cannot exceed 60 characters.")]
       // public required string Username { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(20, ErrorMessage = "Username cannot exceed 20 characters.")]
        public string Password { get; set; }
        public string Email { get; set; }
    }
    public class GetUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }

    }
}
