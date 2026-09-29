using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج فتح / إنشاء محفظة جديدة بعملة معينة
    /// </summary>
    public class CreateWalletDto
    {
        /// <summary>
        /// رمز العملة المراد فتح محفظة بها (YER, SAR, USD, إلخ)
        /// </summary>
        [Required(ErrorMessage = "رمز العملة مطلوب")]
        public string CurrencyCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج الإيداع المالي في المحفظة
    /// </summary>
    public class DepositDto
    {
        /// <summary>
        /// رقم حساب المحفظة المراد الإيداع فيها
        /// </summary>
        [Required(ErrorMessage = "رقم حساب المحفظة مطلوب")]
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// المبلغ المراد إيداعه
        /// </summary>
        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        /// <summary>
        /// بيان أو ملاحظات الإيداع
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// نموذج استرجاع تفاصيل المحفظة المالية
    /// </summary>
    public class WalletResponseDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyNameAr { get; set; } = string.Empty;
        public string CurrencyNameEn { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
