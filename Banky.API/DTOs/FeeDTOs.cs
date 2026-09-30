using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// كائن نقل بيانات عرض إعدادات الرسوم والعمولات (FeeSettingResponseDto)
    /// يعيد لمدير النظام والتطبيق تفاصيل الرسوم والنسب وطريقة الاحتساب
    /// </summary>
    public class FeeSettingResponseDto
    {
        public int Id { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string OperationTypeNameAr { get; set; } = string.Empty;
        public string? CurrencyCode { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FeeType { get; set; } = "Percentage";
        public string FeeTypeNameAr { get; set; } = "نسبة مئوية";
        public decimal Percentage { get; set; }
        public decimal FixedAmount { get; set; }
        public decimal MinFee { get; set; }
        public decimal MaxFee { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// كائن نقل بيانات إنشاء قاعدة رسوم جديدة لعملية وعملة محددة
    /// </summary>
    public class CreateFeeSettingDto
    {
        [Required(ErrorMessage = "نوع العملية المصرفية مطلوب")]
        public string OperationType { get; set; } = "TransferByPhone";

        /// <summary>
        /// رمز العملة المستهدفة (مثلاً YER, SAR, USD) أو null/فارغ لينطبق على كافة العملات
        /// </summary>
        public string? CurrencyCode { get; set; }

        [Required(ErrorMessage = "اسم السياسة بالعربية مطلوب")]
        [MaxLength(150, ErrorMessage = "اسم السياسة لا يجب أن يتجاوز 150 حرف")]
        public string NameAr { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "الوصف لا يجب أن يتجاوز 500 حرف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "طريقة احتساب الرسوم مطلوبة")]
        public string FeeType { get; set; } = "Percentage";

        [Range(0, 100, ErrorMessage = "النسبة المئوية يجب أن تكون بين 0 و 100")]
        public decimal Percentage { get; set; }

        [Range(0, 100000000, ErrorMessage = "المبلغ الثابت يجب أن يكون أكبر أو يساوي 0")]
        public decimal FixedAmount { get; set; }

        [Range(0, 100000000, ErrorMessage = "الحد الأدنى يجب أن يكون أكبر أو يساوي 0")]
        public decimal MinFee { get; set; }

        [Range(0, 100000000, ErrorMessage = "الحد الأقصى يجب أن يكون أكبر أو يساوي 0")]
        public decimal MaxFee { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// كائن نقل بيانات تعديل إعدادات الرسوم والعمولات من لوحة التحكم
    /// </summary>
    public class UpdateFeeSettingDto
    {
        [MaxLength(150, ErrorMessage = "اسم السياسة لا يجب أن يتجاوز 150 حرف")]
        public string? NameAr { get; set; }

        [Required(ErrorMessage = "نوع احتساب الرسوم مطلوب")]
        public string FeeType { get; set; } = "Percentage";

        [Range(0, 100, ErrorMessage = "النسبة المئوية يجب أن تكون بين 0 و 100")]
        public decimal Percentage { get; set; }

        [Range(0, 100000000, ErrorMessage = "المبلغ الثابت يجب أن يكون أكبر أو يساوي 0")]
        public decimal FixedAmount { get; set; }

        [Range(0, 100000000, ErrorMessage = "الحد الأدنى يجب أن يكون أكبر أو يساوي 0")]
        public decimal MinFee { get; set; }

        [Range(0, 100000000, ErrorMessage = "الحد الأقصى يجب أن يكون أكبر أو يساوي 0")]
        public decimal MaxFee { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(500, ErrorMessage = "الوصف لا يجب أن يتجاوز 500 حرف")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// كائن نقل بيانات معاينة واحتساب الرسوم قبل إتمام العملية (FeePreviewDto)
    /// يتيح لتطبيق الهاتف طلب معاينة الرسوم وإظهار ملخص الحسبة للعميل
    /// </summary>
    public class FeeCalculationPreviewDto
    {
        public string OperationType { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public string CurrencyCode { get; set; } = "YER";
        public string CurrencySymbol { get; set; } = "ر.ي";
        public decimal FeeAmount { get; set; }
        public decimal TotalAmountToDebit { get; set; }
        public decimal EffectivePercentage { get; set; }
        public string FeeRuleDescription { get; set; } = string.Empty;
        public bool IsFeeWaived { get; set; }
    }
}
