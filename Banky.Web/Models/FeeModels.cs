using System.ComponentModel.DataAnnotations;

namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج عرض إعدادات الرسوم والعمولات المصرفية في لوحة التحكم
    /// </summary>
    public class FeeSettingViewModel
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
    /// نموذج تعديل إعداد الرسوم والعمولة من قبل مدير النظام
    /// </summary>
    public class EditFeeSettingViewModel
    {
        public int Id { get; set; }

        public string OperationType { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع احتساب الرسوم مطلوب")]
        public string FeeType { get; set; } = "Percentage";

        [Range(0, 100, ErrorMessage = "النسبة المئوية يجب أن تكون بين 0 و 100")]
        [Display(Name = "النسبة المئوية (%)")]
        public decimal Percentage { get; set; }

        [Range(0, 1000000, ErrorMessage = "المبلغ الثابت يجب أن يكون أكبر أو يساوي 0")]
        [Display(Name = "المبلغ الثابت المقطوع")]
        public decimal FixedAmount { get; set; }

        [Range(0, 1000000, ErrorMessage = "الحد الأدنى يجب أن يكون أكبر أو يساوي 0")]
        [Display(Name = "الحد الأدنى للرسوم")]
        public decimal MinFee { get; set; }

        [Range(0, 1000000, ErrorMessage = "الحد الأقصى يجب أن يكون أكبر أو يساوي 0")]
        [Display(Name = "الحد الأقصى للرسوم")]
        public decimal MaxFee { get; set; }

        [Display(Name = "حالة التفعيل")]
        public bool IsActive { get; set; }

        [MaxLength(500, ErrorMessage = "الوصف لا يجب أن يتجاوز 500 حرف")]
        [Display(Name = "بيان / وصف السياسة")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// نموذج إنشاء إعداد ورسوم جديدة لعملية وعملة محددة من قبل المدير
    /// </summary>
    public class CreateFeeSettingViewModel
    {
        [Required(ErrorMessage = "نوع العملية المصرفية مطلوب")]
        [Display(Name = "نوع العملية المصرفية")]
        public string OperationType { get; set; } = "TransferByPhone";

        [Display(Name = "العملة المستهدفة")]
        public string? CurrencyCode { get; set; } // فارغ لكافة العملات أو رمز العملة مثل YER, SAR, USD

        [Required(ErrorMessage = "اسم السياسة بالعربية مطلوب")]
        [MaxLength(150, ErrorMessage = "اسم السياسة لا يجب أن يتجاوز 150 حرف")]
        [Display(Name = "اسم السياسة بالعربية")]
        public string NameAr { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "الوصف لا يجب أن يتجاوز 500 حرف")]
        [Display(Name = "بيان / وصف السياسة")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "طريقة احتساب الرسوم مطلوبة")]
        [Display(Name = "طريقة احتساب الرسوم")]
        public string FeeType { get; set; } = "Percentage";

        [Range(0, 100, ErrorMessage = "النسبة المئوية يجب أن تكون بين 0 و 100")]
        [Display(Name = "النسبة المئوية (%)")]
        public decimal Percentage { get; set; }

        [Range(0, 100000000, ErrorMessage = "المبلغ الثابت يجب أن يكون أكبر أو يساوي 0")]
        [Display(Name = "المبلغ الثابت المقطوع")]
        public decimal FixedAmount { get; set; }

        [Range(0, 100000000, ErrorMessage = "الحد الأدنى يجب أن يكون أكبر أو يساوي 0")]
        [Display(Name = "الحد الأدنى للرسوم")]
        public decimal MinFee { get; set; }

        [Range(0, 100000000, ErrorMessage = "الحد الأقصى يجب أن يكون أكبر أو يساوي 0")]
        [Display(Name = "الحد الأقصى للرسوم")]
        public decimal MaxFee { get; set; }

        [Display(Name = "حالة التفعيل")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// نموذج صفحة إدارة الرسوم والعمولات متضمناً قائمة القواعد وقائمة العملات المتاحة
    /// </summary>
    public class FeeIndexViewModel
    {
        public List<FeeSettingViewModel> Fees { get; set; } = new();
        public List<CurrencyModel> Currencies { get; set; } = new();
        public string? SelectedCurrencyFilter { get; set; }
        public string? SelectedOperationFilter { get; set; }
    }

    /// <summary>
    /// نموذج معاينة واختبار حاسبة الرسوم
    /// </summary>
    public class FeePreviewViewModel
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
