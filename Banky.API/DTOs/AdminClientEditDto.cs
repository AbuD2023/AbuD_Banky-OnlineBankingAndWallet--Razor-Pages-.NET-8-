using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج تعديل بيانات العميل من قبل الإدارة
    /// </summary>
    public class AdminUpdateClientDto
    {
        [Required(ErrorMessage = "الاسم الرباعي مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "حالة التوثيق مطلوبة")]
        public string KycStatus { get; set; } = "NotSubmitted";

        public bool IsBlocked { get; set; }

        public string? BlockedMessage { get; set; }

        public bool HideFullName { get; set; }

        public bool HidePhoneOnPos { get; set; }
    }
}
