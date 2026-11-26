using System.ComponentModel.DataAnnotations;

namespace SupertexWeb.Models
{
    public class HomeAboutSection
    {
        public int Id { get; set; }

        [Display(Name = "Nhãn nhỏ trên tiêu đề (Badge)")]
        public string? BadgeText { get; set; } = "Giới thiệu Supertex";

        [Display(Name = "Tiêu đề chính")]
        public string? Title { get; set; } = "Đối tác sản xuất tất cho các thương hiệu quốc tế";

        [Display(Name = "Đoạn văn mô tả ngắn")]
        public string? Description { get; set; } = "Được thành lập với định hướng chuyên môn hóa cao trong ngành dệt may, Supertex không ngừng đầu tư công nghệ hiện đại và nâng cao tay nghề nhân sự. Chúng tôi tự hào cung cấp các sản phẩm tất đa dạng chủng loại đáp ứng những tiêu chuẩn khắt khe nhất của đối tác trong nước và quốc tế.";

        [Display(Name = "Loại hiển thị (Ảnh tĩnh hoặc Video)")]
        public bool IsVideoMode { get; set; } = false; // false = Ảnh, true = Video

        [Display(Name = "Đường dẫn Hình ảnh minh họa")]
        public string? ImageUrl { get; set; } = "/images/partners/factory-line.jpg";

        [Display(Name = "Đường dẫn Video (Link YouTube embed hoặc file .mp4)")]
        public string? VideoUrl { get; set; } = "";
    }
}