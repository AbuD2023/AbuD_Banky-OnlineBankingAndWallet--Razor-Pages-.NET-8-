using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان أجهزة المستخدم (Devices)
    /// لتتبع الأجهزة المصرح لها بالدخول، وإرسال إشعارات FCM والتأكد من أمان الحساب
    /// </summary>
    [Table("devices")]
    public class Device
    {
        /// <summary>
        /// المعرف الفريد لسجل الجهاز (GUID)
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// معرف العميل صاحب الجهاز
        /// </summary>
        [Column("client_id")]
        public Guid? ClientId { get; set; }

        /// <summary>
        /// رمز الإشعارات اللحظية الخاص بـ Firebase Cloud Messaging
        /// </summary>
        [MaxLength(1000)]
        [Column("fcm_token")]
        public string? FcmToken { get; set; }

        /// <summary>
        /// المعرف الفريد للجهاز الفعلي (Hardware/Platform Device ID)
        /// </summary>
        [MaxLength(255)]
        [Column("device_id")]
        public string? DeviceId { get; set; }

        /// <summary>
        /// اسم ونوع الجهاز (مثال: Samsung Galaxy S24, iPhone 15 Pro)
        /// </summary>
        [MaxLength(255)]
        [Column("device_name")]
        public string? DeviceName { get; set; }

        /// <summary>
        /// هل هذا هو الجهاز الأساسي المعتمد للمستخدم
        /// </summary>
        [Column("main_device")]
        public bool MainDevice { get; set; } = false;

        /// <summary>
        /// هل تم قبول والموافقة على الجهاز من قبل الإدارة
        /// </summary>
        [Column("is_approved")]
        public bool IsApproved { get; set; } = true;

        /// <summary>
        /// اسم أو معرف الموظف/المسؤول الذي وافق على الجهاز
        /// </summary>
        [MaxLength(255)]
        [Column("approved_by")]
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// تاريخ وتوقيت الموافقة على الجهاز
        /// </summary>
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// عنوان الـ IP لآخر تسجيل دخول من هذا الجهاز
        /// </summary>
        [MaxLength(50)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// تاريخ تسجيل الجهاز لأول مرة
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// تاريخ آخر نشاط من هذا الجهاز
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        #region العلاقات (Navigation Properties)

        /// <summary>
        /// العميل المرتبط بهذا الجهاز
        /// </summary>
        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        #endregion
    }
}
