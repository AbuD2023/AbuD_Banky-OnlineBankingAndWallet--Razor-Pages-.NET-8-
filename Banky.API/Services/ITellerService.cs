using Banky.API.DTOs;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة خدمة موظف الصندوق والسحب والإيداع (Teller Operations Service)
    /// تتيح لموظفي الصندوق الاستعلام عن العملاء، وتغذية وإيداع الأرصدة بأي عملة، وتنفيذ السحوبات النقدية، وإضافة محافظ جديدة للعميل
    /// </summary>
    public interface ITellerService
    {
        /// <summary>
        /// الاستعلام والبحث عن العميل برقم الهاتف أو البريد أو المعرف
        /// </summary>
        Task<TellerClientSummaryDto?> SearchClientAsync(string query);

        /// <summary>
        /// تنفيذ عملية إيداع وتغذية رصيد للعميل بأي عملة معتمدة
        /// </summary>
        Task<(bool Success, string Message, TransactionResponseDto? Data)> DepositAsync(TellerDepositRequestDto dto, Guid? tellerId = null, string tellerName = "Teller");

        /// <summary>
        /// تنفيذ عملية سحب نقدي من محفظة العميل
        /// </summary>
        Task<(bool Success, string Message, TransactionResponseDto? Data)> WithdrawAsync(TellerWithdrawalRequestDto dto, Guid? tellerId = null, string tellerName = "Teller");

        /// <summary>
        /// فتح وإضافة محفظة جديدة للعميل بعملة محددة
        /// </summary>
        Task<(bool Success, string Message, WalletResponseDto? Data)> AddWalletAsync(TellerAddWalletDto dto, Guid? tellerId = null, string tellerName = "Teller");

        /// <summary>
        /// استرجاع أحدث عمليات السحب والإيداع المنفذة عبر الصندوق
        /// </summary>
        Task<List<TransactionResponseDto>> GetTellerRecentOperationsAsync(Guid? tellerId = null);
    }
}
