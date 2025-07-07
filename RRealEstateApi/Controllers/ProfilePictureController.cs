using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RRealEstateApi.Models;

namespace RRealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilePictureController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfilePictureController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        //  Upload or Update Profile Picture
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
                return BadRequest(new { message = "No image uploaded." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(profilePicture.FileName).ToLower();

            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { message = "Invalid file type." });

            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profilepics");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            //  Delete old profile picture from server
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var oldPath = Path.Combine(_env.WebRootPath ?? "wwwroot", user.ProfilePictureUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            user.ProfilePictureUrl = $"/uploads/profilepics/{fileName}";
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile picture uploaded.", profilePictureUrl = user.ProfilePictureUrl });
        }

        //  View Profile Picture URL
        [HttpGet("view")]
        [Authorize]
        public async Task<IActionResult> ViewProfilePicture()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            return Ok(new
            {
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        // Delete Profile Picture
        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                return BadRequest(new { message = "No profile picture to delete." });

            var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", user.ProfilePictureUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile picture deleted." });
        }
    }
}