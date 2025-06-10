using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;
using RRealEstateApi.Services;
using RRealEstateApi.Services.Implementations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RRealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IPhoneService _phoneService;

        private static readonly Dictionary<string, string> _login2FACodes = new();

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config,
            IEmailService emailService,
            IPhoneService phoneService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
            _emailService = emailService;
            _phoneService = phoneService;
        }

        [HttpPost("register-agent")]
        public async Task<IActionResult> RegisterAgent([FromBody] RegisterDto model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Agent already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                UserEmail = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Agent"))
                await _roleManager.CreateAsync(new IdentityRole("Agent"));

            await _userManager.AddToRoleAsync(user, "Agent");

            return Ok(new { message = "Agent registered successfully." });
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Admin already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                UserEmail = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            await _userManager.AddToRoleAsync(user, "Admin");

            return Ok(new { message = "Admin registered successfully." });
        }

        [HttpPost("register-User")]
        public async Task<IActionResult> Register(UserRegisterDto model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "User already exists." });

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                UserEmail = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login-User")]
        public async Task<IActionResult> Login(UserLoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized("Invalid login");

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateToken(user, roles);

            return Ok(new
            {
                token,
                expiration = DateTime.UtcNow.AddDays(2),
                roles
            });
        }

        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid email or password" });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
                return Unauthorized(new { message = "Access denied. Not an admin." });

            var token = GenerateToken(user, roles);
            return Ok(new
            {
                token,
                expiration = DateTime.UtcNow.AddDays(2),
                roles
            });
        }

        [HttpPost("login-agent")]
        public async Task<IActionResult> LoginAgent([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid email or password" });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Agent"))
                return Unauthorized(new { message = "Access denied. Not an agent." });

            var token = GenerateToken(user, roles);
            return Ok(new
            {
                token,
                expiration = DateTime.UtcNow.AddDays(2),
                roles
            });
        }

        [HttpPost("initiate-login")]
        public async Task<IActionResult> InitiateLogin([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            if (string.IsNullOrEmpty(user.PhoneNumber))
                return BadRequest(new { message = "Phone number not registered for 2FA." });

            var code = new Random().Next(100000, 999999).ToString();
            _login2FACodes[user.Email] = code;

            var smsMessage = $"Your 2FA code is: {code}";
            await _phoneService.SendSmsAsync(user.PhoneNumber, smsMessage);

            return Ok(new { message = "2FA code sent to your phone." });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] TwoFactorDto model)
        {
            if (!_login2FACodes.TryGetValue(model.Email, out var expectedCode) || expectedCode != model.Code)
                return Unauthorized(new { message = "Invalid or expired 2FA code." });

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateToken(user, roles);

            _login2FACodes.Remove(model.Email);

            return Ok(new
            {
                token,
                expiration = DateTime.UtcNow.AddDays(2),
                roles
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Web.HttpUtility.UrlEncode(token);
            var resetLink = $"{_config["Frontend:ResetPasswordUrl"]}?email={model.Email}&token={encodedToken}";

            var emailBody = $"<p>Hi {user.FullName},</p><p>Click the link below to reset your password:</p><p><a href='{resetLink}'>Reset Password</a></p>";

            await _emailService.SendEmailAsync(model.Email, "Password Reset Request", emailBody);

            return Ok(new { message = "Password reset link has been sent to your email." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Password reset failed.", errors = result.Errors });

            return Ok(new { message = "Password has been reset successfully." });
        }

        private string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(2),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}