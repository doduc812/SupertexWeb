using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupertexWeb.Data;
using SupertexWeb.Models;
using SupertexWeb.Services;
using System.Security.Claims;

namespace SupertexWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountController> _logger;
        private readonly PasswordHasher<AdminUser> _passwordHasher = new();

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

        public AccountController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<AccountController> logger)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        private async Task EnsureSeedSuperAdminAsync()
        {
            if (await _context.AdminUsers.AnyAsync())
            {
                return;
            }

            // KHÔNG còn mật khẩu mặc định trong mã nguồn.
            // Mật khẩu ban đầu phải được cấp qua User Secrets / biến môi trường:
            //   AdminAccount:InitialPassword  (mật khẩu thường, sẽ được băm khi tạo tài khoản)
            //   hoặc AdminAccount:PasswordHash (mã băm đã tạo sẵn)
            var configuredHash = _configuration["AdminAccount:PasswordHash"];
            var initialPassword = _configuration["AdminAccount:InitialPassword"];
            var hasHash = !string.IsNullOrWhiteSpace(configuredHash);
            var hasPassword = !string.IsNullOrWhiteSpace(initialPassword);

            if (!hasHash && !hasPassword)
            {
                _logger.LogError(
                    "Chưa có tài khoản admin nào và chưa cấu hình AdminAccount:InitialPassword " +
                    "(hoặc AdminAccount:PasswordHash). Không tạo tài khoản mặc định.");
                return;
            }

            var seedUser = new AdminUser
            {
                Username = (_configuration["AdminAccount:Username"] ?? "admin@supertex.com.vn").Trim().ToLowerInvariant(),
                FullName = "SuperAdmin (Chủ hệ thống)",
                IsSuperAdmin = true,
                CanFullAccess = true,
                CanManageProducts = true,
                CanManagePosts = true,
                CanManageCustomers = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            seedUser.PasswordHash = hasHash
                ? configuredHash!.Trim()
                : _passwordHasher.HashPassword(seedUser, initialPassword!);

            _context.AdminUsers.Add(seedUser);
            await _context.SaveChangesAsync();
        }

        private bool CurrentUserHasFullAccess()
        {
            return string.Equals(User.FindFirstValue("IsSuperAdmin"), "True", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(User.FindFirstValue("CanFullAccess"), "True", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return CurrentUserHasFullAccess()
                    ? RedirectToAction("Index", "Admin")
                    : RedirectToAction("Workspace", "Admin");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var normalizedUser = (username ?? string.Empty).Trim().ToLowerInvariant();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var lockKey = $"login_lock_{ip}_{normalizedUser}";

            if (_cache.TryGetValue(lockKey, out int attempts) && attempts >= MaxFailedAttempts)
            {
                ViewBag.Error = "Bạn đã nhập sai quá 5 lần. Vui lòng thử lại sau 10 phút.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            await EnsureSeedSuperAdminAsync();

            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUser);

            bool isValid = false;
            if (user != null && user.IsActive && !string.IsNullOrEmpty(password))
            {
                var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                isValid = verifyResult == PasswordVerificationResult.Success ||
                          verifyResult == PasswordVerificationResult.SuccessRehashNeeded;
            }

            if (isValid && user != null)
            {
                _cache.Remove(lockKey);

                var hasFull = user.IsSuperAdmin || user.CanFullAccess;

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    AdminClaims.BuildPrincipal(user),
                    new AuthenticationProperties { IsPersistent = false });

                if (!hasFull)
                {
                    return RedirectToAction("Workspace", "Admin");
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Admin");
            }

            var nextAttempts = attempts + 1;
            _cache.Set(lockKey, nextAttempts, LockoutDuration);

            ViewBag.Error = nextAttempts >= MaxFailedAttempts
                ? "Bạn đã nhập sai quá 5 lần. Tài khoản tạm khóa đăng nhập trong 10 phút."
                : $"Sai thông tin đăng nhập hoặc tài khoản bị khóa (còn {MaxFailedAttempts - nextAttempts} lần thử).";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}