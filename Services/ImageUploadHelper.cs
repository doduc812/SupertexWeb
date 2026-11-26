namespace SupertexWeb.Services
{
    public sealed record ImageSaveResult(bool Success, string? Url, string? Error)
    {
        public static ImageSaveResult Ok(string url) => new(true, url, null);
        public static ImageSaveResult Fail(string error) => new(false, null, error);
    }

    /// <summary>
    /// Nơi DUY NHẤT xử lý lưu / xóa ảnh upload trong wwwroot/images.
    /// - Chỉ nhận JPG, PNG, WEBP, GIF (kiểm tra theo NỘI DUNG file, không tin tên file hay Content-Type).
    /// - Đuôi file do server quyết định theo nội dung, không lấy từ tên file người dùng gửi.
    /// - Giới hạn dung lượng.
    /// - Không nhận SVG/HTML vì có thể chứa script (XSS).
    /// </summary>
    public static class ImageUploadHelper
    {
        public const int MaxFileBytes = 5 * 1024 * 1024;      // 5 MB mỗi ảnh
        public const int MaxRequestBytes = 6 * 1024 * 1024;   // cho phép thêm phần dư của form multipart

        private const string InvalidTypeMessage =
            "File không phải ảnh hợp lệ. Chỉ chấp nhận ảnh JPG, PNG, WEBP hoặc GIF.";

        public static async Task<ImageSaveResult> SaveAsync(
            IFormFile file, string webRootPath, string subFolder, string namePrefix)
        {
            if (file.Length == 0)
            {
                return ImageSaveResult.Fail("File ảnh bị rỗng.");
            }

            if (file.Length > MaxFileBytes)
            {
                return ImageSaveResult.Fail($"Ảnh quá lớn. Dung lượng tối đa là {MaxFileBytes / 1024 / 1024} MB.");
            }

            using var ms = new MemoryStream((int)file.Length);
            await file.CopyToAsync(ms);
            return await SaveBytesAsync(ms.ToArray(), webRootPath, subFolder, namePrefix);
        }

        public static async Task<ImageSaveResult> SaveBytesAsync(
            byte[] data, string webRootPath, string subFolder, string namePrefix)
        {
            if (data.Length == 0)
            {
                return ImageSaveResult.Fail("File ảnh bị rỗng.");
            }

            if (data.Length > MaxFileBytes)
            {
                return ImageSaveResult.Fail($"Ảnh quá lớn. Dung lượng tối đa là {MaxFileBytes / 1024 / 1024} MB.");
            }

            var extension = DetectExtension(data);
            if (extension == null)
            {
                return ImageSaveResult.Fail(InvalidTypeMessage);
            }

            var folder = Path.Combine(webRootPath, "images", subFolder);
            Directory.CreateDirectory(folder);

            var fileName = $"{namePrefix}{Guid.NewGuid():N}".Substring(0, namePrefix.Length + 12) + extension;
            await File.WriteAllBytesAsync(Path.Combine(folder, fileName), data);

            return ImageSaveResult.Ok($"/images/{subFolder}/{fileName}");
        }

        /// <summary>Giải mã chuỗi Base64 (có hoặc không có tiền tố "data:image/...;base64,").</summary>
        public static bool TryDecodeBase64(string input, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();

            // Base64 lớn hơn dữ liệu gốc khoảng 4/3 lần
            if (string.IsNullOrWhiteSpace(input) || input.Length > (MaxFileBytes / 3 * 4) + 1024)
            {
                return false;
            }

            var comma = input.IndexOf(',');
            var payload = comma >= 0 ? input[(comma + 1)..] : input;

            try
            {
                bytes = Convert.FromBase64String(payload);
                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Xóa ảnh cũ một cách an toàn: chỉ xóa file nằm đúng trong wwwroot/images/{subFolder}
        /// và (nếu có) tên file bắt đầu bằng filePrefix. Ảnh mặc định của hệ thống không bị đụng tới.
        /// </summary>
        public static void DeleteManaged(string? url, string webRootPath, string subFolder, string filePrefix = "")
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            var expectedUrlStart = $"/images/{subFolder}/";
            if (!url.StartsWith(expectedUrlStart, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileName(url);
            if (string.IsNullOrEmpty(fileName) ||
                (filePrefix.Length > 0 && !fileName.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var folder = Path.GetFullPath(Path.Combine(webRootPath, "images", subFolder));
            var fullPath = Path.GetFullPath(Path.Combine(folder, fileName));

            if (fullPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private static string? DetectExtension(byte[] d)
        {
            // JPEG: FF D8 FF
            if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF)
            {
                return ".jpg";
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (d.Length >= 8 &&
                d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47 &&
                d[4] == 0x0D && d[5] == 0x0A && d[6] == 0x1A && d[7] == 0x0A)
            {
                return ".png";
            }

            // GIF: "GIF87a" hoặc "GIF89a"
            if (d.Length >= 6 &&
                d[0] == 'G' && d[1] == 'I' && d[2] == 'F' && d[3] == '8' &&
                (d[4] == '7' || d[4] == '9') && d[5] == 'a')
            {
                return ".gif";
            }

            // WEBP: "RIFF" .... "WEBP"
            if (d.Length >= 12 &&
                d[0] == 'R' && d[1] == 'I' && d[2] == 'F' && d[3] == 'F' &&
                d[8] == 'W' && d[9] == 'E' && d[10] == 'B' && d[11] == 'P')
            {
                return ".webp";
            }

            return null;
        }
    }
}
