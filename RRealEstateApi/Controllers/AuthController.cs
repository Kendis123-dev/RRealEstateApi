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
            _context = context;
            _roleManager = roleManager;
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

            var agent = new Agent
            {
                FullName = model.FullName,
                Email = user.Email,
                Aspuserid = user.Id,
                PhoneNumber = model.PhoneNumber,
                RegisteredAt = user.CreatedAt,
                IsVerified = false
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
                return Unauthorized(new { message = "Invalid credentials." });

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
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Code))
                    return BadRequest(new { message = "Email and code are required." });

                // Check if the 2FA code exists and matches
                if (!_login2FACodes.TryGetValue(model.Email, out var code))
                    return Unauthorized(new { message = "Code not found or expired." });

                if (code != model.Code)
                    return Unauthorized(new { message = "Invalid code." });

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { message = "User not found." });

                var roles = await _userManager.GetRolesAsync(user);
                var token = GenerateToken(user, roles);

                // Remove the used 2FA code
                _login2FACodes.Remove(model.Email);

                // Capture device fingerprint
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();
                var deviceFingerprint = $"{ipAddress}_{userAgent}";

                // Check known devices
                List<string> knownDevices = new();
                if (!string.IsNullOrEmpty(user.KnownDevicesJson))
                {
                    try
                    {
                        knownDevices = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.KnownDevicesJson);
                    }
                    catch
                    {
                        // fallback in case of bad JSON
                        knownDevices = new List<string>();
                    }
                }

                bool isNewDevice = !knownDevices.Contains(deviceFingerprint);

                if (isNewDevice)
                {
                    knownDevices.Add(deviceFingerprint);
                    user.KnownDevicesJson = System.Text.Json.JsonSerializer.Serialize(knownDevices);
                    await _userManager.UpdateAsync(user);

                    var emailBody = $@"
         <p>New login detected on your account.</p>
         <p><strong>IP Address:</strong> {ipAddress}</p>
         <p><strong>Device:</strong> {userAgent}</p>
         <p><strong>Time (UTC):</strong> {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC</p>
         <p>If this wasn't you, please <a href='http://localhost:5173/forgot-password'>reset your password immediately</a>.</p>";

                    await _emailService.SendEmailAsync(user.Email, "Security Alert: New Device Login", emailBody);
                }

                // Log activity
                var loginLog = new LoginActivity
                {
                    UserId = user.Id,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    LoginTime = DateTime.UtcNow
                };

                _context.LoginActivities.Add(loginLog);
                await _context.SaveChangesAsync();

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred during 2FA verification.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("known-devices")]
        public async Task<IActionResult> GetKnownDevices()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            List<string> knownDevices = new();
            if (!string.IsNullOrEmpty(user.KnownDevicesJson))
            {
                knownDevices = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.KnownDevicesJson);
            }

            return Ok(new { devices = knownDevices });
        }

        [Authorize]
        [HttpPost("remove-device")]
        public async Task<IActionResult> RemoveKnownDevice([FromBody] RemoveDeviceDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            List<string> knownDevices = new();
            if (!string.IsNullOrEmpty(user.KnownDevicesJson))
            {
                knownDevices = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.KnownDevicesJson);
            }

            if (knownDevices.Contains(model.DeviceFingerprint))
            {
                knownDevices.Remove(model.DeviceFingerprint);
                user.KnownDevicesJson = System.Text.Json.JsonSerializer.Serialize(knownDevices);
                await _userManager.UpdateAsync(user);

                return Ok(new { message = "Device removed successfully." });
            }

            return BadRequest(new { message = "Device not found." });
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

        private async Task SendEmail2fa(ApplicationUser user, string code)
        {
            var body = $"<p>Your 2FA code is : <strong>{code}</strong></p>";
            await _emailService.SendEmailAsync(user.Email, "Your 2FA Code", body);
        }

        // PASSWORD 

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "User not found." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Web.HttpUtility.UrlEncode(token);

            //  Access the value from your config
            var resetUrl = _config["http://localhost:5173/forgot-password/create-new-password"];
            //var link = $"{resetUrl}?email={model.Email}&token={encodedToken}";
            var link = $"{resetUrl}";

            // Send the email
            await _emailService.SendEmailAsync(model.Email, "Reset Password", $@"
        <p>Click below to reset your password:</p>
        <p><a href='http://localhost:5173/forgot-password/create-new-password'>Reset Password</a></p>
        <p>If the link doesn't work, copy and paste this in your browser:</p>
        <p>http://localhost:5173/forgot-password/create-new-password</p>
    ");

            return Ok(new { message = "Reset link sent." });
        }

        [HttpPost("update-change-password")]
        public async Task<IActionResult> ManualPasswordUpdate([FromBody] UpdateChangePasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // Hash new password manually
            var hashedPassword = _userManager.PasswordHasher.HashPassword(user, model.NewPassword);

            // Update the password hash directly
            user.PasswordHash = hashedPassword;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = "Password update failed.", errors = result.Errors });

            return Ok(new { message = "Password updated successfully." });
        }


        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "User not found or already deleted." });

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
                return BadRequest(new { message = "Invalid password." });

            var email = user.Email;
            var fullName = user.FullName;

            try
            {
                // With cascade delete configured, just delete the user
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { message = "Failed to delete account.", errors = result.Errors });
                }

                // Email notification
                var subject = "Account Deleted";
                var body = $@"
            <p>Dear {fullName},</p>
            <p>Your account associated with this email <strong>{email}</strong> has been deleted successfully.</p>
            <p>If this wasn't you or you have any questions, please contact our support team.</p>
            <br/>
            <p>Thank you,<br/>The Real Estate Team</p>";

                await _emailService.SendEmailAsync(email, subject, body);

                return Ok(new { message = "Account deleted and email notification sent." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the account.", error = ex.Message });
            }
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            // This method checks the old password and sets the new one
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { message = "Reset failed.", errors = result.Errors });

            return Ok(new { message = "Password changed successfully." });
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
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}