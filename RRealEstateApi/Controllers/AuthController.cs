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
using System.Text.RegularExpressions;

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

        // REGISTRATION

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
        {
            if (!IsValidEmail(model.Email))
                return BadRequest(new { message = "Invalid email format" });

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Admin already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            await _userManager.AddToRoleAsync(user, "Admin");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{_config["Frontend:ConfirmEmailUrl"]}?email={user.Email}&token={token}";
            await _emailService.SendEmailAsync(user.Email, "Confirm Your Email", $"<p>Click to confirm: <a href='{confirmationLink}'>Verify Email</a></p>");

            return Ok(new { message = "Admin registered. Please check your email to confirm." });
        }

        [HttpPost("register-user")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterDto model)
        {
            if (!IsValidEmail(model.Email))
                return BadRequest(new { message = "Invalid email format" });

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "User already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{_config["Frontend:ConfirmEmailUrl"]}?email={user.Email}&token={token}";
            await _emailService.SendEmailAsync(user.Email, "Confirm Your Email", $"<p>Click to confirm: <a href='{confirmationLink}'>Verify Email</a></p>");

            return Ok(new { message = "User registered. Please check your email to confirm." });
        }

        // LOGIN INITIATE + 2FA

        [HttpPost("initiate-login")]
        public async Task<IActionResult> InitiateLogin([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            if (!user.EmailConfirmed)
                return Unauthorized(new { message = "Please confirm your email before logging in." });

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                return BadRequest(new { message = "Phone number is not registered." });

            var code = new Random().Next(100000, 999999).ToString();
            _login2FACodes[user.Email] = code;

            await _phoneService.SendSmsAsync(user.PhoneNumber, $"Your 2FA code is: {code}");

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

        // EMAIL CONFIRMATION

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!result.Succeeded)
                return BadRequest(new { message = "Email confirmation failed.", errors = result.Errors });

            return Ok(new { message = "Email confirmed successfully." });
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] EmailDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            if (user.EmailConfirmed)
                return BadRequest(new { message = "Email already confirmed." });

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{_config["Frontend:ConfirmEmailUrl"]}?email={user.Email}&token={token}";
            await _emailService.SendEmailAsync(user.Email, "Resend Email Confirmation", $"<p>Click to confirm: <a href='{confirmationLink}'>Verify Email</a></p>");

            return Ok(new { message = "Confirmation email resent." });
        }

        // PASSWORD RESET

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Web.HttpUtility.UrlEncode(token);
            var resetLink = $"{_config["Frontend:ResetPasswordUrl"]}?email={model.Email}&token={encodedToken}";

            await _emailService.SendEmailAsync(model.Email, "Password Reset", $"<p>Reset your password: <a href='{resetLink}'>Reset</a></p>");

            return Ok(new { message = "Password reset link sent." });
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

            return Ok(new { message = "Password reset successfully." });
        }

        // TOKEN GENERATION

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
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }
}