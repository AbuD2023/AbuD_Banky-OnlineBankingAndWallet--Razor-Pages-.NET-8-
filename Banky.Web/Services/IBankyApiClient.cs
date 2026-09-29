using Banky.Web.Models;

namespace Banky.Web.Services
{
    /// <summary>
    /// واجهة عميل الـ API المخصص للوحة التحكم الإدارية
    /// مسؤول عن التواصل الكامل مع مشروع Banky.API عبر طلبات HTTP الموثقة بـ Bearer Token
    /// </summary>
    public interface IBankyApiClient
    {
        /// <summary>
        /// تسجيل دخول المسؤول واستلام رمز الـ JWT
        /// </summary>
        Task<ApiResponse<AuthResponseModel>> LoginAsync(AdminLoginViewModel model);

        /// <summary>
        /// استرجاع إحصائيات لوحة التحكم والبيانات العامة
        /// </summary>
        Task<ApiResponse<AdminDashboardStatsModel>> GetDashboardStatsAsync();

        /// <summary>
        /// استرجاع قائمة طلبات التوثيق المعلقة KYC
        /// </summary>
        Task<ApiResponse<List<RecentKycRequestModel>>> GetPendingKycRequestsAsync();

        /// <summary>
        /// مراجعة طلب التوثيق بالقبول أو الرفض
        /// </summary>
        Task<ApiResponse> ReviewKycAsync(KycReviewRequestModel model);

        /// <summary>
        /// استرجاع قائمة كافة العملاء في النظام
        /// </summary>
        Task<ApiResponse<List<ClientProfileModel>>> GetAllClientsAsync();

        /// <summary>
        /// استرجاع تفاصيل عميل محدد ومحافظه ونقاط بيعه وعملياته
        /// </summary>
        Task<ApiResponse<ClientDetailsModel>> GetClientDetailsAsync(Guid id);

        /// <summary>
        /// إعادة تعيين كلمة المرور للعميل إلى كلمة مؤقتة وإجباره على تغييرها
        /// </summary>
        Task<ApiResponse> ResetClientPasswordAsync(Guid id, string tempPassword);

        /// <summary>
        /// حظر أو فك حظر حساب عميل
        /// </summary>
        Task<ApiResponse> ToggleBlockClientAsync(BlockClientRequestModel model);

        /// <summary>
        /// استرجاع كافة العملات المعتمدة
        /// </summary>
        Task<ApiResponse<List<CurrencyModel>>> GetAllCurrenciesAsync();

        /// <summary>
        /// إضافة عملة جديدة إلى النظام
        /// </summary>
        Task<ApiResponse<CurrencyModel>> CreateCurrencyAsync(CurrencyCreateViewModel model);

        /// <summary>
        /// تعديل بيانات عملة موجودة
        /// </summary>
        Task<ApiResponse<CurrencyModel>> UpdateCurrencyAsync(int id, CurrencyEditViewModel model);

        /// <summary>
        /// تفعيل أو تعطيل عملة
        /// </summary>
        Task<ApiResponse> ToggleCurrencyStatusAsync(int id);

        /// <summary>
        /// استرجاع كافة نقاط البيع المسجلة في النظام
        /// </summary>
        Task<ApiResponse<List<AdminPosPointModel>>> GetAllPosPointsAsync();

        /// <summary>
        /// استرجاع سجل العمليات والحركات المالية الكاملة مع الفلاتر
        /// </summary>
        Task<ApiResponse<List<TransactionModel>>> GetAllTransactionsAsync(TransactionFilterModel? filter = null);
    }
}
