using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;
using RRealEstateApi.Services;
using RRealEstateApi.Services.Implementations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

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

        // Registrtion

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
        {
            if (!IsValidEmail(model.Email)) return BadRequest(new { message = "Invalid email format" });

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Admin already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                UserEmail = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            await _userManager.AddToRoleAsync(user, "Admin");

            await SendEmailConfirmationLink(user);
            return Ok(new { message = "Admin registered. Please confirm your email." });
        }

        [HttpPost("register-user")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterDto model)
        {
            if (!IsValidEmail(model.Email)) return BadRequest(new { message = "Invalid email format" });

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "User already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                UserEmail = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User");

            await SendEmailConfirmationLink(user);
            return Ok(new { message = "User registered. Please confirm your email." });
        }

        [HttpPost("register-agent")]
        public async Task<IActionResult> RegisterAgent([FromBody] RegisterDto model)
        {
            if (!IsValidEmail(model.Email)) return BadRequest(new { message = "Invalid email format" });

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Agent already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                UserEmail = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Agent"))
                await _roleManager.CreateAsync(new IdentityRole("Agent"));

            await _userManager.AddToRoleAsync(user, "Agent");

            await SendEmailConfirmationLink(user);
            return Ok(new { message = "Agent registered. Please confirm your email." });
        }

        //LOGIN INITIATE 

        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid login." });

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                return Unauthorized(new { message = "Not an admin." });

            return await Begin2FA(user);
        }

        [HttpPost("login-user")]
        public async Task<IActionResult> LoginUser([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid login." });

            if (!await _userManager.IsInRoleAsync(user, "User"))
                return Unauthorized(new { message = "Not a user." });

            return await Begin2FA(user);
        }

        [HttpPost("login-agent")]
        public async Task<IActionResult> LoginAgent([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid login." });

            if (!await _userManager.IsInRoleAsync(user, "Agent"))
                return Unauthorized(new { message = "Not an agent." });

            return await Begin2FA(user);
        }

        private async Task<IActionResult> Begin2FA(ApplicationUser user)
        {
            if (!user.EmailConfirmed)
                return Unauthorized(new { message = "Please confirm your email." });

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                return BadRequest(new { message = "Phone number not set." });

            var code = new Random().Next(100000, 999999).ToString();
            _login2FACodes[user.Email] = code;

            await SendEmail2fa(user, code);

            return Ok(new { message = "2FA code sent to your phone." });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] TwoFactorDto model)
        {
            if (!_login2FACodes.TryGetValue(model.Email, out var code) || code != model.Code)
                return Unauthorized(new { message = "Invalid or expired code." });

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateToken(user, roles);

            _login2FACodes.Remove(model.Email);

            return Ok(new
            {
                token,
                expiration = DateTime.UtcNow.AddDays(2),
                roles,
                user
            });
        }

        //EMAIL CONFIRMATION 

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "User not found." });

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!result.Succeeded)
                return BadRequest(new { message = "Email confirmation failed.", errors = result.Errors });

            return Ok(new { message = "Email confirmed." });
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "User not found." });

            if (user.EmailConfirmed)
                return BadRequest(new { message = "Email already confirmed." });

            await SendEmailConfirmationLink(user);
            return Ok(new { message = "Confirmation link resent." });
        }

        private async Task SendEmailConfirmationLink(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebUtility.UrlEncode(token);
            var link = $"{_config["Frontend:ConfirmEmailUrl"]}http://localhost:5173/confirm-email?email={user.Email}&token={encoded}";
            var body = $"<p>Click to verify your email: <a href='{link}'>Confirm Email</a></p>";
            await _emailService.SendEmailAsync(user.Email, "Confirm Your Email", body);
        }
        private async Task SendEmail2fa(ApplicationUser user,string code)
        {

            
            //var link = $"{_config["Frontend:ConfirmEmailUrl"]}http://localhost:5173/confirm-email?email={user.Email}&token={encoded}";
            var body = $"<p>Your 2FA code is : <strong>{code}</strong></p>";
            await _emailService.SendEmailAsync(user.Email, "Confirm Your Email", body);
        }

        // PASSWORD 

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "User not found." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = System.Web.HttpUtility.UrlEncode(token);
            var link = $"{_config["Frontend:ResetPasswordUrl"]}?email={model.Email}&token={encoded}";

            await _emailService.SendEmailAsync(model.Email, "Reset Password", $"<p><a href='{link}'>Reset</a></p>");
            return Ok(new { message = "Reset link sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "User not found." });

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Reset failed.", errors = result.Errors });

            return Ok(new { message = "Password reset." });
        }

        // HELPERS 

        private string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

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

        private bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}