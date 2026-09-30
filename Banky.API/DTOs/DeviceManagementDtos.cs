using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج بيانات جهاز العميل المعروض للمسؤول
    /// </summary>
    public class ClientDeviceDto
    {
        public Guid Id { get; set; }
        public Guid? ClientId { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public bool MainDevice { get; set; }
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// نموذج اتخاذ إجراء على جهاز (موافقة أو تعيين كرئيسي أو إزالة)
    /// </summary>
    public class ManageDeviceActionDto
    {
        [Required]
        public Guid DeviceId { get; set; }

        [Required]
        public Guid ClientId { get; set; }
    }
}
