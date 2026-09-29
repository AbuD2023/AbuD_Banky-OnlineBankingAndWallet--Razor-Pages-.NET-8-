using Banky.API.DTOs;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة خدمة استعراض وتتبع الحركات والعمليات المالية
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// استرجاع سجل العمليات الخاصة بالعميل (الواردة والصادرة والإيداعات)
        /// </summary>
        Task<List<TransactionResponseDto>> GetClientTransactionsAsync(Guid clientId, TransactionFilterDto? filter = null);

        /// <summary>
        /// استرجاع تفاصيل عملية مالية محددة
        /// </summary>
        Task<TransactionResponseDto?> GetTransactionDetailsAsync(Guid clientId, Guid transactionId);
    }
}
