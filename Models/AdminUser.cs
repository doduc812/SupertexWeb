using System.ComponentModel.DataAnnotations;

namespace SupertexWeb.Models
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Bậc cao nhất: không thể bị xóa, khóa hoặc hạ quyền bởi bất kỳ tài khoản nào khác
        public bool IsSuperAdmin { get; set; } = false;

        // Nút gạt Full: toàn quyền nghiệp vụ (trừ đụng vào IsSuperAdmin)
        public bool CanFullAccess { get; set; } = false;

        // Nút gạt Sản phẩm: Products, Create, Edit, Delete
        public bool CanManageProducts { get; set; } = false;

        // Nút gạt Bài viết: Posts, Upsert, DeletePost, UploadEditorImage
        public bool CanManagePosts { get; set; } = false;

        // Nút gạt Khách hàng: CustomerRequests, ToggleProcessed, DeleteCustomerRequest
        public bool CanManageCustomers { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}