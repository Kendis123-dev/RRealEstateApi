using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RRealEstateApi.Data;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;
using RRealEstateApi.Services;
using RRealEstateApi.Services.Implementations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace RRealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IPhoneService _phoneService;
        private readonly RealEstateDbContext _context;
        private static readonly Dictionary<string, string> _login2FACodes = new();

        public AuthController(
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            RealEstateDbContext context,
            IConfiguration config,
            IEmailService emailService,
            IPhoneService phoneService)
        {
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _config = config;
            _emailService = emailService;
            _phoneService = phoneService;
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
        {
            if (!IsValidEmail(model.Email)) return BadRequest(new { message = "Invalid email format" });
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Admin already exists." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false,
                IsDisabled = false
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
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false,
                IsDisabled = false
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
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false,
                IsDisabled = false
            };

            var agent = new Agent
            {
                FullName = model.FullName,
                Email = user.Email,
                PhoneNumber = model.PhoneNumber,
                IsVerified = false,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            user.AgentId = agent.Id;

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Agent"))
                await _roleManager.CreateAsync(new IdentityRole("Agent"));

            await _userManager.AddToRoleAsync(user, "Agent");

            await SendEmailConfirmationLink(user);
            return Ok(new { message = "Agent registered. Please confirm your email." });
        }

        [HttpPost("login-user")]
        public async Task<IActionResult> LoginUser([FromBody] LoginDto model)
        {
            return await HandleLogin(model, "User");
        }

        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto model)
        {
            return await HandleLogin(model, "Admin");
        }

        [HttpPost("login-agent")]
        public async Task<IActionResult> LoginAgent([FromBody] LoginDto model)
        {
            return await HandleLogin(model, "Agent");
        }

        private async Task<IActionResult> HandleLogin(LoginDto model, string role)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            if (user.IsDisabled)
                return Unauthorized(new { message = "Account has been disabled." });

            if (!await _userManager.IsInRoleAsync(user, role))
                return Unauthorized(new { message = $"Not a {role.ToLower()}." });

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

            _login2FACodes.Remove(model.Email);

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateToken(user, roles);

            return Ok(new
            {
                token,
                expiration = DateTime.UtcNow.AddMinutes(10),
                roles,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.ProfilePictureUrl
                }
            });
        }

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
            if (user == null || user.EmailConfirmed)
                return BadRequest(new { message = "User not found or already confirmed." });

            await SendEmailConfirmationLink(user);
            return Ok(new { message = "Confirmation link resent." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebUtility.UrlEncode(token);
            var link = $"{_config["Frontend:ResetPasswordurl"]}?email={user.Email}&token={encoded}";

            await _emailService.SendEmailAsync(user.Email, "Reset Password", $@"
                <p>Click below to reset your password:</p>
                <p><a href='{link}'>Reset Password</a></p>");

            return Ok(new { message = "Reset link sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Reset failed.", errors = result.Errors });

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPost("update-change-password")]
        public async Task<IActionResult> ManualPasswordUpdate([FromBody] UpdateChangePasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return NotFound(new { message = "User not found." });

            var hashed = _userManager.PasswordHasher.HashPassword(user, model.NewPassword);
            user.PasswordHash = hashed;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? Ok(new { message = "Password updated successfully." })
                : BadRequest(new { message = "Update failed.", errors = result.Errors });
        }

        [HttpDelete("disable-account")]
        [Authorize]
        public async Task<IActionResult> DisableAccount([FromBody] DisableAccountDto model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            var valid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!valid)
                return BadRequest(new { message = "Invalid password." });

            user.IsDisabled = true;
            await _userManager.UpdateAsync(user);

            await _emailService.SendEmailAsync(user.Email, "Account Disabled", $@"
                <p>Your account has been permanently disabled.</p>");

            return Ok(new { message = "Account disabled successfully." });
        }

        private async Task SendEmailConfirmationLink(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"{_config["Frontend:ConfirmEmailUrl"]}?email={user.Email}&token={WebUtility.UrlEncode(token)}";
            await _emailService.SendEmailAsync(user.Email, "Confirm Your Email", $"<p>Click to verify your email: <a href='{link}'>Confirm Email</a></p>");
        }

        private async Task SendEmail2fa(ApplicationUser user, string code)
        {
            var body = $"<p>Your 2FA code is: <strong>{code}</strong></p>";
            await _emailService.SendEmailAsync(user.Email, "Your 2FA Code", body);
        }

        private string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
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
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}