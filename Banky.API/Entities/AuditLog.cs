using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان سجل الأنشطة والتدقيق (Audit Log)
    /// لتتبع الإجراءات الإدارية ومن قام بها بالتفصيل والوقت
    /// </summary>
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// معرف الموظف/المسؤول الذي قام بالعملية
        /// </summary>
        [Column("admin_id")]
        public Guid? AdminId { get; set; }

        /// <summary>
        /// اسم الموظف/المسؤول
        /// </summary>
        [MaxLength(255)]
        [Column("admin_name")]
        public string AdminName { get; set; } = string.Empty;

        /// <summary>
        /// دور الموظف (Admin, KycOfficer, CurrencyOfficer, Auditor)
        /// </summary>
        [MaxLength(100)]
        [Column("admin_role")]
        public string AdminRole { get; set; } = "Admin";

        /// <summary>
        /// نوع الإجراء (مثال: ApproveKyc, RejectKyc, UpdateCurrencyRate, ResetPassword, BlockClient, ApproveDevice, SetMainDevice)
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("action_type")]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// اسم الكيان المستهدف (Client, Currency, Device, PosPoint, Transaction)
        /// </summary>
        [MaxLength(100)]
        [Column("entity_name")]
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// المعرف الخاص بالكيان المستهدف
        /// </summary>
        [MaxLength(100)]
        [Column("entity_id")]
        public string? EntityId { get; set; }

        /// <summary>
        /// تفاصيل ووصف الإجراء المنفذ
        /// </summary>
        [Required]
        [MaxLength(1000)]
        [Column("details")]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// عنوان IP للموظف أثناء تنفيذ العملية
        /// </summary>
        [MaxLength(50)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// تاريخ وتوقيت تنفيذ الإجراء
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
