using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان العملات المعتمدة في النظام المالي
    /// يمكن لإدارة النظام إضافة عملات جديدة وتفعيلها أو تعطيلها
    /// </summary>
    [Table("currencies")]
    public class Currency
    {
        /// <summary>
        /// المعرف الرقمي للعملة
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// الرمز القياسي للعملة (مثل: YER, SAR, USD, EUR)
        /// </summary>
        [Required(ErrorMessage = "رمز العملة مطلوب")]
        [MaxLength(10)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// اسم العملة باللغة العربية (مثل: ريال يمني، ريال سعودي، دولار أمريكي)
        /// </summary>
        [Required(ErrorMessage = "اسم العملة بالعربية مطلوب")]
        [MaxLength(100)]
        [Column("name_ar")]
        public string NameAr { get; set; } = string.Empty;

        /// <summary>
        /// اسم العملة باللغة الإنجليزية (مثل: Yemeni Rial, Saudi Riyal, US Dollar)
        /// </summary>
        [Required(ErrorMessage = "اسم العملة بالإنجليزية مطلوب")]
        [MaxLength(100)]
        [Column("name_en")]
        public string NameEn { get; set; } = string.Empty;

        /// <summary>
        /// رمز العرض للعملة (مثل: ر.ي, ر.س, $)
        /// </summary>
        [Required(ErrorMessage = "رمز العرض مطلوب")]
        [MaxLength(10)]
        [Column("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// هل العملة مفعلة ومتاحة للمستخدمين لإنشاء محافظ بها
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// سعر التحويل المرجعي مقارنة بالعملة الأساسية (لأغراض الإحصاءات)
        /// </summary>
        [Column("exchange_rate", TypeName = "decimal(18,4)")]
        public decimal ExchangeRate { get; set; } = 1.0000m;

        /// <summary>
        /// تاريخ إضافة العملة للنظام
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region العلاقات (Navigation Properties)

        /// <summary>
        /// المحافظ المالية المنشأة بهذه العملة
        /// </summary>
        public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

        #endregion
    }
}
