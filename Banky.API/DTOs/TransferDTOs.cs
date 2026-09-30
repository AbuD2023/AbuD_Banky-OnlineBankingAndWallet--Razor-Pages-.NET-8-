using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج التحويل المالي لمشترك عبر رقم الهاتف
    /// </summary>
    public class TransferByPhoneDto
    {
        /// <summary>
        /// رقم هاتف المستلم (الرقم الأساسي المسجل)
        /// </summary>
        [Required(ErrorMessage = "رقم هاتف المستلم مطلوب")]
        public string ReceiverPhone { get; set; } = string.Empty;

        /// <summary>
        /// رمز عملة التحويل (YER, SAR, USD)
        /// </summary>
        [Required(ErrorMessage = "رمز العملة مطلوب")]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>
        /// المبلغ المحول
        /// </summary>
        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        /// <summary>
        /// بيان أو ملاحظات التحويل
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// نموذج الدفع والشراء عبر نقطة بيع (POS)
    /// </summary>
    public class PosPaymentDto
    {
        /// <summary>
        /// رمز نقطة البيع أو كود الـ QR (مثال: POS-100200)
        /// </summary>
        [Required(ErrorMessage = "كود نقطة البيع مطلوب")]
        public string PosCode { get; set; } = string.Empty;

        /// <summary>
        /// رمز العملة للدفع
        /// </summary>
        [Required(ErrorMessage = "رمز العملة مطلوب")]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>
        /// قيمة المشتريات / المبلغ المدفوع
        /// </summary>
        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        /// <summary>
        /// ملاحظات أو رقم الفاتورة لدى التاجر
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// استجابة الاستعلام عن مستلم برقم الهاتف قبل تأكيد التحويل مع بيان الرسوم
    /// </summary>
    public class RecipientLookupResponseDto
    {
        public Guid ClientId { get; set; }
        /// <summary>
        /// اسم المستلم (الاسم الكامل أو الحروف الأولى المشفرة حسب إعدادات الخصوصية)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsNameMasked { get; set; }
        public bool HasActiveWalletInCurrency { get; set; }

        /// <summary>
        /// تفاصيل الرسوم والعمولة التقديرية
        /// </summary>
        public decimal EstimatedFee { get; set; } = 0.00m;
        public string? FeeDescription { get; set; }
    }

    /// <summary>
    /// استجابة الاستعلام عن نقطة بيع قبل تأكيد الدفع مع بيان الرسوم
    /// </summary>
    public class PosLookupResponseDto
    {
        public Guid PosId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PosCode { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Address { get; set; }
        public string MerchantDisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>
        /// تفاصيل الرسوم والعمولة التقديرية
        /// </summary>
        public decimal EstimatedFee { get; set; } = 0.00m;
        public string? FeeDescription { get; set; }
    }

    /// <summary>
    /// نموذج التحويل والمصارفة بين محافظ العميل الخاصة
    /// </summary>
    public class SelfExchangeDto
    {
        [Required(ErrorMessage = "العملة المصدر مطلوبة")]
        public string FromCurrencyCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "العملة الهدف مطلوبة")]
        public string ToCurrencyCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0.01, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// نموذج حساب سعر الصرف والمبلغ المحول مع بيان العمولة
    /// </summary>
    public class ExchangeCalculationResultDto
    {
        public string FromCurrencyCode { get; set; } = string.Empty;
        public string ToCurrencyCode { get; set; } = string.Empty;
        public decimal SourceAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal Fee { get; set; } = 0.00m;
        public decimal TotalSourceAmountWithFee { get; set; }
        public string? FeeDescription { get; set; }
    }
}
