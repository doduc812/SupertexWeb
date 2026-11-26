namespace SupertexWeb.Models
{
    public class CustomerRequest
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty; // Bắt buộc
        public string Email { get; set; } = string.Empty;        // Bắt buộc

        public string? CompanyName { get; set; }
        public string? Country { get; set; } = "Vietnam";

        // Cho phép để trống (null) để khách nước ngoài không cần nhập SĐT vẫn gửi được
        public string? Phone { get; set; }

        // Phân loại: "Quote" hoặc "Sample"
        public string RequestType { get; set; } = "Quote";

        // Dùng mã sản phẩm (Code) chuẩn xác tuyệt đối
        public string? ProductCode { get; set; }

        public string? Quantity { get; set; }
        public string? ShippingAddress { get; set; }

        public string? Message { get; set; }
        public string? Note
        {
            get => Message;
            set => Message = value;
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsProcessed { get; set; } = false;
    }
}