using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SupertexWeb.Data;
using SupertexWeb.Models;

namespace SupertexWeb.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<AdminUser> _passwordHasher = new();

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==========================================
        // HÀM HỖ TRỢ KIỂM TRA QUYỀN TỪ CLAIMS
        // ==========================================
        private bool IsClaimTrue(string claimType) =>
            string.Equals(User.FindFirstValue(claimType), "True", StringComparison.OrdinalIgnoreCase);

        private bool IsCurrentSuperAdmin() => IsClaimTrue("IsSuperAdmin");
        private bool HasFullAccess() => IsCurrentSuperAdmin() || IsClaimTrue("CanFullAccess");
        private bool HasProductAccess() => HasFullAccess() || IsClaimTrue("CanManageProducts");
        private bool HasPostAccess() => HasFullAccess() || IsClaimTrue("CanManagePosts");
        private bool HasCustomerAccess() => HasFullAccess() || IsClaimTrue("CanManageCustomers");

        private int? GetCurrentUserId()
        {
            var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawId, out int id) ? id : null;
        }

        private IActionResult? DenyIfNotAllowed(bool allowed) => allowed ? null : Forbid();

        // ==========================================
        // 1. TRANG TỔNG QUAN DASHBOARD ADMIN (DỮ LIỆU THẬT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!HasFullAccess())
            {
                return RedirectToAction(nameof(Workspace));
            }

            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalRequests = await _context.CustomerRequests.CountAsync();
            ViewBag.TotalPosts = await _context.Posts.CountAsync();
            ViewBag.RecentRequests = await _context.CustomerRequests
                                                 .OrderByDescending(r => r.CreatedAt)
                                                 .Take(5)
                                                 .ToListAsync();
            return View();
        }

        // ==========================================
        // 1B. KHÔNG GIAN LÀM VIỆC CHO ADMIN PHỤ (KHÔNG CÓ DASHBOARD)
        // ==========================================
        [HttpGet]
        public IActionResult Workspace()
        {
            ViewBag.FullName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "Quản trị viên";
            ViewBag.CanManageProducts = HasProductAccess();
            ViewBag.CanManagePosts = HasPostAccess();
            ViewBag.CanManageCustomers = HasCustomerAccess();
            return View();
        }

        // ==========================================
        // 2. DANH SÁCH QUẢN LÝ SẢN PHẨM (DẠNG LƯỚI)
        // ==========================================
        public async Task<IActionResult> Products()
        {
            if (DenyIfNotAllowed(HasProductAccess()) is { } denied) return denied;

            var products = await _context.Products
                                         .OrderByDescending(p => p.Id)
                                         .ToListAsync();
            return View(products);
        }

        // ==========================================
        // 3. MỞ FORM THÊM SẢN PHẨM MỚI (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            if (DenyIfNotAllowed(HasProductAccess()) is { } denied) return denied;
            return View(new Product());
        }

        // ==========================================
        // 4. XỬ LÝ LƯU SẢN PHẨM MỚI + TỰ ĐỘNG SINH SKU + NHẬN ẢNH CẮT BASE64 (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, string? croppedImageBase64)
        {
            if (DenyIfNotAllowed(HasProductAccess()) is { } denied) return denied;

            // Loại bỏ kiểm tra lỗi cho trường Code vì hệ thống sẽ tự động sinh ngay sau đây
            ModelState.Remove(nameof(model.Code));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Bước A: Ánh xạ Enum SockCategory sang tiền tố chuẩn ST-XXX
            string prefix = model.Category switch
            {
                SockCategory.ChildrenSocks => "ST-KID",
                SockCategory.SportSocks => "ST-SPT",
                SockCategory.NormalSocks => "ST-NRM",
                SockCategory.FingerSocks => "ST-FNG",
                SockCategory.OtherSpecialSocks => "ST-SPC",
                _ => "ST-GEN"
            };

            // Bước B: Tìm số thứ tự lớn nhất hiện có trong danh mục đó để chống trùng mã nếu từng xóa sản phẩm
            var existingCodes = await _context.Products
                .Where(p => p.Category == model.Category && p.Code.StartsWith(prefix + "-"))
                .Select(p => p.Code)
                .ToListAsync();

            int maxNumber = 0;
            foreach (var code in existingCodes)
            {
                var parts = code.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                {
                    if (num > maxNumber) maxNumber = num;
                }
            }

            // Sinh mã tự động dạng: ST-KID-001, ST-SPT-002...
            string autoCode = $"{prefix}-{(maxNumber + 1):D3}";
            model.Code = autoCode;

            // Bước C: Xử lý lưu ảnh từ chuỗi Base64 do công cụ cắt ảnh giao diện gửi lên
            if (!string.IsNullOrEmpty(croppedImageBase64))
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tách lấy phần dữ liệu base64 thực tế và chuyển thành byte mảng
                string base64Data = croppedImageBase64.Substring(croppedImageBase64.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                string fileName = $"{autoCode.ToLowerInvariant()}.jpg";
                string filePath = Path.Combine(uploadsFolder, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                model.ImageUrl = $"/images/products/{fileName}";
            }

            // Bước D: Ghi thời gian tạo và lưu xuống Database
            model.CreatedDate = DateTime.UtcNow;
            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm thành công sản phẩm [{model.Code}] - {model.Name}";
            return RedirectToAction(nameof(Products));
        }

        // ==========================================
        // 5. MỞ TRANG CHI TIẾT / CHỈNH SỬA SẢN PHẨM (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (DenyIfNotAllowed(HasProductAccess()) is { } denied) return denied;

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // ==========================================
        // 6. XỬ LÝ CẬP NHẬT THÔNG TIN SẢN PHẨM (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? imageFile)
        {
            if (DenyIfNotAllowed(HasProductAccess()) is { } denied) return denied;

            if (id != model.Id)
            {
                return NotFound();
            }

            // Loại bỏ check validation của Code vì mã SKU giữ nguyên không đổi
            ModelState.Remove(nameof(model.Code));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Cập nhật các thông tin cơ bản
                existingProduct.Name = model.Name;
                existingProduct.Description = model.Description;
                existingProduct.Material = model.Material;
                existingProduct.Size = model.Size;
                existingProduct.Color = model.Color;
                existingProduct.IsActive = model.IsActive;
                existingProduct.IsFeatured = model.IsFeatured;

                // Nếu người quản lý tải lên ảnh mới thì thay thế file cũ
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Xóa ảnh cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        string oldPhysicalPath = Path.Combine(_env.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPhysicalPath))
                        {
                            System.IO.File.Delete(oldPhysicalPath);
                        }
                    }

                    // Lưu ảnh mới với tên khớp SKU
                    string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    string fileName = $"{existingProduct.Code.ToLowerInvariant()}-{Guid.NewGuid().ToString().Substring(0, 4)}{fileExtension}";
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    existingProduct.ImageUrl = $"/images/products/{fileName}";
                }

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã cập nhật thành công sản phẩm [{existingProduct.Code}]!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Products));
        }

        // ==========================================
        // 7. XÓA SẢN PHẨM & DỌN DẸP FILE ẢNH TRONG WWWROOT (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (DenyIfNotAllowed(HasProductAccess()) is { } denied) return denied;

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Xóa file ảnh vật lý trong thư mục wwwroot nếu tồn tại
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    string physicalPath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa sản phẩm {product.Code}.";
            }
            return RedirectToAction(nameof(Products));
        }

        // ==========================================
        // 8. QUẢN LÝ BANNER ĐẦU TRANG (HERO SECTION)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Hero()
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var hero = await _context.HeroSections.FirstOrDefaultAsync();
            if (hero == null)
            {
                hero = new HeroSection();
                _context.HeroSections.Add(hero);
                await _context.SaveChangesAsync();
            }
            return View(hero);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hero(HeroSection model, IFormFile? imageFile)
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var existingHero = await _context.HeroSections.FindAsync(model.Id);
            if (existingHero == null)
            {
                return NotFound();
            }

            existingHero.BadgeText = model.BadgeText;
            existingHero.Title = model.Title;
            existingHero.Description = model.Description;
            existingHero.Stat1Value = model.Stat1Value;
            existingHero.Stat1Label = model.Stat1Label;
            existingHero.Stat2Value = model.Stat2Value;
            existingHero.Stat2Label = model.Stat2Label;
            existingHero.Stat3Value = model.Stat3Value;
            existingHero.Stat3Label = model.Stat3Label;

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "partners");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                string fileName = $"home_background_{Guid.NewGuid().ToString().Substring(0, 4)}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existingHero.ImageUrl = $"/images/partners/{fileName}";
            }

            _context.Update(existingHero);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật Banner đầu trang (Hero Section) thành công!";
            return RedirectToAction(nameof(Hero));
        }

        // ==========================================
        // 10. QUẢN LÝ TÓM TẮT NĂNG LỰC TRANG CHỦ (HOME ABOUT SECTION)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> HomeAbout()
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var homeAbout = await _context.HomeAboutSections.FirstOrDefaultAsync();
            if (homeAbout == null)
            {
                homeAbout = new HomeAboutSection();
                _context.HomeAboutSections.Add(homeAbout);
                await _context.SaveChangesAsync();
            }
            return View(homeAbout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HomeAbout(HomeAboutSection model, IFormFile? imageFile)
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var existing = await _context.HomeAboutSections.FindAsync(model.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.BadgeText = model.BadgeText;
            existing.Title = model.Title;
            existing.Description = model.Description;
            existing.IsVideoMode = model.IsVideoMode;
            existing.VideoUrl = model.VideoUrl;

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "partners");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                string fileName = $"home_about_{Guid.NewGuid().ToString().Substring(0, 4)}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existing.ImageUrl = $"/images/partners/{fileName}";
            }

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật nội dung tóm tắt năng lực trang chủ thành công!";
            return RedirectToAction(nameof(HomeAbout));
        }

        // ==========================================
        // 11. QUẢN LÝ TIN TỨC & BÀI VIẾT (NEWS / POSTS - UPSERT PATTERN)
        // ==========================================
        public async Task<IActionResult> Posts()
        {
            if (DenyIfNotAllowed(HasPostAccess()) is { } denied) return denied;

            var posts = await _context.Posts.OrderByDescending(p => p.PublishedDate).ToListAsync();
            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            if (DenyIfNotAllowed(HasPostAccess()) is { } denied) return denied;

            Post model = new Post();

            if (id == null || id == 0)
            {
                return View(model);
            }

            model = await _context.Posts.FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Post model, IFormFile? imageFile)
        {
            if (DenyIfNotAllowed(HasPostAccess()) is { } denied) return denied;

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "posts");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                string fileName = $"post_{Guid.NewGuid().ToString().Substring(0, 6)}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                model.ImageUrl = $"/images/posts/{fileName}";
            }

            if (model.Id == 0)
            {
                model.PublishedDate = DateTime.Now;
                _context.Posts.Add(model);
                TempData["SuccessMessage"] = "Đã đăng bài viết mới thành công!";
            }
            else
            {
                if (string.IsNullOrEmpty(model.ImageUrl))
                {
                    var existingPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Id);
                    if (existingPost != null)
                    {
                        model.ImageUrl = existingPost.ImageUrl;
                    }
                }

                _context.Posts.Update(model);
                TempData["SuccessMessage"] = "Đã cập nhật bài viết thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Posts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            if (DenyIfNotAllowed(HasPostAccess()) is { } denied) return denied;

            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa bài viết thành công!";
            }
            return RedirectToAction(nameof(Posts));
        }

        [HttpPost]
        public async Task<IActionResult> UploadEditorImage(IFormFile upload)
        {
            if (!HasPostAccess())
            {
                return Json(new { success = false, message = "Bạn không có quyền tải ảnh bài viết." });
            }

            if (upload != null && upload.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "PostNews");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(upload.FileName).ToLowerInvariant();
                string fileName = $"news_{Guid.NewGuid().ToString().Substring(0, 8)}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                string url = $"/images/PostNews/{fileName}";
                return Json(new { success = true, url = url });
            }
            return Json(new { success = false, message = "Không có file nào được tải lên." });
        }

        // ==========================================
        // 12. QUẢN LÝ YÊU CẦU KHÁCH HÀNG (RFQ & SAMPLES)
        // ==========================================
        public async Task<IActionResult> CustomerRequests(string? type)
        {
            if (DenyIfNotAllowed(HasCustomerAccess()) is { } denied) return denied;

            var query = _context.CustomerRequests.AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(r => r.RequestType == type);
                ViewBag.CurrentType = type;
            }
            else
            {
                ViewBag.CurrentType = "All";
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomerRequest(int id)
        {
            if (DenyIfNotAllowed(HasCustomerAccess()) is { } denied) return denied;

            var request = await _context.CustomerRequests.FindAsync(id);
            if (request != null)
            {
                _context.CustomerRequests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa yêu cầu thành công!";
            }
            return RedirectToAction(nameof(CustomerRequests));
        }

        // ==========================================
        // ĐỔI TRẠNG THÁI ĐÃ XỬ LÝ / CHƯA XỬ LÝ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProcessed(int id)
        {
            if (DenyIfNotAllowed(HasCustomerAccess()) is { } denied) return denied;

            var request = await _context.CustomerRequests.FindAsync(id);
            if (request != null)
            {
                request.IsProcessed = !request.IsProcessed;
                _context.CustomerRequests.Update(request);
                await _context.SaveChangesAsync();

                string statusText = request.IsProcessed ? "Đã đánh dấu xử lý xong" : "Đã chuyển về trạng thái chưa xử lý";
                TempData["SuccessMessage"] = $"{statusText} cho yêu cầu của khách hàng [{request.CustomerName}].";
            }
            return RedirectToAction(nameof(CustomerRequests));
        }

        // ==========================================
        // 13. QUẢN LÝ TÀI KHOẢN ADMIN & PHÂN QUYỀN NÚT GẠT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            ViewBag.IsCurrentSuperAdmin = IsCurrentSuperAdmin();
            var users = await _context.AdminUsers
                .OrderByDescending(u => u.IsSuperAdmin)
                .ThenBy(u => u.Id)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(
            string username,
            string fullName,
            string password,
            bool canFullAccess,
            bool canManageProducts,
            bool canManagePosts,
            bool canManageCustomers,
            bool isActive = true)
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var normalizedUser = (username ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ email đăng nhập và mật khẩu.";
                return RedirectToAction(nameof(Users));
            }

            if (await _context.AdminUsers.AnyAsync(u => u.Username.ToLower() == normalizedUser))
            {
                TempData["ErrorMessage"] = $"Tài khoản [{normalizedUser}] đã tồn tại.";
                return RedirectToAction(nameof(Users));
            }

            // Chỉ SuperAdmin gốc mới được cấp nút gạt Full cho tài khoản khác
            bool effectiveFull = IsCurrentSuperAdmin() && canFullAccess;

            var newUser = new AdminUser
            {
                Username = normalizedUser,
                FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedUser : fullName.Trim(),
                IsSuperAdmin = false,
                CanFullAccess = effectiveFull,
                CanManageProducts = effectiveFull || canManageProducts,
                CanManagePosts = effectiveFull || canManagePosts,
                CanManageCustomers = effectiveFull || canManageCustomers,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);

            _context.AdminUsers.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo tài khoản quản trị [{newUser.Username}].";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdmin(
            int id,
            string fullName,
            string? newPassword,
            bool canFullAccess,
            bool canManageProducts,
            bool canManagePosts,
            bool canManageCustomers,
            bool isActive)
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var targetUser = await _context.AdminUsers.FindAsync(id);
            if (targetUser == null)
            {
                return NotFound();
            }

            // SuperAdmin là cao nhất: người khác (kể cả có Full) không thể sửa/đổi quyền/đổi mật khẩu SuperAdmin
            if (targetUser.IsSuperAdmin && !IsCurrentSuperAdmin())
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa tài khoản SuperAdmin cao nhất.";
                return RedirectToAction(nameof(Users));
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                targetUser.FullName = fullName.Trim();
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                targetUser.PasswordHash = _passwordHasher.HashPassword(targetUser, newPassword);
            }

            if (!targetUser.IsSuperAdmin)
            {
                bool effectiveFull = IsCurrentSuperAdmin() ? canFullAccess : targetUser.CanFullAccess;
                targetUser.CanFullAccess = effectiveFull;
                targetUser.CanManageProducts = effectiveFull || canManageProducts;
                targetUser.CanManagePosts = effectiveFull || canManagePosts;
                targetUser.CanManageCustomers = effectiveFull || canManageCustomers;
                targetUser.IsActive = isActive;
            }
            else
            {
                // Giữ nguyên trạng thái bất khả xâm phạm của SuperAdmin
                targetUser.IsSuperAdmin = true;
                targetUser.CanFullAccess = true;
                targetUser.CanManageProducts = true;
                targetUser.CanManagePosts = true;
                targetUser.CanManageCustomers = true;
                targetUser.IsActive = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật tài khoản [{targetUser.Username}].";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            if (DenyIfNotAllowed(HasFullAccess()) is { } denied) return denied;

            var targetUser = await _context.AdminUsers.FindAsync(id);
            if (targetUser == null)
            {
                return RedirectToAction(nameof(Users));
            }

            // Tuyệt đối không cho xóa SuperAdmin
            if (targetUser.IsSuperAdmin)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản SuperAdmin cao nhất.";
                return RedirectToAction(nameof(Users));
            }

            // Không cho tự xóa chính tài khoản đang đăng nhập
            var currentId = GetCurrentUserId();
            if (currentId.HasValue && currentId.Value == targetUser.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Users));
            }

            _context.AdminUsers.Remove(targetUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa tài khoản [{targetUser.Username}].";
            return RedirectToAction(nameof(Users));
        }
    }
}