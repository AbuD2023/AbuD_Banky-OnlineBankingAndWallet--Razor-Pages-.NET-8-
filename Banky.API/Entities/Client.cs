using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banky.API.Entities
{
    /// <summary>
    /// كيان العميل / المستخدم في النظام
    /// يحتوي على بيانات الحساب، التوثيق KYC، إعدادات الخصوصية والأمان، والمحافظ التابعة له
    /// </summary>
    [Table("clients")]
    public class Client
    {
        /// <summary>
        /// المعرف الفريد للمستخدم (GUID)
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// الاسم الرباعي الكامل للمستخدم (مطلوب لعملية التوثيق والمطابقة)
        /// </summary>
        [Required(ErrorMessage = "الاسم الرباعي مطلوب")]
        [MaxLength(255)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// البريد الإلكتروني للمستخدم (يستخدم لتسجيل الدخول والإشعارات)
        /// </summary>
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// رقم الهاتف الأساسي للمستخدم (يستخدم للتحويلات المباشرة وتسجيل الدخول)
        /// </summary>
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [MaxLength(50)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// تجزئة كلمة المرور المشفرة بواسطة خوارزمية BCrypt الآمنة
        /// </summary>
        [Required]
        [MaxLength(500)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// دور المستخدم في النظام ("Client" للعملاء أو "Admin" للمسؤولين)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("role")]
        public string Role { get; set; } = "Client";

        /// <summary>
        /// رابط أو مسار الصورة الشخصية للعميل
        /// </summary>
        [MaxLength(500)]
        [Column("profile_image")]
        public string? ProfileImage { get; set; }

        #region التوثيق والتحقق من الهوية (KYC Verification)

        /// <summary>
        /// حالة توثيق الحساب بالهوية:
        /// "NotSubmitted" لم يرفع بعد
        /// "PendingApproval" قيد مراجعة الإدارة
        /// "Approved" تم التوثيق وقبول الحساب
        /// "Rejected" تم رفض التوثيق
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("kyc_status")]
        public string KycStatus { get; set; } = "NotSubmitted";

        /// <summary>
        /// مسار صورة الوجه الأمامي لبطاقة الهوية الشخصية
        /// </summary>
        [MaxLength(500)]
        [Column("kyc_id_front")]
        public string? KycIdFront { get; set; }

        /// <summary>
        /// مسار صورة الوجه الخلفي لبطاقة الهوية الشخصية
        /// </summary>
        [MaxLength(500)]
        [Column("kyc_id_back")]
        public string? KycIdBack { get; set; }

        /// <summary>
        /// سبب رفض التوثيق من قبل الإدارة في حال الرفض
        /// </summary>
        [MaxLength(500)]
        [Column("kyc_rejection_reason")]
        public string? KycRejectionReason { get; set; }

        /// <summary>
        /// تاريخ رفع وثائق الهوية
        /// </summary>
        [Column("kyc_submitted_at")]
        public DateTime? KycSubmittedAt { get; set; }

        /// <summary>
        /// تاريخ مراجعة الوثائق والموافقة/الرفض من قبل الإدارة
        /// </summary>
        [Column("kyc_reviewed_at")]
        public DateTime? KycReviewedAt { get; set; }

        #endregion

        #region إعدادات الخصوصية والأمان (Privacy & Security Settings)

        /// <summary>
        /// إخفاء الاسم الكامل عند البحث والتحويل وعرض الحروف الأولى من الاسم الرباعي فقط (مثال: أ . م . ع . ص)
        /// </summary>
        [Column("hide_full_name")]
        public bool HideFullName { get; set; } = false;

        /// <summary>
        /// إخفاء رقم الهاتف عند التحويل عبر نقاط البيع (POS) لحماية خصوصية العميل
        /// </summary>
        [Column("hide_phone_on_pos")]
        public bool HidePhoneOnPos { get; set; } = false;

        /// <summary>
        /// الرقم البديل المقنع المولد تلقائياً ليظهر عند الشراء من نقاط البيع بدلاً من الهاتف الحقيقي
        /// </summary>
        [MaxLength(50)]
        [Column("pos_alias_phone")]
        public string? PosAliasPhone { get; set; }

        /// <summary>
        /// تفعيل أو تعطيل تسجيل الدخول باستخدام البصمة البيومترية في التطبيق
        /// </summary>
        [Column("is_biometric_enabled")]
        public bool IsBiometricEnabled { get; set; } = false;

        /// <summary>
        /// راية تفيد بأن المسؤول قام بإعادة تعيين كلمة المرور ويجب على العميل تغييرها فور تسجيل الدخول
        /// </summary>
        [Column("must_change_password")]
        public bool MustChangePassword { get; set; } = false;

        /// <summary>
        /// رمز التحقق لإعادة تعيين كلمة المرور (OTP / Reset Token)
        /// </summary>
        [MaxLength(50)]
        [Column("reset_password_token")]
        public string? ResetPasswordToken { get; set; }

        /// <summary>
        /// وقت انتهاء صلاحية رمز إعادة تعيين كلمة المرور
        /// </summary>
        [Column("reset_password_expiry")]
        public DateTime? ResetPasswordExpiry { get; set; }

        #endregion

        #region حالة الحساب العامة (Account Status)

        /// <summary>
        /// هل الحساب محظور من قبل إدارة النظام
        /// </summary>
        [Column("is_blocked")]
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// رسالة أو سبب الحظر الظاهر للعميل
        /// </summary>
        [MaxLength(500)]
        [Column("blocked_message")]
        public string? BlockedMessage { get; set; }

        /// <summary>
        /// تاريخ إنشاء الحساب
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// تاريخ آخر تحديث لبيانات الحساب
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        #endregion

        #region العلاقات والروابط (Navigation Properties)

        /// <summary>
        /// المحافظ المالية التابعة للمستخدم بمختلف العملات
        /// </summary>
        public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

        /// <summary>
        /// نقاط البيع المنشأة بواسطة هذا العميل
        /// </summary>
        public virtual ICollection<PosPoint> PosPoints { get; set; } = new List<PosPoint>();

        /// <summary>
        /// الأجهزة المرتبطة بحساب العميل
        /// </summary>
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

        #endregion

        /// <summary>
        /// دالة مساعدة لتوليد الاسم المقنع بالحروف الأولى من الاسم الرباعي
        /// </summary>
        public string GetMaskedName()
        {
            if (string.IsNullOrWhiteSpace(FullName)) return string.Empty;
            
            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = parts.Select(p => p[0].ToString()).Take(4);
            return string.Join(" . ", initials);
        }

        /// <summary>
        /// دالة مساعدة لتوليد رقم بديل عشوائي لنقاط البيع
        /// </summary>
        public static string GenerateAliasPhone()
        {
            var random = new Random();
            return $"ALIAS-{random.Next(100000, 999999)}";
        }
    }
}
