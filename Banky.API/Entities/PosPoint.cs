using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان نقطة البيع (POS - Point of Sale)
    /// يتيح لأي عميل إنشاء نقطة بيع أو متجر لاستقبال المدفوعات والتحويلات المباشرة عبر رمز أو QR Code
    /// </summary>
    [Table("pos_points")]
    public class PosPoint
    {
        /// <summary>
        /// المعرف الفريد لنقطة البيع (GUID)
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// معرف العميل التاجر / صاحب نقطة البيع
        /// </summary>
        [Required]
        [Column("client_id")]
        public Guid ClientId { get; set; }

        /// <summary>
        /// اسم المتجر أو نقطة البيع (مثال: سوبرماركت الأمل، كافيه النجوم)
        /// </summary>
        [Required(ErrorMessage = "اسم نقطة البيع مطلوب")]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// كود نقطة البيع الفريد المخصص للدفع السريع والمسح بالباركود (مثال: POS-789012)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("pos_code")]
        public string PosCode { get; set; } = string.Empty;

        /// <summary>
        /// عنوان أو موقع نقطة البيع الجغرافي
        /// </summary>
        [MaxLength(255)]
        [Column("address")]
        public string? Address { get; set; }

        /// <summary>
        /// تصنيف أو نشاط نقطة البيع (مطاعم، بقالة، صيدليات، خدمات، إلخ)
        /// </summary>
        [MaxLength(100)]
        [Column("category")]
        public string? Category { get; set; }

        /// <summary>
        /// هل نقطة البيع نشطة وتستقبل المدفوعات
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاريخ إنشاء نقطة البيع
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region العلاقات (Navigation Properties)

        /// <summary>
        /// التاجر / العميل المالك لنقطة البيع
        /// </summary>
        [ForeignKey(nameof(ClientId))]
        public virtual Client? Owner { get; set; }

        /// <summary>
        /// العمليات والمدفوعات التي تمت عبر نقطة البيع هذه
        /// </summary>
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        #endregion

        /// <summary>
        /// دالة لتوليد كود نقطة بيع فريد وسهل الحفظ
        /// </summary>
        public static string GeneratePosCode()
        {
            var random = new Random();
            return $"POS-{random.Next(100000, 999999)}";
        }
    }
}
