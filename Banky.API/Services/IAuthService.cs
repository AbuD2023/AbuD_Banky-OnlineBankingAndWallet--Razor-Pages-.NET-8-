using Banky.API.DTOs;
using Banky.API.Entities;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة خدمة المصادقة والتحقق من الهوية وإدارة الحسابات
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// تسجيل حساب عميل جديد
        /// </summary>
        Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto);

        /// <summary>
        /// تسجيل الدخول للعميل أو المسؤول
        /// </summary>
        Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto);

        /// <summary>
        /// استرجاع الملف الشخصي للعميل الحالي
        /// </summary>
        Task<ClientProfileDto?> GetProfileAsync(Guid clientId);

        /// <summary>
        /// رفع وتحديث وثائق التوثيق KYC (صورة الوجه الأمامي والخلفي للهوية)
        /// </summary>
        Task<(bool Success, string Message)> SubmitKycAsync(Guid clientId, KycSubmissionDto dto);

        /// <summary>
        /// طلب استعادة كلمة المرور (نسيت كلمة المرور)
        /// </summary>
        Task<(bool Success, string Message, string? DemoToken)> ForgotPasswordAsync(ForgotPasswordDto dto);

        /// <summary>
        /// إعادة تعيين كلمة المرور بواسطة رمز التحقق
        /// </summary>
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);

        /// <summary>
        /// تغيير كلمة المرور (سواء كان اختيارياً أو إجبارياً بعد إعادة التعيين من الإدارة)
        /// </summary>
        Task<(bool Success, string Message)> ChangePasswordAsync(Guid clientId, ChangePasswordDto dto);

        /// <summary>
        /// تحديث إعدادات الخصوصية (إخفاء الاسم، إخفاء الهاتف على POS، البصمة)
        /// </summary>
        Task<(bool Success, string Message, ClientProfileDto? Data)> UpdatePrivacyAsync(Guid clientId, UpdatePrivacyDto dto);

        /// <summary>
        /// توليد وتجديد الرقم البديل لنقاط البيع
        /// </summary>
        Task<(bool Success, string NewAlias)> RegeneratePosAliasAsync(Guid clientId);

        /// <summary>
        /// إنشاء التوكن الرقمي JWT
        /// </summary>
        string GenerateJwtToken(Client client);
    }
}
