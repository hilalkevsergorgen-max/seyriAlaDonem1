using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEYRİ_ALA.Data;
using SEYRİ_ALA.Models;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
namespace SEYRİ_ALA.Controllers
{
    public class AccountController : Controller
    {
        //  veritabanı bağlantısını ekliyoruz
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        //  boş form yapıları (Aynen korundu)
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            //  validation (doğrulama) yapısını koruyoruz
            if (!ModelState.IsValid) return View(model);

            // Kayıt Mantığı - Veritabanı kontrolü ve EF Core ile yazma
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ViewBag.Message = "❌ Bu e-posta zaten kayıtlı.";
                return View(model);
            }
  
            var user = new User
            {
                FullName = model.FullName, // Burayı ekledik, veritabanına isim gitmeli
                Email = model.Email,
                // BCrypt ile şifreyi hashliyoruz:
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "User" // Varsayılan rol
            };
           
            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Önce kullanıcıyı e-posta ile bul
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            // 2. Kullanıcı varsa VE şifresi BCrypt ile doğrulanıyorsa içeri al
            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                var claims = new List<Claim>
                  {
                           new Claim(ClaimTypes.Name, user.Email),
                           new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                           new Claim(ClaimTypes.Role, user.Role), // Rol Bazlı Erişim
                           new Claim("FullName", user.FullName ?? "") // Null kontrolü eklendi
                   };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "❌ Hatalı giriş bilgileri.";
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }
    }
}