using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SupertexWeb.Data;
using SupertexWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CSDL (SQLite cho môi trường hiện tại)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=supertex.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Đăng ký dịch vụ MVC & Cache
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// 2B. Khi chạy sau IIS/Nginx/Cloudflare: lấy đúng IP và giao thức (http/https) thật của khách.
//     Mặc định chỉ tin proxy đặt cùng máy (loopback). Nếu proxy ở máy khác, thêm IP của nó vào KnownProxies.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// 3. Cấu hình Cookie Authentication cho khu vực Admin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Môi trường thật: cookie chỉ gửi qua HTTPS. Khi chạy local (Development) vẫn cho phép http://localhost.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        // Kiểm tra lại tài khoản trong DB ở MỖI request của admin:
        //  - tài khoản bị xóa/khóa -> đăng xuất ngay
        //  - mật khẩu bị đổi -> các phiên cũ bị đăng xuất
        //  - quyền bị thay đổi -> cập nhật quyền mới ngay, không cần chờ hết 8 giờ
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var idRaw = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (principal == null || !int.TryParse(idRaw, out var userId))
            {
                await RejectAsync(context);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await db.AdminUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
            {
                await RejectAsync(context);
                return;
            }

            var fresh = AdminClaims.BuildPrincipal(user);

            if (principal.FindFirstValue(AdminClaims.StampClaim) != fresh.FindFirstValue(AdminClaims.StampClaim))
            {
                await RejectAsync(context);
                return;
            }

            if (AdminClaims.PermissionSignature(principal) != AdminClaims.PermissionSignature(fresh))
            {
                context.ReplacePrincipal(fresh);
                context.ShouldRenew = true;
            }
        };
    });

var app = builder.Build();

// Tự tạo/cập nhật cấu trúc CSDL khi khởi động. Cần thiết vì file supertex.db không còn nằm trong Git:
// máy mới hoặc server mới chạy lần đầu sẽ tự có đủ bảng (dữ liệu thật vẫn giữ nguyên nếu DB đã có).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Phải đặt ĐẦU TIÊN để các bước sau (HTTPS redirect, rate limit theo IP...) thấy đúng IP/giao thức
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Các header bảo mật cơ bản (chưa bật CSP vì trang đang dùng script inline và CDN)
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();

// 4. Bắt buộc đặt UseAuthentication() TRƯỚC UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task RejectAsync(CookieValidatePrincipalContext context)
{
    context.RejectPrincipal();
    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
