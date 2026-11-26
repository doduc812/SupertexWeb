using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SupertexWeb.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CSDL (SQLite cho môi trường hiện tại)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=supertex.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Đăng ký dịch vụ MVC & Cache
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// 3. Cấu hình Cookie Authentication cho khu vực Admin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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