namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج نقطة البيع المعروض في لوحة التحكم الإدارية
    /// </summary>
    public class AdminPosPointModel
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PosCode { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantPhone { get; set; } = string.Empty;
        public string MerchantEmail { get; set; } = string.Empty;
        public string MerchantKycStatus { get; set; } = string.Empty;
        public decimal TotalSalesAmount { get; set; }
        public int TotalSalesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
