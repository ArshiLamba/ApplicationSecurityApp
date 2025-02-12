using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ApplicationSecurityApp.Models;
using ApplicationSecurityApp.ViewModels;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using ApplicationSecurityApp.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;

namespace ApplicationSecurityApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ReCaptchaService _reCaptchaService;



        public AccountController(ApplicationDbContext context, IConfiguration configuration, ReCaptchaService reCaptchaService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
            _configuration = configuration;
            _reCaptchaService = reCaptchaService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpGet]
        public IActionResult SendTwoFactorCode() => View();

        [HttpGet]
        public IActionResult VerifyTwoFactor() => View();




        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Verify reCAPTCHA token
            if (string.IsNullOrEmpty(model.ReCaptchaToken) || !await _reCaptchaService.VerifyTokenAsync(model.ReCaptchaToken))
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            var user = await _context.Members.FirstOrDefaultAsync(m => m.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Check if account is locked
            if (user.IsLockedOut && user.LockoutEnd > DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Your account is locked. Please try again later.");
                return View(model);
            }

            // Verify password before signing in
            if (!VerifyPassword(model.Password, user.Password))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 3)
                {
                    user.IsLockedOut = true;
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    ModelState.AddModelError("", "Your account is locked due to multiple failed login attempts.");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }

                await _context.SaveChangesAsync();
                return View(model);
            }

            // Reset failed attempts on successful login
            user.FailedLoginAttempts = 0;

            // Check if 2FA is enabled
            if (user.Is2FAEnabled)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                return RedirectToAction("SendTwoFactorCode");
            }

            // Ensure proper session handling
            if (!string.IsNullOrEmpty(user.SessionId))
            {
                user.SessionId = null;
                await _context.SaveChangesAsync();
            }

            string newSessionId = Guid.NewGuid().ToString();
            user.SessionId = newSessionId;

            // Clear any previous session and set new session values
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("SessionId", newSessionId);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("SessionStart", DateTime.UtcNow.ToString());

            await _context.SaveChangesAsync();

            // Create authentication cookie after successful login
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim("SessionId", user.SessionId)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            await LogAudit(user.Id, "User logged in.");

            return RedirectToAction("LoginSuccess", "Account");
        }



        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var user = await _context.Members.FirstOrDefaultAsync(m => m.Id == userId);
                if (user != null)
                {
                    user.SessionId = null; // Clear session ID to prevent reuse
                    await _context.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear(); // Clear all session data
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Clear authentication

            return RedirectToAction("Login");
        }


        public IActionResult LoginSuccess()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Members.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (!VerifyPassword(model.CurrentPassword, user.Password))
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View(model);
            }

            var now = DateTime.UtcNow;
            const int minPasswordAgeDays = 1;   // Example: User must wait at least 1 day before changing the password again
            const int maxPasswordAgeDays = 90;  // Example: Password expires after 90 days

            // 🔴 Enforce Minimum Password Age
            if (user.LastPasswordChange.HasValue)
            {
                var passwordAge = (now - user.LastPasswordChange.Value).TotalDays;
                if (passwordAge < minPasswordAgeDays)
                {
                    ModelState.AddModelError("", $"You must wait at least {minPasswordAgeDays} day(s) before changing your password again.");
                    return View(model);
                }
            }

            // 🔴 Enforce Maximum Password Age
            if (user.LastPasswordChange.HasValue)
            {
                var passwordAge = (now - user.LastPasswordChange.Value).TotalDays;
                if (passwordAge > maxPasswordAgeDays)
                {
                    ModelState.AddModelError("", "Your password has expired. Please change it now.");
                    return View(model);
                }
            }

            // 🔴 Check Password History (Prevent reusing last 2 passwords)
            var recentPasswords = _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedAt)
                .Take(2)
                .Select(ph => ph.HashedPassword)
                .ToList();

            foreach (var oldPassword in recentPasswords)
            {
                if (_passwordHasher.VerifyHashedPassword(user, oldPassword, model.NewPassword) == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "You cannot reuse your last 2 passwords.");
                    return View(model);
                }
            }

            // 🔴 Update the User's Password
            user.Password = HashPassword(model.NewPassword);
            user.LastPasswordChange = now;

            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                HashedPassword = user.Password,
                ChangedAt = now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token });

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Members.FirstOrDefaultAsync(m => m.ResetToken == model.Token);
            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Invalid or expired token.");
                return View(model);
            }

            var recentPasswords = _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.ChangedAt)
                .Take(2)
                .Select(ph => ph.HashedPassword)
                .ToList();

            foreach (var oldPassword in recentPasswords)
            {
                if (_passwordHasher.VerifyHashedPassword(user, oldPassword, model.NewPassword) == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "You cannot reuse your last 2 passwords.");
                    return View(model);
                }
            }

            user.Password = HashPassword(model.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.LastPasswordChange = DateTime.UtcNow;

            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                HashedPassword = user.Password,
                ChangedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password reset successfully.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Members.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Email == model.Email);

            if (user == null)
            {
                ViewBag.SuccessMessage = "If this email exists, a reset link has been sent.";
                return View();
            }

            // Clear any existing reset token before generating a new one
            _context.Attach(user);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync(); // Save to clear old token

            // Generate a new reset token
            var resetToken = Guid.NewGuid().ToString();
            user.ResetToken = resetToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync(); // Ensure new token is stored in the database

            // Generate reset link
            var resetLink = $"https://localhost:7009/Account/ResetPassword?token={resetToken}";

            // Send email with a timestamp in subject to avoid email queue issues
            SendPasswordResetEmail(user.Email, resetLink);

            ViewBag.SuccessMessage = "If this email exists, a reset link has been sent.";
            return View(); // Stay on the same page to avoid losing the response
        }



        private void SendPasswordResetEmail(string toEmail, string resetLink)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));

            // Add timestamp to subject to force refresh in email queue
            email.Subject = $"Password Reset Request - {DateTime.UtcNow}";

            email.Body = new TextPart("plain") { Text = $"Click the link to reset your password: {resetLink}" };

            using (var smtp = new SmtpClient())
            {
                smtp.Connect(emailSettings["SMTPServer"], int.Parse(emailSettings["SMTPPort"]), false);
                smtp.Authenticate(emailSettings["SMTPUsername"], emailSettings["SMTPPassword"]);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }
        [HttpPost]
        public async Task<IActionResult> SendTwoFactorCode(SendTwoFactorCode model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Members.FirstOrDefaultAsync(m => m.Email.ToLower() == model.Email.ToLower());

            if (user == null || !user.Is2FAEnabled)
            {
                ModelState.AddModelError("", "Invalid email or 2FA is not enabled for this account.");
                return View(model);
            }

            // Generate a 6-digit OTP
            var random = new Random();
            string otpCode = random.Next(100000, 999999).ToString();

            // Hash the OTP before storing it
            user.TwoFactorCode = HashPassword(otpCode); // Store hashed OTP
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5); // OTP valid for 5 minutes
            await _context.SaveChangesAsync();

            // Send OTP via email
            SendTwoFactorEmail(user.Email, otpCode); // Send plain OTP to email, not hashed

            // Store user ID in session for verification step
            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToAction("VerifyTwoFactor");
        }

        private void SendTwoFactorEmail(string toEmail, string otpCode)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));

            // Add timestamp to subject to force refresh in email queue
            email.Subject = $"Two-Factor Authentication Code - {DateTime.UtcNow}";

            email.Body = new TextPart("plain") { Text = $"Your 2FA code is: {otpCode}. It expires in 5 minutes." };

            using (var smtp = new SmtpClient())
            {
                smtp.Connect(emailSettings["SMTPServer"], int.Parse(emailSettings["SMTPPort"]), false);
                smtp.Authenticate(emailSettings["SMTPUsername"], emailSettings["SMTPPassword"]);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }




        [HttpPost]
        public async Task<IActionResult> VerifyTwoFactor(VerifyTwoCode model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Members.FindAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            // Check if OTP is valid
            if (user.TwoFactorExpiry < DateTime.UtcNow || !VerifyPassword(model.OtpCode, user.TwoFactorCode))
            {
                ModelState.AddModelError("", "Invalid or expired OTP.");
                return View(model);
            }

            // Clear OTP details after verification
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;

            // Ensure a new session ID is assigned
            user.SessionId = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();

            // Store user session details
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("SessionId", user.SessionId);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("SessionStart", DateTime.UtcNow.ToString());

            // Authenticate user after 2FA success
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Email ?? "Unknown"),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim("SessionId", user.SessionId)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            HttpContext.Session.SetString("IsTwoFactorVerified", "true");
            await LogAudit(user.Id, "User logged in via 2FA.");

            return RedirectToAction("LoginSuccess", "Account");
        }





        private string HashPassword(string password) => _passwordHasher.HashPassword(new Member(), password);

        private bool VerifyPassword(string enteredPassword, string storedHashedPassword)
            => _passwordHasher.VerifyHashedPassword(new Member(), storedHashedPassword, enteredPassword) == PasswordVerificationResult.Success;

        private async Task LogAudit(int userId, string activity)
        {
            _context.AuditLogs.Add(new AuditLog { UserId = userId, Activity = activity, Timestamp = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }
    }
}