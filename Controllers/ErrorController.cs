using Microsoft.AspNetCore.Mvc;

namespace ApplicationSecurityApp.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Oops! The page you are looking for does not exist.";
                    return View("NotFound");

                case 403:
                    ViewBag.ErrorMessage = "Access denied! You do not have permission to view this page.";
                    return View("Forbidden");

                default:
                    ViewBag.ErrorMessage = "Something went wrong! Please try again later.";
                    return View("Error");
            }
        }
    }
}