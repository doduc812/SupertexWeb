using System.ComponentModel.DataAnnotations;

namespace SupertexWeb.Models
{
    public class HeroSection
    {
        public int Id { get; set; }

        [Display(Name = "Huy hiệu thông báo (Badge)")]
        public string? BadgeText { get; set; } = "✨ ISO 9001:2015 & OEKO-TEX Standard";

        [Display(Name = "Tiêu đề chính (Title)")]
        public string? Title { get; set; } = "Sản xuất tất OEM/ODM cho thị trường quốc tế";

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; } = "Supertex chuyên gia công và sản xuất tất theo yêu cầu khắt khe cho các đối tác tại Nhật Bản, Mỹ và Châu Âu. Kiểm soát toàn diện từ nguồn sợi đến thành phẩm.";

        [Display(Name = "Đường dẫn hình ảnh Banner/Nhà máy")]
        public string? ImageUrl { get; set; } = "/images/partners/home_background.png";

        [Display(Name = "Thống kê 1 (Ví dụ: 100M+)")]
        public string? Stat1Value { get; set; } = "100M+";
        [Display(Name = "Nhãn thống kê 1")]
        public string? Stat1Label { get; set; } = "Sản phẩm / năm";

        [Display(Name = "Thống kê 2 (Ví dụ: 100%)")]
        public string? Stat2Value { get; set; } = "100%";
        [Display(Name = "Nhãn thống kê 2")]
        public string? Stat2Label { get; set; } = "Sợi chuẩn quốc tế";

        [Display(Name = "Thống kê 3 (Ví dụ: US & JP & EU)")]
        public string? Stat3Value { get; set; } = "US & JP & EU";
        [Display(Name = "Nhãn thống kê 3")]
        public string? Stat3Label { get; set; } = "Thị trường xuất khẩu";
    }
}