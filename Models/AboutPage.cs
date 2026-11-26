using System.ComponentModel.DataAnnotations;

namespace SupertexWeb.Models
{
    public class AboutPage
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề trang giới thiệu")]
        public string? Title { get; set; } = "Năng Lực & Quy Mô Sản Xuất Supertex";

        [Display(Name = "Phụ đề / Slogan ngắn")]
        public string? Subtitle { get; set; } = "Hệ thống nhà máy hiện đại cùng quy trình kiểm soát chất lượng chuẩn quốc tế";

        [Display(Name = "Nội dung bài viết chi tiết (Hỗ trợ HTML hoặc đoạn văn)")]
        public string? MainContent { get; set; } = "Được thành lập với định hướng chuyên môn hóa cao trong ngành dệt may, Supertex không ngừng đầu tư công nghệ hiện đại và nâng cao tay nghề nhân sự. Chúng tôi tự hào cung cấp các sản phẩm tất đa dạng chủng loại đáp ứng những tiêu chuẩn khắt khe nhất của đối tác trong nước và quốc tế...";

        [Display(Name = "Đường dẫn ảnh minh chứng chính (Nhà máy/Dây chuyền)")]
        public string? MainImageUrl { get; set; } = "/images/partners/factory-line.jpg";

        [Display(Name = "Tiêu chuẩn 1 (Ví dụ: ISO 9001:2015)")]
        public string? Standard1Title { get; set; } = "ISO 9001:2015";
        [Display(Name = "Mô tả tiêu chuẩn 1")]
        public string? Standard1Desc { get; set; } = "Hệ thống quản lý chất lượng toàn diện từ nguyên liệu đầu vào đến thành phẩm.";

        [Display(Name = "Tiêu chuẩn 2 (Ví dụ: OEKO-TEX Standard 100)")]
        public string? Standard2Title { get; set; } = "OEKO-TEX Standard 100";
        [Display(Name = "Mô tả tiêu chuẩn 2")]
        public string? Standard2Desc { get; set; } = "Chứng nhận an toàn sinh thái dệt may, không chứa chất độc hại cho da.";

        [Display(Name = "Tiêu chuẩn 3 (Ví dụ: BSCI / Social Audit)")]
        public string? Standard3Title { get; set; } = "BSCI Compliance";
        [Display(Name = "Mô tả tiêu chuẩn 3")]
        public string? Standard3Desc { get; set; } = "Đảm bảo tiêu chuẩn trách nhiệm xã hội và môi trường làm việc an toàn.";
    }
}