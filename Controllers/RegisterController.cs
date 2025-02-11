using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AceJobAgency.ViewModels;
using ApplicationSecurityApp.Models;
using ApplicationSecurityApp.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ApplicationSecurityApp.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RegistrationController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(MembershipRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            if (await _context.Members.AnyAsync(m => m.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Hash the password
            string hashedPassword = model.HashPassword();

            // Convert ViewModel to Member Model
            var newMember = new Member
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,
                EncryptedNRIC = EncryptionHelper.Encrypt(model.NRIC), // Encrypt NRIC before storing
                Email = model.Email,
                Password = hashedPassword, // Store hashed password
                DateOfBirth = model.DateOfBirth,
                WhoAmI = model.WhoAmI
            };

            // Save the resume file and store path
            if (model.Resume != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

                string uniqueFileName = $"{Guid.NewGuid()}_{model.Resume.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Resume.CopyToAsync(fileStream);
                }

                newMember.ResumeFilePath = "/uploads/" + uniqueFileName;
            }

            // Save the user in the database
            _context.Members.Add(newMember);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegisterSuccess");
        }

        public IActionResult RegisterSuccess()
        {
            return View();
        }
    }
}
