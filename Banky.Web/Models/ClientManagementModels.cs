using System.ComponentModel.DataAnnotations;

namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج تفاصيل العميل ومحافظه ونقاط بيعه وعملياته وأجهزته
    /// </summary>
    public class ClientDetailsModel
    {
        public ClientProfileModel Client { get; set; } = new();
        public List<WalletModel> Wallets { get; set; } = new();
        public List<PosPointModel> PosPoints { get; set; } = new();
        public List<ClientDeviceModel> Devices { get; set; } = new();
        public List<TransactionModel> RecentTransactions { get; set; } = new();
    }

    /// <summary>
    /// نموذج جهاز العميل
    /// </summary>
    public class ClientDeviceModel
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
    /// نموذج تعديل بيانات العميل من الإدارة
    /// </summary>
    public class AdminUpdateClientModel
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

    /// <summary>
    /// نموذج المحفظة المالية
    /// </summary>
    public class WalletModel
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyNameAr { get; set; } = string.Empty;
        public string CurrencyNameEn { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نموذج نقطة البيع التابعة للعميل
    /// </summary>
    public class PosPointModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PosCode { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// نموذج طلب مراجعة وتوثيق الهوية
    /// </summary>
    public class KycReviewRequestModel
    {
        public Guid ClientId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// نموذج طلب حظر العميل
    /// </summary>
    public class BlockClientRequestModel
    {
        public Guid ClientId { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockedMessage { get; set; }
    }
}
