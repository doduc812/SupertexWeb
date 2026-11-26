using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupertexWeb.Data;
using SupertexWeb.Models;
using System.Diagnostics;

namespace SupertexWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        // 1. TRANG CHỦ KHÁCH HÀNG: Chỉ lấy sản phẩm NỔI BẬT (IsFeatured) và dữ liệu HomeAbout
        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _context.Products
                                                .Where(p => p.IsActive && p.IsFeatured)
                                                .OrderByDescending(p => p.Id)
                                                .ToListAsync();

            ViewBag.HomeAbout = await _context.HomeAboutSections.FirstOrDefaultAsync();

            var latestPosts = await _context.Posts
                                            .Where(p => p.IsPublished)
                                            .OrderByDescending(p => p.PublishedDate)
                                            .Take(3)
                                            .ToListAsync();
            ViewBag.LatestPosts = latestPosts;

            return View(featuredProducts);
        }

        // 2. TRANG DANH MỤC RIÊNG CHO TỪNG LOẠI TẤT
        public async Task<IActionResult> Category(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            var categoryName = Enum.GetNames(typeof(SockCategory))
                .FirstOrDefault(name =>
                    string.Equals(name, id, StringComparison.OrdinalIgnoreCase));

            if (categoryName is null)
            {
                return NotFound();
            }

            var category = Enum.Parse<SockCategory>(categoryName);

            var products = await _context.Products
                .Where(p => p.IsActive && p.Category == category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.CurrentCategory = categoryName;
            return View(products);
        }

        // 3. TRANG CHI TIẾT SẢN PHẨM CHO KHÁCH HÀNG
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (product == null)
            {
                return NotFound();
            }

            var relatedProducts = await _context.Products
                                                .Where(p => p.IsActive && p.Id != id)
                                                .OrderByDescending(p => p.Category == product.Category)
                                                .ThenByDescending(p => p.Id)
                                                .Take(4)
                                                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // 4. TRANG HỒ SƠ NĂNG LỰC CHI TIẾT CÔNG TY
        public async Task<IActionResult> About()
        {
            var aboutInfo = await _context.AboutPages.FirstOrDefaultAsync();
            if (aboutInfo == null)
            {
                aboutInfo = new AboutPage();
            }
            return View(aboutInfo);
        }

        // 5. TRANG CHI TIẾT BÀI VIẾT CHO KHÁCH HÀNG
        public async Task<IActionResult> PostDetail(int id)
        {
            var post = await _context.Posts
                                        .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        // ==========================================
        // XỬ LÝ GỬI YÊU CẦU BÁO GIÁ (RFQ) & NHẬN MẪU THỬ (SAMPLES) QUA SKU
        // ==========================================

        private async Task PrepareRequestFormViewBagAsync(string? code)
        {
            ViewBag.SelectedCode = code;
            if (!string.IsNullOrEmpty(code))
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Code == code);
                ViewBag.SelectedProduct = product != null ? $"{product.Name} (Mã: {product.Code})" : code;
            }
            ViewBag.ProductList = await _context.Products.Where(p => p.IsActive).ToListAsync();
        }

        private bool IsSpamSubmission(string? websiteTrap, long? formRenderedAt, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. Chặn honeypot nếu bot tự điền trường ẩn
            if (!string.IsNullOrWhiteSpace(websiteTrap))
            {
                errorMessage = "Không thể gửi yêu cầu lúc này.";
                return true;
            }

            // 2. Chặn submit quá nhanh (< 2 giây)
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (formRenderedAt.HasValue && (nowUnix - formRenderedAt.Value) < 2)
            {
                errorMessage = "Vui lòng điền thông tin kỹ hơn trước khi gửi.";
                return true;
            }

            // 3. Giới hạn tối đa 5 yêu cầu / 15 phút theo địa chỉ IP
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateKey = $"rfq_rate_{ip}";
            var count = _cache.Get<int>(rateKey);
            if (count >= 5)
            {
                errorMessage = "Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau.";
                return true;
            }

            _cache.Set(rateKey, count + 1, TimeSpan.FromMinutes(15));
            return false;
        }

        // GET: Hiển thị form yêu cầu báo giá theo Code
        [HttpGet]
        public async Task<IActionResult> RequestQuote(string? code)
        {
            await PrepareRequestFormViewBagAsync(code);
            return View();
        }

        // POST: Nhận dữ liệu báo giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestQuote(CustomerRequest model, string? WebsiteTrap, long? FormRenderedAt)
        {
            if (IsSpamSubmission(WebsiteTrap, FormRenderedAt, out var spamError))
            {
                ModelState.AddModelError("", spamError);
                await PrepareRequestFormViewBagAsync(model.ProductCode);
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await PrepareRequestFormViewBagAsync(model.ProductCode);
                return View(model);
            }

            model.RequestType = "Quote";
            model.CreatedAt = DateTime.Now;
            _context.CustomerRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["ShowSuccessModal"] = "True";
            TempData["SuccessTitle"] = "Gửi Yêu Cầu Báo Giá Thành Công!";
            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi RFQ. Bộ phận kinh doanh quốc tế của Supertex sẽ xử lý và liên hệ lại với bạn trong 24 giờ tới.";

            return RedirectToAction(nameof(RequestQuote), new { code = model.ProductCode });
        }

        // GET: Hiển thị form đăng ký nhận mẫu thử theo Code
        [HttpGet]
        public async Task<IActionResult> RequestSample(string? code)
        {
            await PrepareRequestFormViewBagAsync(code);
            return View();
        }

        // POST: Nhận dữ liệu đăng ký nhận mẫu thử
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSample(CustomerRequest model, string? WebsiteTrap, long? FormRenderedAt)
        {
            if (IsSpamSubmission(WebsiteTrap, FormRenderedAt, out var spamError))
            {
                ModelState.AddModelError("", spamError);
                await PrepareRequestFormViewBagAsync(model.ProductCode);
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await PrepareRequestFormViewBagAsync(model.ProductCode);
                return View(model);
            }

            model.RequestType = "Sample";
            model.CreatedAt = DateTime.Now;
            _context.CustomerRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["ShowSuccessModal"] = "True";
            TempData["SuccessTitle"] = "Đăng Ký Nhận Mẫu Thử Thành Công!";
            TempData["SuccessMessage"] = "Nhà máy Supertex đã tiếp nhận thông tin và sẽ sớm đóng gói bưu phẩm mẫu gửi đến địa chỉ của bạn.";

            return RedirectToAction(nameof(RequestSample), new { code = model.ProductCode });
        }
    }
}