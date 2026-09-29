using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج إحصائيات لوحة التحكم الإدارية
    /// </summary>
    public class AdminDashboardStatsDto
    {
        public int TotalClientsCount { get; set; }
        public int PendingKycCount { get; set; }
        public int ApprovedKycCount { get; set; }
        public int TotalPosCount { get; set; }
        public int TotalTransactionsCount { get; set; }
        public decimal TotalTransferredAmountYer { get; set; }
        public decimal TotalTransferredAmountSar { get; set; }
        public decimal TotalTransferredAmountUsd { get; set; }
        public List<RecentKycRequestDto> RecentKycRequests { get; set; } = new();
        public List<TransactionResponseDto> RecentTransactions { get; set; } = new();
    }

    /// <summary>
    /// نموذج طلب توثيق الهوية المعروض للإدارة
    /// </summary>
    public class RecentKycRequestDto
    {
        public Guid ClientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public string? KycIdFront { get; set; }
        public string? KycIdBack { get; set; }
        public DateTime? KycSubmittedAt { get; set; }
    }

    /// <summary>
    /// نموذج اتخاذ قرار المراجعة على طلب التوثيق (قبول أو رفض)
    /// </summary>
    public class KycReviewDto
    {
        /// <summary>
        /// معرف العميل
        /// </summary>
        [Required]
        public Guid ClientId { get; set; }

        /// <summary>
        /// هل تم قبول التوثيق (true للقبول، false للرفض)
        /// </summary>
        [Required]
        public bool IsApproved { get; set; }

        /// <summary>
        /// سبب الرفض في حال كان القرار بالرفض
        /// </summary>
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// نموذج إنشاء عملة جديدة من لوحة التحكم
    /// </summary>
    public class CurrencyCreateDto
    {
        [Required(ErrorMessage = "رمز العملة مطلوب (مثال: YER, SAR)")]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العملة بالعربية مطلوب")]
        [MaxLength(100)]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العملة بالإنجليزية مطلوب")]
        [MaxLength(100)]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز العرض مطلوب (مثال: ر.ي, $)")]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public decimal ExchangeRate { get; set; } = 1.00m;
    }

    /// <summary>
    /// نموذج تعديل عملة
    /// </summary>
    public class CurrencyUpdateDto
    {
        [Required(ErrorMessage = "اسم العملة بالعربية مطلوب")]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العملة بالإنجليزية مطلوب")]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز العرض مطلوب")]
        public string Symbol { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public decimal ExchangeRate { get; set; }
    }

    /// <summary>
    /// نموذج حظر أو فك حظر عميل
    /// </summary>
    public class BlockClientDto
    {
        [Required]
        public Guid ClientId { get; set; }

        public bool IsBlocked { get; set; }

        public string? BlockedMessage { get; set; }
    }
}
