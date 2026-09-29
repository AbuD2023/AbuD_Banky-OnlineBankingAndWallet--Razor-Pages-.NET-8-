using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج بيانات إنشاء حساب جديد
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// الاسم الرباعي الكامل
        /// </summary>
        [Required(ErrorMessage = "الاسم الرباعي مطلوب")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// البريد الإلكتروني
        /// </summary>
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// رقم الهاتف
        /// </summary>
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// كلمة المرور
        /// </summary>
        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف أو أرقام")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج بيانات تسجيل الدخول
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// البريد الإلكتروني أو رقم الهاتف
        /// </summary>
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// كلمة المرور
        /// </summary>
        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// معرف الجهاز (اختياري)
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// رمز الإشعارات FCM (اختياري)
        /// </summary>
        public string? FcmToken { get; set; }
    }

    /// <summary>
    /// استجابة عملية المصادقة وتسجيل الدخول
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// رمز التحقق JWT
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// بيانات المستخدم الأساسية
        /// </summary>
        public ClientProfileDto User { get; set; } = new();

        /// <summary>
        /// هل يجب على المستخدم تغيير كلمة المرور فوراً (إذا تم تعيينها بواسطة الإدارة)
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// حالة توثيق الحساب (NotSubmitted, PendingApproval, Approved, Rejected)
        /// </summary>
        public string KycStatus { get; set; } = string.Empty;

        /// <summary>
        /// رسالة تنبيه أو توجيه للعميل
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// نموذج بيانات الملف الشخصي للعميل
    /// </summary>
    public class ClientProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public string? KycIdFront { get; set; }
        public string? KycIdBack { get; set; }
        public string? KycRejectionReason { get; set; }
        public bool HideFullName { get; set; }
        public bool HidePhoneOnPos { get; set; }
        public string? PosAliasPhone { get; set; }
        public bool IsBiometricEnabled { get; set; }
        public bool MustChangePassword { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockedMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نموذج رفع وتحديث وثائق التوثيق KYC
    /// </summary>
    public class KycSubmissionDto
    {
        /// <summary>
        /// مسار أو ملف صورة الوجه الأمامي للهوية
        /// </summary>
        [Required(ErrorMessage = "صورة الوجه الأمامي للبطاقة مطلوبة")]
        public string IdFrontImageBase64OrPath { get; set; } = string.Empty;

        /// <summary>
        /// مسار أو ملف صورة الوجه الخلفي للهوية
        /// </summary>
        [Required(ErrorMessage = "صورة الوجه الخلفي للبطاقة مطلوبة")]
        public string IdBackImageBase64OrPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج طلب استعادة كلمة المرور (نسيت كلمة المرور)
    /// </summary>
    public class ForgotPasswordDto
    {
        /// <summary>
        /// البريد الإلكتروني أو رقم الهاتف لاستلام رمز الاستعادة
        /// </summary>
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        public string Identifier { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج إعادة تعيين كلمة المرور عبر رمز الاستعادة
    /// </summary>
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز التحقق مطلوب")]
        public string ResetToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف أو أرقام")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج تغيير كلمة المرور العادي أو الإجباري
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// كلمة المرور الحالية (اختيارية في حال التغيير الإجباري بعد إعادة التعيين من الإدارة)
        /// </summary>
        public string? CurrentPassword { get; set; }

        /// <summary>
        /// كلمة المرور الجديدة
        /// </summary>
        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف أو أرقام")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج تحديث إعدادات الخصوصية والأمان
    /// </summary>
    public class UpdatePrivacyDto
    {
        /// <summary>
        /// إخفاء الاسم الكامل وعرض الحروف الأولى
        /// </summary>
        public bool HideFullName { get; set; }

        /// <summary>
        /// إخفاء رقم الهاتف عند الشراء عبر نقاط البيع
        /// </summary>
        public bool HidePhoneOnPos { get; set; }

        /// <summary>
        /// تفعيل أو تعطيل تسجيل الدخول بالبصمة
        /// </summary>
        public bool IsBiometricEnabled { get; set; }
    }

    /// <summary>
    /// نموذج إعادة تعيين كلمة المرور لمستخدم بواسطة الإدارة
    /// </summary>
    public class ResetUserPasswordByAdminDto
    {
        /// <summary>
        /// كلمة المرور المؤقتة البسيطة المقترحة
        /// </summary>
        [Required(ErrorMessage = "كلمة المرور المؤقتة مطلوبة")]
        public string TemporaryPassword { get; set; } = "123456";
    }
}
