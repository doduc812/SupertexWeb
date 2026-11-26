using Ganss.Xss;

namespace SupertexWeb.Services
{
    /// <summary>
    /// Làm sạch HTML do trình soạn thảo (Summernote) tạo ra trước khi hiển thị cho khách:
    /// loại bỏ &lt;script&gt;, sự kiện onclick/onerror..., liên kết javascript:, v.v.
    /// Vẫn giữ lại định dạng thông thường: đoạn văn, tiêu đề, in đậm/nghiêng, danh sách, ảnh, liên kết.
    /// Video nhúng (iframe) chỉ được phép từ YouTube và Vimeo.
    /// </summary>
    public static class RichTextSanitizer
    {
        private static readonly string[] AllowedIframeHosts =
        {
            "www.youtube.com",
            "youtube.com",
            "www.youtube-nocookie.com",
            "player.vimeo.com"
        };

        private static readonly HtmlSanitizer Sanitizer = Create();
        private static readonly object Gate = new();

        public static string? Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return html;
            }

            // HtmlSanitizer không đảm bảo an toàn khi nhiều luồng dùng chung một đối tượng -> khóa lại cho chắc
            lock (Gate)
            {
                return Sanitizer.Sanitize(html);
            }
        }

        private static HtmlSanitizer Create()
        {
            var sanitizer = new HtmlSanitizer();

            // Nút "Video" của Summernote chèn thẻ iframe
            sanitizer.AllowedTags.Add("iframe");
            sanitizer.AllowedAttributes.Add("allowfullscreen");
            sanitizer.AllowedAttributes.Add("frameborder");

            // Mọi đường dẫn đi qua đây. Iframe chỉ cho phép trỏ tới YouTube/Vimeo qua HTTPS
            sanitizer.FilterUrl += (sender, e) =>
            {
                if (!string.Equals(e.Tag.LocalName, "iframe", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var url = e.SanitizedUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                // Summernote thường tạo địa chỉ dạng //www.youtube.com/embed/...
                if (url.StartsWith("//", StringComparison.Ordinal))
                {
                    url = "https:" + url;
                }

                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                    uri.Scheme == Uri.UriSchemeHttps &&
                    AllowedIframeHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                {
                    e.SanitizedUrl = uri.ToString();
                }
                else
                {
                    e.SanitizedUrl = null; // bỏ src -> iframe vô hại
                }
            };

            return sanitizer;
        }
    }
}
