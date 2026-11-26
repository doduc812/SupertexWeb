using System.ComponentModel.DataAnnotations;

namespace SupertexWeb.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [Display(Name = "Tiêu đề bài viết")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Đường dẫn thân thiện (Slug)")]
        public string? Slug { get; set; }

        [Display(Name = "Mô tả ngắn (Sô-lê-gân / Tóm tắt)")]
        public string? Summary { get; set; }

        [Display(Name = "Nội dung chi tiết bài viết")]
        public string? Content { get; set; }

        [Display(Name = "Hình ảnh minh họa")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Ngày đăng")]
        public DateTime PublishedDate { get; set; } = DateTime.Now;

        [Display(Name = "Hiển thị công khai")]
        public bool IsPublished { get; set; } = true;
    }
}