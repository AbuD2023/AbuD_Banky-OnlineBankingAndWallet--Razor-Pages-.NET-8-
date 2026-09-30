using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// طلب إيداع وتغذية رصيد العميل عبر موظف الصندوق (Teller Deposit)
    /// </summary>
    public class TellerDepositRequestDto
    {
        [Required(ErrorMessage = "معرف العميل مطلوب")]
        public Guid ClientId { get; set; }

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        public string CurrencyCode { get; set; } = "YER";

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// طلب سحب نقدي من حساب العميل عبر موظف الصندوق (Teller Withdrawal)
    /// </summary>
    public class TellerWithdrawalRequestDto
    {
        [Required(ErrorMessage = "معرف العميل مطلوب")]
        public Guid ClientId { get; set; }

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        public string CurrencyCode { get; set; } = "YER";

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// طلب فتح محفظة جديدة للعميل بعملة محددة عبر موظف الصندوق أو الإدارة
    /// </summary>
    public class TellerAddWalletDto
    {
        [Required(ErrorMessage = "معرف العميل مطلوب")]
        public Guid ClientId { get; set; }

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        public string CurrencyCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// ملخص بيانات العميل ومحافظه وعملياته لموظف الصندوق والسحب والإيداع
    /// </summary>
    public class TellerClientSummaryDto
    {
        public Guid ClientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public bool IsBlocked { get; set; }
        public bool HideFullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<WalletResponseDto> Wallets { get; set; } = new();
        public List<TransactionResponseDto> RecentTellerTransactions { get; set; } = new();
    }
}
