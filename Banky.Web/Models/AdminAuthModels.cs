using System.ComponentModel.DataAnnotations;

namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج واجهة تسجيل دخول الإدارة للوحة التحكم
    /// </summary>
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        [Display(Name = "البريد الإلكتروني أو الهاتف")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; } = true;
    }

    /// <summary>
    /// نموذج استجابة المصادقة من الـ API
    /// </summary>
    public class AuthResponseModel
    {
        public string Token { get; set; } = string.Empty;
        public ClientProfileModel User { get; set; } = new();
        public bool MustChangePassword { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public string? Message { get; set; }
    }

    /// <summary>
    /// نموذج بيانات ملف المستخدم / العميل
    /// </summary>
    public class ClientProfileModel
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
}
