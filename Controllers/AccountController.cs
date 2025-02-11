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


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Verify reCAPTCHA token
            if (string.IsNullOrEmpty(model.ReCaptchaToken))
            {
                ModelState.AddModelError("", "Invalid reCAPTCHA token.");
                return View(model);
            }

            var isHuman = await _reCaptchaService.VerifyTokenAsync(model.ReCaptchaToken);
            if (!isHuman)
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

            if (user.IsLockedOut && user.LockoutEnd > DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Your account is locked. Please try again later.");
                return View(model);
            }

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

            if (user.IsLockedOut && user.LockoutEnd <= DateTime.UtcNow)
            {
                user.IsLockedOut = false;
                user.FailedLoginAttempts = 0;
            }

            if (!string.IsNullOrEmpty(user.SessionId))
            {
                user.SessionId = null;
                await _context.SaveChangesAsync();
            }

            string newSessionId = Guid.NewGuid().ToString();
            user.SessionId = newSessionId;

            HttpContext.Session.SetString("SessionId", newSessionId);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("SessionStart", DateTime.UtcNow.ToString());

            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

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
                    user.SessionId = null;
                    await _context.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

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
            user.LastPasswordChange = DateTime.UtcNow;

            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                HashedPassword = user.Password,
                ChangedAt = DateTime.UtcNow
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
