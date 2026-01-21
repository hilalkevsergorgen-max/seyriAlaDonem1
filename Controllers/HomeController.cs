using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEYRİ_ALA.Data;
using SEYRİ_ALA.Models;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace SEYRİ_ALA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // --- DİL DEĞİŞTİRME FONKSİYONU ---
        [HttpPost]
        public IActionResult ChangeLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
        // ---------------------------------

        public async Task<IActionResult> Index()
        {
            var cities = await _context.Cities.ToListAsync();
            return View(cities);
        }

        public async Task<IActionResult> Details(int id)
        {
            var city = await _context.Cities
                .Include(c => c.Comments)
                    .ThenInclude(com => com.User)
                .Include(c => c.Favorites)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int CityId, string Content)
        {
            if (string.IsNullOrWhiteSpace(Content))
                return RedirectToAction("Details", new { id = CityId });

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                var comment = new Comment
                {
                    CityId = CityId,
                    UserId = userId,
                    Text = Content,
                    CreatedAt = DateTime.Now,
                    IsApproved = true
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = CityId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int cityId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.CityId == cityId && f.UserId == userId);

                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                    TempData["FavoriteMessage"] = "Şehir favorilerinizden çıkarıldı.";
                }
                else
                {
                    _context.Favorites.Add(new Favorite { CityId = cityId, UserId = userId });
                    TempData["FavoriteMessage"] = "Şehir favorilerinize başarıyla eklendi!";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = cityId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id);

            if (comment != null && userIdString != null && comment.UserId.ToString() == userIdString)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                TempData["FavoriteMessage"] = "Yorumunuz başarıyla silindi.";
            }

            return RedirectToAction("Details", new { id = comment?.CityId });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}