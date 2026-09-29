using Banky.API.DTOs;
using Banky.API.Entities;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة خدمة إدارة النظام ولوحة التحكم (Admin Operations)
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// استرجاع إحصائيات عامة للنظام ولوحة التحكم
        /// </summary>
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync();

        /// <summary>
        /// استرجاع قائمة طلبات التوثيق المعلقة بالصور
        /// </summary>
        Task<List<RecentKycRequestDto>> GetPendingKycRequestsAsync();

        /// <summary>
        /// اتخاذ قرار مراجعة التوثيق (قبول أو رفض مع إبداء السبب)
        /// </summary>
        Task<(bool Success, string Message)> ReviewKycAsync(KycReviewDto dto);

        /// <summary>
        /// استرجاع قائمة كافة العملاء في النظام مع تفاصيل حساباتهم ومحافظهم
        /// </summary>
        Task<List<ClientProfileDto>> GetAllClientsAsync();

        /// <summary>
        /// استرجاع تفاصيل عميل محدد بما يشمل محافظه ونقاط بيعه وآخر عملياته
        /// </summary>
        Task<ClientDetailsDto?> GetClientDetailsAsync(Guid clientId);

        /// <summary>
        /// استرجاع كافة نقاط البيع المسجلة في النظام
        /// </summary>
        Task<List<AdminPosPointDto>> GetAllPosPointsAsync();

        /// <summary>
        /// استرجاع كافة العمليات والحركات المالية في النظام مع الفلاتر
        /// </summary>
        Task<List<TransactionResponseDto>> GetAllTransactionsAsync(TransactionFilterDto? filter = null);

        /// <summary>
        /// إعادة تعيين كلمة المرور لعميل بواسطة الإدارة وتوليد كلمة مرور مؤقتة وإجباره على تغييرها
        /// </summary>
        Task<(bool Success, string Message, string TempPassword)> ResetClientPasswordAsync(Guid clientId, string? customTempPassword = null);

        /// <summary>
        /// حظر أو فك حظر حساب عميل
        /// </summary>
        Task<(bool Success, string Message)> ToggleBlockClientAsync(BlockClientDto dto);

        /// <summary>
        /// استرجاع كافة العملات المسجلة في النظام
        /// </summary>
        Task<List<Currency>> GetAllCurrenciesAsync();

        /// <summary>
        /// إضافة عملة جديدة إلى النظام
        /// </summary>
        Task<(bool Success, string Message, Currency? Data)> CreateCurrencyAsync(CurrencyCreateDto dto);

        /// <summary>
        /// تعديل بيانات عملة موجودة
        /// </summary>
        Task<(bool Success, string Message, Currency? Data)> UpdateCurrencyAsync(int id, CurrencyUpdateDto dto);

        /// <summary>
        /// تفعيل أو تعطيل عملة في النظام
        /// </summary>
        Task<(bool Success, string Message)> ToggleCurrencyStatusAsync(int id);
    }

    /// <summary>
    /// نموذج تفاصيل العميل ومحافظه ونقاط بيعه وعملياته المعروض للمسؤول
    /// </summary>
    public class ClientDetailsDto
    {
        public ClientProfileDto Client { get; set; } = new();
        public List<WalletResponseDto> Wallets { get; set; } = new();
        public List<PosResponseDto> PosPoints { get; set; } = new();
        public List<TransactionResponseDto> RecentTransactions { get; set; } = new();
    }

    /// <summary>
    /// نموذج عرض نقطة البيع للمسؤول في لوحة التحكم
    /// </summary>
    public class AdminPosPointDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PosCode { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantPhone { get; set; } = string.Empty;
        public decimal TotalSalesAmount { get; set; }
        public int TotalSalesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
