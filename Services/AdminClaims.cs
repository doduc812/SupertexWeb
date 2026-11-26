using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using SupertexWeb.Models;

namespace SupertexWeb.Services
{
    /// <summary>
    /// Tạo ClaimsPrincipal cho tài khoản admin ở MỘT nơi duy nhất,
    /// dùng chung cho lúc đăng nhập (AccountController) và lúc kiểm tra lại phiên (Program.cs).
    /// </summary>
    public static class AdminClaims
    {
        public const string StampClaim = "PwdStamp";

        private static readonly string[] PermissionClaims =
        {
            "IsSuperAdmin", "CanFullAccess", "CanManageProducts", "CanManagePosts", "CanManageCustomers", "FullName"
        };

        public static ClaimsPrincipal BuildPrincipal(AdminUser user)
        {
            var hasFull = user.IsSuperAdmin || user.CanFullAccess;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim("IsSuperAdmin", user.IsSuperAdmin.ToString()),
                new Claim("CanFullAccess", hasFull.ToString()),
                new Claim("CanManageProducts", (hasFull || user.CanManageProducts).ToString()),
                new Claim("CanManagePosts", (hasFull || user.CanManagePosts).ToString()),
                new Claim("CanManageCustomers", (hasFull || user.CanManageCustomers).ToString()),
                // Dấu vân tay của mật khẩu: đổi mật khẩu -> các phiên cũ tự hết hiệu lực
                new Claim(StampClaim, ComputeStamp(user.PasswordHash))
            };

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        /// <summary>Chuỗi so sánh các quyền hiện có trong claims (để biết quyền đã bị đổi chưa).</summary>
        public static string PermissionSignature(ClaimsPrincipal principal)
        {
            var sb = new StringBuilder();
            foreach (var type in PermissionClaims)
            {
                sb.Append(type).Append('=').Append(principal.FindFirstValue(type)).Append(';');
            }
            return sb.ToString();
        }

        private static string ComputeStamp(string passwordHash)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash ?? string.Empty));
            return Convert.ToHexString(bytes, 0, 8);
        }
    }
}
