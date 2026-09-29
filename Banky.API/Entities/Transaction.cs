using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان الحركة المالية / العملية المصرفية (Transaction)
    /// يسجل كافة تفاصيل التحويلات بين الحسابات، الدفع لنقاط البيع، وعمليات الإيداع والسحب
    /// </summary>
    [Table("transactions")]
    public class Transaction
    {
        /// <summary>
        /// المعرف الفريد للعملية (GUID)
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// الرقم المرجعي الفريد للعملية الظاهر في الإيصالات والرسائل (مثال: TRX-2026-981245)
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("transaction_number")]
        public string TransactionNumber { get; set; } = string.Empty;

        /// <summary>
        /// نوع العملية المالية:
        /// "TransferByPhone" تحويل لمشترك برقم الهاتف
        /// "PosPayment" شراء / دفع عبر نقطة بيع
        /// "Deposit" إيداع وتغذية رصيد
        /// "Withdrawal" سحب نقدي
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("type")]
        public string Type { get; set; } = "TransferByPhone";

        /// <summary>
        /// معرف العميل المرسل / الدافع (null في حال الإيداع المباشر)
        /// </summary>
        [Column("sender_client_id")]
        public Guid? SenderClientId { get; set; }

        /// <summary>
        /// معرف العميل المستلم / التاجر (null في حال السحب)
        /// </summary>
        [Column("receiver_client_id")]
        public Guid? ReceiverClientId { get; set; }

        /// <summary>
        /// معرف نقطة البيع في حال كانت العملية دفع عبر POS
        /// </summary>
        [Column("pos_point_id")]
        public Guid? PosPointId { get; set; }

        /// <summary>
        /// رمز العملة المنفذة بها العملية (YER, SAR, USD)
        /// </summary>
        [Required]
        [MaxLength(10)]
        [Column("currency_code")]
        public string CurrencyCode { get; set; } = "YER";

        /// <summary>
        /// المبلغ المحول أو المدفوع
        /// </summary>
        [Column("amount", TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// رسوم أو عمولة العملية (إن وجدت)
        /// </summary>
        [Column("fee", TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; } = 0.00m;

        /// <summary>
        /// المبلغ الإجمالي المخصوم من المرسل (المبلغ + الرسوم)
        /// </summary>
        [Column("total_amount", TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// حالة العملية:
        /// "Completed" مكتملة وناجحة
        /// "Pending" قيد التنفيذ
        /// "Failed" فشلت العملية
        /// "Cancelled" ملغاة
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Completed";

        #region بيانات العرض والخصوصية المحفوظة مع العملية

        /// <summary>
        /// اسم المرسل الظاهر في إيصال المستلم (الاسم الكامل أو الحروف الأولى المشفرة حسب إعدادات الخصوصية)
        /// </summary>
        [MaxLength(255)]
        [Column("sender_display_name")]
        public string? SenderDisplayName { get; set; }

        /// <summary>
        /// رقم هاتف المرسل الظاهر للتاجر أو المستلم (الرقم الحقيقي أو الرقم البديل المقنع)
        /// </summary>
        [MaxLength(50)]
        [Column("sender_display_phone")]
        public string? SenderDisplayPhone { get; set; }

        /// <summary>
        /// اسم المستلم أو المتجر الظاهر للمرسل في الإيصال
        /// </summary>
        [MaxLength(255)]
        [Column("receiver_display_name")]
        public string? ReceiverDisplayName { get; set; }

        /// <summary>
        /// ملاحظات أو بيان العملية المدخل من قبل العميل
        /// </summary>
        [MaxLength(500)]
        [Column("note")]
        public string? Note { get; set; }

        #endregion

        /// <summary>
        /// تاريخ وتوقيت تنفيذ العملية
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region العلاقات (Navigation Properties)

        /// <summary>
        /// العميل المرسل
        /// </summary>
        [ForeignKey(nameof(SenderClientId))]
        public virtual Client? SenderClient { get; set; }

        /// <summary>
        /// العميل المستلم
        /// </summary>
        [ForeignKey(nameof(ReceiverClientId))]
        public virtual Client? ReceiverClient { get; set; }

        /// <summary>
        /// نقطة البيع المستقبلة للمبلغ (في عمليات الـ POS)
        /// </summary>
        [ForeignKey(nameof(PosPointId))]
        public virtual PosPoint? PosPoint { get; set; }

        #endregion

        /// <summary>
        /// دالة لتوليد رقم مرجعي مميز للعملية
        /// </summary>
        public static string GenerateTransactionNumber()
        {
            var random = new Random();
            return $"TRX-{DateTime.UtcNow:yyyyMMdd}-{random.Next(100000, 999999)}";
        }
    }
}
