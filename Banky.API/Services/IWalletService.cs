using Banky.API.DTOs;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة إدارة المحافظ المالية للعملاء
    /// </summary>
    public interface IWalletService
    {
        /// <summary>
        /// استرجاع كافة المحافظ المالية التابعة للعميل
        /// </summary>
        Task<List<WalletResponseDto>> GetClientWalletsAsync(Guid clientId);

        /// <summary>
        /// إنشاء / فتح محفظة جديدة بعملة معتمدة (مثل YER, SAR, USD)
        /// </summary>
        Task<(bool Success, string Message, WalletResponseDto? Data)> CreateWalletAsync(Guid clientId, CreateWalletDto dto);

        /// <summary>
        /// إيداع وتغذية رصيد في محفظة معينة
        /// </summary>
        Task<(bool Success, string Message, WalletResponseDto? Data)> DepositAsync(Guid? clientId, DepositDto dto);

        /// <summary>
        /// استرجاع العملات المتاحة التي لم ينشئ العميل محفظة بها بعد
        /// </summary>
        Task<List<CurrencyResponseDto>> GetAvailableCurrenciesForClientAsync(Guid clientId);
    }

    /// <summary>
    /// نموذج بيانات العملة المتاحة
    /// </summary>
    public class CurrencyResponseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal ExchangeRate { get; set; }
    }
}
