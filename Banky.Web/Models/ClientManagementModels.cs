using System.ComponentModel.DataAnnotations;

namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج تفاصيل العميل ومحافظه ونقاط بيعه وعملياته
    /// </summary>
    public class ClientDetailsModel
    {
        public ClientProfileModel Client { get; set; } = new();
        public List<WalletModel> Wallets { get; set; } = new();
        public List<PosPointModel> PosPoints { get; set; } = new();
        public List<TransactionModel> RecentTransactions { get; set; } = new();
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
