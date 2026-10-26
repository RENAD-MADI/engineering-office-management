using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;
using RenadWebApp.Models.DataModel;
using RenadWebApp.Services;

namespace RenadWebApp.Controllers
{
    [Authorize(Roles = UserService.AdminRole)]
    public class LoginController : AppController
    {
        private readonly ApplicationDbContext _db;
        private readonly UserService _users;

        public LoginController(ApplicationDbContext db, UserService users)
        {
            _db = db;
            _users = users;
        }

        public ActionResult Index(string? search)
        {
            return View(_db.Login.Where(x => search == null || x.username.StartsWith(search)).ToList());
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = await _users.ValidateAsync(model.username, model.password);
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.userId.ToString()),
                        new(ClaimTypes.Name, user.username),
                    };
                    if (UserService.IsAdmin(user))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, UserService.AdminRole));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewBag.Message = "Fail";
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LoginModel user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            if (await _db.Login.AnyAsync(x => x.username == user.username))
            {
                return ShowMessage("اسم المستخدم موجود مسبقاً");
            }

            var newUser = new LoginModel { username = user.username, password = "" };
            newUser.password = _users.Hash(newUser, user.password);

            _db.Login.Add(newUser);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int? id)
        {
            var user = await _db.Login.AsNoTracking().FirstOrDefaultAsync(m => m.userId == id);
            if (user == null)
            {
                return ShowMessage("المستخدم غير موجود");
            }
            user.password = "";
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, LoginModel userModel)
        {
            var user = await _db.Login.FindAsync(userModel.userId);
            if (user == null)
            {
                return ShowMessage("المستخدم غير موجود");
            }
            if (await _db.Login.AnyAsync(x => x.username == userModel.username && x.userId != user.userId))
            {
                return ShowMessage("اسم المستخدم موجود مسبقاً");
            }

            user.username = userModel.username;
            // Leaving the password empty keeps the current one.
            if (!string.IsNullOrWhiteSpace(userModel.password))
            {
                user.password = _users.Hash(user, userModel.password);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public ActionResult Delete(int id)
        {
            var user = _db.Login.Find(id);
            if (user == null)
            {
                return ShowMessage("المستخدم غير موجود");
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var user = await _db.Login.SingleOrDefaultAsync(m => m.userId == id);
            if (user == null)
            {
                return ShowMessage("المستخدم غير موجود");
            }
            if (user.username == User.Identity?.Name)
            {
                return ShowMessage("لا يمكنك حذف حسابك الحالي");
            }

            _db.Login.Remove(user);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
