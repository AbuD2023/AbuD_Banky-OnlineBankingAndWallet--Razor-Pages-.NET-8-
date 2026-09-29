using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان المحفظة المالية للعميل
    /// يمكن لكل عميل امتلاك محافظ متعددة بعملات مختلفة (ريال يمني، سعودي، دولار، إلخ)
    /// </summary>
    [Table("wallets")]
    public class Wallet
    {
        /// <summary>
        /// المعرف الفريد للمحفظة (GUID)
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// معرف العميل صاحب المحفظة
        /// </summary>
        [Required]
        [Column("client_id")]
        public Guid ClientId { get; set; }

        /// <summary>
        /// رقم الحساب المالي الفريد للمحفظة (مكون من أرقام مميزة مثل: 1001234567)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("account_number")]
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// رمز عملة المحفظة (مثل: YER, SAR, USD)
        /// </summary>
        [Required]
        [MaxLength(10)]
        [Column("currency_code")]
        public string CurrencyCode { get; set; } = "YER";

        /// <summary>
        /// الرصيد المالي المتاح في المحفظة
        /// </summary>
        [Column("balance", TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0.00m;

        /// <summary>
        /// حالة المحفظة (نشطة / مجمدة)
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاريخ إنشاء المحفظة
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// تاريخ آخر تعديل على رصيد أو حالة المحفظة
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        #region العلاقات (Navigation Properties)

        /// <summary>
        /// العميل صاحب هذه المحفظة
        /// </summary>
        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        /// <summary>
        /// العملة المرتبطة بالمحفظة
        /// </summary>
        [ForeignKey(nameof(CurrencyCode))]
        public virtual Currency? Currency { get; set; }

        #endregion

        /// <summary>
        /// دالة لتوليد رقم حساب فريد للمحفظة مكون من بادئة رقمية ورقم عشوائي
        /// </summary>
        public static string GenerateAccountNumber()
        {
            var random = new Random();
            return $"77{random.Next(10000000, 99999999)}";
        }
    }
}
