using Banky.API.DTOs;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة خدمة التحويلات المالية والمدفوعات الإلكترونية
    /// </summary>
    public interface ITransferService
    {
        /// <summary>
        /// الاستعلام عن مستلم برقم الهاتف قبل تنفيذ التحويل والتحقق من اسمه وقواعد الخصوصية
        /// </summary>
        Task<(bool Success, string Message, RecipientLookupResponseDto? Data)> LookupRecipientByPhoneAsync(Guid senderId, string phone, string currencyCode);

        /// <summary>
        /// الاستعلام عن نقطة بيع عبر كود الـ POS أو الباركود
        /// </summary>
        Task<(bool Success, string Message, PosLookupResponseDto? Data)> LookupPosByCodeAsync(string posCode);

        /// <summary>
        /// تنفيذ التحويل المالي لمشترك عبر رقم الهاتف
        /// </summary>
        Task<(bool Success, string Message, TransactionResponseDto? Data)> TransferByPhoneAsync(Guid senderId, TransferByPhoneDto dto);

        /// <summary>
        /// تنفيذ الدفع والشراء عبر نقطة بيع (POS)
        /// </summary>
        Task<(bool Success, string Message, TransactionResponseDto? Data)> PayToPosAsync(Guid senderId, PosPaymentDto dto);
    }
}
