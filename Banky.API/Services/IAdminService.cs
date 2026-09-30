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
        Task<(bool Success, string Message)> ReviewKycAsync(KycReviewDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// استرجاع قائمة كافة العملاء في النظام مع تفاصيل حساباتهم ومحافظهم
        /// </summary>
        Task<List<ClientProfileDto>> GetAllClientsAsync();

        /// <summary>
        /// استرجاع تفاصيل عميل محدد بما يشمل محافظه ونقاط بيعه وآخر عملياته وأجهزته
        /// </summary>
        Task<ClientDetailsDto?> GetClientDetailsAsync(Guid clientId);

        /// <summary>
        /// تعديل بيانات العميل من قبل الإدارة
        /// </summary>
        Task<(bool Success, string Message)> UpdateClientAsync(Guid clientId, AdminUpdateClientDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// استرجاع أجهزة العميل
        /// </summary>
        Task<List<ClientDeviceDto>> GetClientDevicesAsync(Guid clientId);

        /// <summary>
        /// موافقة الإدارة على تسجيل الدخول من جهاز
        /// </summary>
        Task<(bool Success, string Message)> ApproveDeviceAsync(Guid deviceId, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// تعيين جهاز كجهاز رئيسي معتمد للعميل
        /// </summary>
        Task<(bool Success, string Message)> SetMainDeviceAsync(Guid deviceId, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// حذف أو فك ارتباط جهاز
        /// </summary>
        Task<(bool Success, string Message)> DeleteDeviceAsync(Guid deviceId, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// استرجاع قائمة كافة الموظفين في لوحة التحكم
        /// </summary>
        Task<List<StaffUserDto>> GetAllStaffAsync();

        /// <summary>
        /// إضافة موظف جديد وتحديد دوره
        /// </summary>
        Task<(bool Success, string Message, StaffUserDto? Data)> CreateStaffAsync(CreateStaffDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// تعديل دور وصلاحيات الموظف
        /// </summary>
        Task<(bool Success, string Message)> UpdateStaffRoleAsync(UpdateStaffRoleDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// تفعيل أو تعطيل حساب موظف
        /// </summary>
        Task<(bool Success, string Message)> ToggleStaffStatusAsync(Guid staffId, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// استرجاع سجل الأنشطة والتدقيق للإدارة
        /// </summary>
        Task<List<AuditLogResponseDto>> GetAuditLogsAsync();

        /// <summary>
        /// تسجيل نشاط إداري في سجل التدقيق
        /// </summary>
        Task LogActivityAsync(Guid? adminId, string adminName, string adminRole, string actionType, string entityName, string? entityId, string details, string? ipAddress = null);

        /// <summary>
        /// استرجاع كافة نقاط البيع المسجلة في النظام
        /// </summary>
        Task<List<AdminPosPointDto>> GetAllPosPointsAsync();

        /// <summary>
        /// استرجاع كافة العمليات والحركات المالية في النظام مع الفلاتر (مع الأسماء والأرقام الحقيقية غير المقنعة)
        /// </summary>
        Task<List<TransactionResponseDto>> GetAllTransactionsAsync(TransactionFilterDto? filter = null);

        /// <summary>
        /// إعادة تعيين كلمة المرور لعميل بواسطة الإدارة وتوليد كلمة مرور مؤقتة وإجباره على تغييرها
        /// </summary>
        Task<(bool Success, string Message, string TempPassword)> ResetClientPasswordAsync(Guid clientId, string? customTempPassword = null, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// حظر أو فك حظر حساب عميل
        /// </summary>
        Task<(bool Success, string Message)> ToggleBlockClientAsync(BlockClientDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// استرجاع كافة العملات المسجلة في النظام
        /// </summary>
        Task<List<Currency>> GetAllCurrenciesAsync();

        /// <summary>
        /// إضافة عملة جديدة إلى النظام
        /// </summary>
        Task<(bool Success, string Message, Currency? Data)> CreateCurrencyAsync(CurrencyCreateDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// تعديل بيانات عملة موجودة
        /// </summary>
        Task<(bool Success, string Message, Currency? Data)> UpdateCurrencyAsync(int id, CurrencyUpdateDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// تفعيل أو تعطيل عملة في النظام
        /// </summary>
        Task<(bool Success, string Message)> ToggleCurrencyStatusAsync(int id, Guid? adminId = null, string adminName = "Admin");

        #region إدارة الرسوم والعمولات (Fee Settings Management)

        /// <summary>
        /// استرجاع كافة إعدادات الرسوم والعمولات لكافة العمليات في النظام
        /// </summary>
        Task<List<FeeSettingResponseDto>> GetAllFeeSettingsAsync();

        /// <summary>
        /// استرجاع إعداد رسوم محدد بالمعرف
        /// </summary>
        Task<FeeSettingResponseDto?> GetFeeSettingByIdAsync(int id);

        /// <summary>
        /// إضافة قاعدة وسياسة رسوم جديدة لعملية وعملة محددة مع التوثيق في سجل التدقيق
        /// </summary>
        Task<(bool Success, string Message, FeeSettingResponseDto? Data)> CreateFeeSettingAsync(CreateFeeSettingDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// تعديل سياسة ورسوم عملية محددة من قبل مدير النظام مع التوثيق في سجل التدقيق
        /// </summary>
        Task<(bool Success, string Message, FeeSettingResponseDto? Data)> UpdateFeeSettingAsync(int id, UpdateFeeSettingDto dto, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// حذف قاعدة رسوم مخصصة من قبل المدير
        /// </summary>
        Task<(bool Success, string Message)> DeleteFeeSettingAsync(int id, Guid? adminId = null, string adminName = "Admin");

        /// <summary>
        /// جلب قاعدة الرسوم الفعالة لنوع عملية وعملة محددة
        /// </summary>
        Task<FeeSetting?> GetFeeRuleAsync(string operationType, string? currencyCode = null);

        /// <summary>
        /// معاينة وحساب الرسوم والمبلغ الإجمالي المخصوم لتطبيق الهاتف ولوحة التحكم
        /// </summary>
        Task<FeeCalculationPreviewDto> CalculateFeePreviewAsync(string operationType, decimal amount, string currencyCode);

        #endregion
    }

    /// <summary>
    /// نموذج تفاصيل العميل ومحافظه ونقاط بيعه وعملياته وأجهزته المعروض للمسؤول
    /// </summary>
    public class ClientDetailsDto
    {
        public ClientProfileDto Client { get; set; } = new();
        public List<WalletResponseDto> Wallets { get; set; } = new();
        public List<PosResponseDto> PosPoints { get; set; } = new();
        public List<ClientDeviceDto> Devices { get; set; } = new();
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
        public string MerchantEmail { get; set; } = string.Empty;
        public string MerchantKycStatus { get; set; } = string.Empty;
        public decimal TotalSalesAmount { get; set; }
        public int TotalSalesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
