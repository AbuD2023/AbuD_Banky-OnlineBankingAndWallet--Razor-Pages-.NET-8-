using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج بيانات الموظف
    /// </summary>
    public class StaffUserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleNameAr { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نموذج إضافة موظف جديد
    /// </summary>
    public class CreateStaffDto
    {
        [Required(ErrorMessage = "الاسم الكامل للموظف مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن لا تقل عن 6 خانات")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// دور الموظف: Admin, KycOfficer, CurrencyOfficer, Auditor
        /// </summary>
        [Required(ErrorMessage = "دور وصلاحية الموظف مطلوبة")]
        public string Role { get; set; } = "KycOfficer";
    }

    /// <summary>
    /// نموذج تعديل دور الموظف
    /// </summary>
    public class UpdateStaffRoleDto
    {
        [Required]
        public Guid StaffId { get; set; }

        [Required(ErrorMessage = "الدور الجديد مطلوب")]
        public string NewRole { get; set; } = string.Empty;
    }
}
