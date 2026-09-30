using System.ComponentModel.DataAnnotations;

namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج ملخص بيانات العميل ومحافظه وعملياته لموظف الصندوق في لوحة التحكم
    /// </summary>
    public class TellerClientSummaryModel
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
        public List<WalletModel> Wallets { get; set; } = new();
        public List<TransactionModel> RecentTellerTransactions { get; set; } = new();
    }

    /// <summary>
    /// نموذج تنفيذ عملية إيداع وتغذية رصيد العميل عبر الصندوق
    /// </summary>
    public class TellerDepositRequestModel
    {
        [Required(ErrorMessage = "معرف العميل مطلوب")]
        public Guid ClientId { get; set; }

        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        [Display(Name = "عملة الإيداع")]
        public string CurrencyCode { get; set; } = "YER";

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        [Display(Name = "مبلغ الإيداع")]
        public decimal Amount { get; set; }

        [Display(Name = "البيان / ملاحظات")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// نموذج تنفيذ عملية سحب نقدي من حساب العميل عبر الصندوق
    /// </summary>
    public class TellerWithdrawalRequestModel
    {
        [Required(ErrorMessage = "معرف العميل مطلوب")]
        public Guid ClientId { get; set; }

        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        [Display(Name = "عملة السحب")]
        public string CurrencyCode { get; set; } = "YER";

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        [Display(Name = "مبلغ السحب")]
        public decimal Amount { get; set; }

        [Display(Name = "البيان / ملاحظات")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// نموذج فتح محفظة جديدة للعميل من قبل موظف الصندوق أو الإدارة
    /// </summary>
    public class TellerAddWalletModel
    {
        [Required(ErrorMessage = "معرف العميل مطلوب")]
        public Guid ClientId { get; set; }

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        [Display(Name = "العملة")]
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
