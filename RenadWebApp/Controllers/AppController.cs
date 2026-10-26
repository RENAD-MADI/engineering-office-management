using Microsoft.AspNetCore.Mvc;

namespace RenadWebApp.Controllers
{
    /// <summary>Base controller with a friendly message page instead of raw text responses.</summary>
    public abstract class AppController : Controller
    {
        protected ViewResult ShowMessage(string message)
        {
            ViewBag.Message = message;
            return View("Message");
        }
    }
}
