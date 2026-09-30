using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان إعدادات الرسوم والعمولات للعمليات المالية (FeeSetting)
    /// يتيح لمدير النظام (Admin) تحديد نسبة مئوية أو مبالغ ثابتة أو كلاهما لكل نوع عملية مصرفية
    /// مع إمكانية تحديد حد أدنى وأعلى للعمولة، وتفعيل أو إيقاف احتساب الرسوم
    /// </summary>
    [Table("fee_settings")]
    public class FeeSetting
    {
        /// <summary>
        /// المعرف الرقمي الفريد لإعداد الرسوم
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// نوع العملية المصرفية المرتبطة بهذا الإعداد:
        /// - "TransferByPhone" : التحويل بين المشتركين برقم الهاتف
        /// - "SelfExchange"    : الصرف والمصارفة والتحويل بين الحسابات الذاتية
        /// - "PosPayment"      : المشتريات والدفع لنقاط البيع
        /// - "Deposit"         : الإيداع وتغذية الحساب
        /// - "Withdrawal"      : السحب النقدي
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("operation_type")]
        public string OperationType { get; set; } = "TransferByPhone";

        /// <summary>
        /// رمز العملة المستهدفة (إذا كان الإعداد خاصاً بعملة معينة مثل YER أو SAR)،
        /// أو يمكن أن يكون null/فارغاً لينطبق كإعداد افتراضي عام لكافة العملات
        /// </summary>
        [MaxLength(10)]
        [Column("currency_code")]
        public string? CurrencyCode { get; set; }

        /// <summary>
        /// اسم وعنوان الإعداد باللغة العربية الظاهر في لوحة التحكم
        /// </summary>
        [Required]
        [MaxLength(150)]
        [Column("name_ar")]
        public string NameAr { get; set; } = string.Empty;

        /// <summary>
        /// وصف تفصيلي يوضح كيفية تطبيق هذه الرسوم وسياسة البنك
        /// </summary>
        [MaxLength(500)]
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// نوع احتساب الرسوم:
        /// - "Percentage"          : نسبة مئوية فقط من قيمة المبلغ
        /// - "Fixed"               : مبلغ ثابت مقطوع لكل عملية
        /// - "PercentageAndFixed"  : نسبة مئوية مضافاً إليها مبلغ ثابت
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("fee_type")]
        public string FeeType { get; set; } = "Percentage";

        /// <summary>
        /// النسبة المئوية المقتطعة من المبلغ (مثال: 0.5 تعني 0.5%)
        /// </summary>
        [Column("percentage", TypeName = "decimal(8,4)")]
        public decimal Percentage { get; set; } = 0.0000m;

        /// <summary>
        /// المبلغ المالي الثابت المقطوع لكل عملية
        /// </summary>
        [Column("fixed_amount", TypeName = "decimal(18,2)")]
        public decimal FixedAmount { get; set; } = 0.00m;

        /// <summary>
        /// الحد الأدنى للرسوم المقتطعة (إذا كانت النسبة أقل من هذا الحد، يتم فرض الحد الأدنى)
        /// القيمة 0 تعني عدم وجود حد أدنى
        /// </summary>
        [Column("min_fee", TypeName = "decimal(18,2)")]
        public decimal MinFee { get; set; } = 0.00m;

        /// <summary>
        /// الحد الأقصى للرسوم المقتطعة (إذا تجاوزت الرسوم هذا المبلغ، يتم سقفها عند هذا الحد)
        /// القيمة 0 تعني عدم وجود حد أقصى
        /// </summary>
        [Column("max_fee", TypeName = "decimal(18,2)")]
        public decimal MaxFee { get; set; } = 0.00m;

        /// <summary>
        /// هل هذا الإعداد مفعل حالياً ويتم اقتطاع الرسوم بناءً عليه؟
        /// في حال التعطيل (false) تصبح الرسوم مجانية (0.00) لتلك العملية
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاريخ وتوقيت آخر تعديل على الإعداد
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// اسم أو معرف مدير النظام (Admin) الذي قام بآخر تعديل
        /// </summary>
        [MaxLength(100)]
        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// دالة احتساب الرسوم الفعلية بناءً على القواعد المحددة في هذا الإعداد
        /// </summary>
        /// <param name="amount">المبلغ المراد تحويله أو دفعه</param>
        /// <returns>قيمة الرسوم والعمولة الناتجة</returns>
        public decimal CalculateFee(decimal amount)
        {
            if (!IsActive || amount <= 0)
            {
                return 0.00m;
            }

            decimal calculatedFee = 0.00m;

            switch (FeeType)
            {
                case "Percentage":
                    calculatedFee = amount * (Percentage / 100.00m);
                    break;

                case "Fixed":
                    calculatedFee = FixedAmount;
                    break;

                case "PercentageAndFixed":
                    calculatedFee = (amount * (Percentage / 100.00m)) + FixedAmount;
                    break;

                default:
                    calculatedFee = amount * (Percentage / 100.00m);
                    break;
            }

            // تطبيق الحد الأدنى للرسوم
            if (MinFee > 0 && calculatedFee < MinFee)
            {
                calculatedFee = MinFee;
            }

            // تطبيق الحد الأقصى للرسوم
            if (MaxFee > 0 && calculatedFee > MaxFee)
            {
                calculatedFee = MaxFee;
            }

            // تقريب الناتج لأقرب خانتين عشريتين
            return Math.Round(calculatedFee, 2, MidpointRounding.AwayFromZero);
        }
    }
}
