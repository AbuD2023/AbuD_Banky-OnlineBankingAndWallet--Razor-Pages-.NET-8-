namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج تفاصيل العملية المالية في لوحة التحكم
    /// </summary>
    public class TransactionModel
    {
        public Guid Id { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TypeNameAr { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusNameAr { get; set; } = string.Empty;
        public string? SenderDisplayName { get; set; }
        public string? SenderDisplayPhone { get; set; }
        public string? ReceiverDisplayName { get; set; }
        public Guid? SenderClientId { get; set; }
        public string? SenderRealFullName { get; set; }
        public string? SenderRealPhone { get; set; }
        public Guid? ReceiverClientId { get; set; }
        public string? ReceiverRealFullName { get; set; }
        public string? ReceiverRealPhone { get; set; }
        public string? PosName { get; set; }
        public string? Note { get; set; }
        public bool IsIncoming { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نموذج فلترة العمليات في لوحة التحكم
    /// </summary>
    public class TransactionFilterModel
    {
        public string? CurrencyCode { get; set; }
        public string? Type { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
