namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج بيانات إحصائيات لوحة التحكم
    /// </summary>
    public class AdminDashboardStatsModel
    {
        public int TotalClientsCount { get; set; }
        public int PendingKycCount { get; set; }
        public int ApprovedKycCount { get; set; }
        public int TotalPosCount { get; set; }
        public int TotalTransactionsCount { get; set; }
        public decimal TotalTransferredAmountYer { get; set; }
        public decimal TotalTransferredAmountSar { get; set; }
        public decimal TotalTransferredAmountUsd { get; set; }
        public List<RecentKycRequestModel> RecentKycRequests { get; set; } = new();
        public List<TransactionModel> RecentTransactions { get; set; } = new();
    }

    /// <summary>
    /// نموذج طلب التوثيق المعروض للإدارة
    /// </summary>
    public class RecentKycRequestModel
    {
        public Guid ClientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public string? KycIdFront { get; set; }
        public string? KycIdBack { get; set; }
        public DateTime? KycSubmittedAt { get; set; }
    }
}
