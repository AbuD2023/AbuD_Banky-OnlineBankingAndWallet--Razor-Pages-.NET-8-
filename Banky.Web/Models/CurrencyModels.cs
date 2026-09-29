using System.ComponentModel.DataAnnotations;

namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج بيانات العملة المعروض في لوحة التحكم
    /// </summary>
    public class CurrencyModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نموذج إضافة عملة جديدة من لوحة التحكم
    /// </summary>
    public class CurrencyCreateViewModel
    {
        [Required(ErrorMessage = "رمز العملة مطلوب (مثال: YER, SAR, USD)")]
        [MaxLength(10)]
        [Display(Name = "رمز العملة")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العملة بالعربية مطلوب")]
        [MaxLength(100)]
        [Display(Name = "الاسم بالعربية")]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العملة بالإنجليزية مطلوب")]
        [MaxLength(100)]
        [Display(Name = "الاسم بالإنجليزية")]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز العرض مطلوب (مثال: ر.ي, $, €)")]
        [MaxLength(10)]
        [Display(Name = "رمز العرض")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "مفعلة")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "سعر الصرف المرجعي")]
        public decimal ExchangeRate { get; set; } = 1.00m;
    }

    /// <summary>
    /// نموذج تعديل بيانات عملة موجودة
    /// </summary>
    public class CurrencyEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العملة بالعربية مطلوب")]
        [Display(Name = "الاسم بالعربية")]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العملة بالإنجليزية مطلوب")]
        [Display(Name = "الاسم بالإنجليزية")]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز العرض مطلوب")]
        [Display(Name = "رمز العرض")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "مفعلة")]
        public bool IsActive { get; set; }

        [Display(Name = "سعر الصرف المرجعي")]
        public decimal ExchangeRate { get; set; }
    }
}
