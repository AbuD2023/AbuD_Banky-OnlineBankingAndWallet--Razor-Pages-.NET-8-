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
        /// استرجاع تفاصيل عميل محدد ومحافظه ونقاط بيعه وعملياته وأجهزته
        /// </summary>
        Task<ApiResponse<ClientDetailsModel>> GetClientDetailsAsync(Guid id);

        /// <summary>
        /// تعديل بيانات العميل من قبل الإدارة
        /// </summary>
        Task<ApiResponse> UpdateClientAsync(Guid id, AdminUpdateClientModel model);

        /// <summary>
        /// استرجاع أجهزة العميل
        /// </summary>
        Task<ApiResponse<List<ClientDeviceModel>>> GetClientDevicesAsync(Guid clientId);

        /// <summary>
        /// الموافقة على تسجيل دخول جهاز
        /// </summary>
        Task<ApiResponse> ApproveDeviceAsync(Guid deviceId);

        /// <summary>
        /// تعيين جهاز كجهاز رئيسي
        /// </summary>
        Task<ApiResponse> SetMainDeviceAsync(Guid deviceId);

        /// <summary>
        /// حذف جهاز
        /// </summary>
        Task<ApiResponse> DeleteDeviceAsync(Guid deviceId);

        /// <summary>
        /// إعادة تعيين كلمة المرور للعميل إلى كلمة مؤقتة وإجباره على تغييرها
        /// </summary>
        Task<ApiResponse> ResetClientPasswordAsync(Guid id, string tempPassword);

        /// <summary>
        /// حظر أو فك حظر حساب عميل
        /// </summary>
        Task<ApiResponse> ToggleBlockClientAsync(BlockClientRequestModel model);

        /// <summary>
        /// استرجاع قائمة الموظفين
        /// </summary>
        Task<ApiResponse<List<StaffUserModel>>> GetAllStaffAsync();

        /// <summary>
        /// إضافة موظف جديد
        /// </summary>
        Task<ApiResponse<StaffUserModel>> CreateStaffAsync(CreateStaffModel model);

        /// <summary>
        /// تعديل دور الموظف
        /// </summary>
        Task<ApiResponse> UpdateStaffRoleAsync(UpdateStaffRoleModel model);

        /// <summary>
        /// تجميد/تفعيل حساب موظف
        /// </summary>
        Task<ApiResponse> ToggleStaffStatusAsync(Guid staffId);

        /// <summary>
        /// استرجاع سجل التدقيق والأنشطة الإدارية
        /// </summary>
        Task<ApiResponse<List<AuditLogModel>>> GetAuditLogsAsync();

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

        #region إدارة الرسوم والعمولات المصرفية

        /// <summary>
        /// استرجاع كافة إعدادات الرسوم والعمولات
        /// </summary>
        Task<ApiResponse<List<FeeSettingViewModel>>> GetAllFeesAsync();

        /// <summary>
        /// استرجاع إعداد رسوم محدد بالمعرف
        /// </summary>
        Task<ApiResponse<FeeSettingViewModel>> GetFeeByIdAsync(int id);

        /// <summary>
        /// إنشاء قاعدة ورسوم جديدة لعملية وعملة محددة
        /// </summary>
        Task<ApiResponse<FeeSettingViewModel>> CreateFeeAsync(CreateFeeSettingViewModel model);

        /// <summary>
        /// تعديل إعداد الرسوم والعمولة لعملية محددة
        /// </summary>
        Task<ApiResponse<FeeSettingViewModel>> UpdateFeeAsync(int id, EditFeeSettingViewModel model);

        /// <summary>
        /// حذف قاعدة ورسوم مخصصة
        /// </summary>
        Task<ApiResponse> DeleteFeeAsync(int id);

        /// <summary>
        /// معاينة وحساب الرسوم التقديرية والمبلغ الإجمالي
        /// </summary>
        Task<ApiResponse<FeePreviewViewModel>> CalculateFeePreviewAsync(string type, decimal amount, string currency = "YER");

        #endregion

        #region خدمات موظف الصندوق والسحب والإيداع (Teller Operations)

        /// <summary>
        /// البحث والاستعلام عن العميل لموظف الصندوق
        /// </summary>
        Task<ApiResponse<TellerClientSummaryModel>> SearchClientForTellerAsync(string query);

        /// <summary>
        /// تنفيذ إيداع وتغذية رصيد العميل بأي عملة
        /// </summary>
        Task<ApiResponse<TransactionModel>> TellerDepositAsync(TellerDepositRequestModel model);

        /// <summary>
        /// تنفيذ سحب نقدي من حساب العميل
        /// </summary>
        Task<ApiResponse<TransactionModel>> TellerWithdrawAsync(TellerWithdrawalRequestModel model);

        /// <summary>
        /// فتح وإضافة محفظة جديدة للعميل بعملة محددة
        /// </summary>
        Task<ApiResponse<WalletModel>> TellerAddWalletAsync(TellerAddWalletModel model);

        /// <summary>
        /// استرجاع سجل آخر عمليات الصندوق
        /// </summary>
        Task<ApiResponse<List<TransactionModel>>> GetTellerRecentOperationsAsync();

        #endregion
    }
}
