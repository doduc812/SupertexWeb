using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm thư viện này để dùng [Column]

namespace SupertexWeb.Models
{
    /// <summary>
    /// Sock Category Classification theo tiêu chuẩn mới
    /// </summary>
    public enum SockCategory
    {
        [Display(Name = "Tất trẻ em")]
        ChildrenSocks = 1,      // ST-KID

        [Display(Name = "Tất thể thao")]
        SportSocks = 2,         // ST-SPT

        [Display(Name = "Tất thông thường")]
        NormalSocks = 3,        // ST-NRM

        [Display(Name = "Tất năm ngón")]
        FingerSocks = 4,        // ST-FNG

        [Display(Name = "Tất đặc biệt khác")]
        OtherSpecialSocks = 5   // ST-SPC
    }

    /// <summary>
    /// Main Product Entity for Supertex
    /// </summary>
    public class Product
    {
        [Key]
        public int Id { get; set; }

        // Mã SKU tự động sinh theo chuẩn: ST-KID-001, ST-SPT-001...
        [StringLength(50)]
        [Display(Name = "Mã sản phẩm (SKU)")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "TEXT")] // Khai báo chuẩn kiểu TEXT cho SQLite
        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(300)]
        [Display(Name = "Hình ảnh sản phẩm")]
        public string? ImageUrl { get; set; }

        [Required]
        [Display(Name = "Danh mục tất")]
        public SockCategory Category { get; set; } = SockCategory.NormalSocks;

        // Thông số kỹ thuật dệt may
        [StringLength(250)]
        [Display(Name = "Chất liệu sợi")]
        public string? Material { get; set; }

        [StringLength(100)]
        [Display(Name = "Kích cỡ (Size)")]
        public string? Size { get; set; }

        [StringLength(100)]
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }

        // Trạng thái quản lý
        [Display(Name = "Đang hiển thị")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}