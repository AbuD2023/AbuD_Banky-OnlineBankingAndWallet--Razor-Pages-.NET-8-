using Banky.Web.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Banky.Web.Services
{
    /// <summary>
    /// خدمة عميل الـ API المعتمدة في لوحة التحكم Banky.Web
    /// تقوم بكافة الاتصالات مع خادم Banky.API عبر HTTP وتمرير الـ JWT Bearer Token تلقائياً
    /// </summary>
    public class BankyApiClient : IBankyApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public BankyApiClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;

            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5021";
            _httpClient.BaseAddress = new Uri(baseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        #region المصادقة والداشبورد
        /// <summary>
        /// تسجيل دخول المسؤول للـ API واستلام التوكن
        /// </summary>
        public async Task<ApiResponse<AuthResponseModel>> LoginAsync(AdminLoginViewModel model)
        {
            var payload = new
            {
                identifier = model.Identifier,
                password = model.Password
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/auth/login", content);

            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var errObj = JsonSerializer.Deserialize<ApiResponse<AuthResponseModel>>(json, _jsonOptions);
                    return errObj ?? new ApiResponse<AuthResponseModel> { Success = false, Message = "فشل تسجيل الدخول" };
                }
                catch
                {
                    return new ApiResponse<AuthResponseModel> { Success = false, Message = "فشل الاتصال بالـ API" };
                }
            }

            return JsonSerializer.Deserialize<ApiResponse<AuthResponseModel>>(json, _jsonOptions)
                   ?? new ApiResponse<AuthResponseModel> { Success = false, Message = "استجابة غير متوقعة" };
        }

        /// <summary>
        /// استرجاع إحصائيات لوحة التحكم
        /// </summary>
        public async Task<ApiResponse<AdminDashboardStatsModel>> GetDashboardStatsAsync()
        {
            return await GetAsync<AdminDashboardStatsModel>("/api/admin/dashboard");
        }

        #endregion

        #region التوثيق KYC
        /// <summary>
        /// استرجاع طلبات التوثيق المعلقة KYC
        /// </summary>
        public async Task<ApiResponse<List<RecentKycRequestModel>>> GetPendingKycRequestsAsync()
        {
            return await GetAsync<List<RecentKycRequestModel>>("/api/admin/kyc-pending");
        }

        /// <summary>
        /// مراجعة طلب التوثيق (قبول / رفض)
        /// </summary>
        public async Task<ApiResponse> ReviewKycAsync(KycReviewRequestModel model)
        {
            return await PostAsync("/api/admin/review-kyc", model);
        }

        #endregion

        #region إدارة العملاء
        /// <summary>
        /// استرجاع كافة العملاء
        /// </summary>
        public async Task<ApiResponse<List<ClientProfileModel>>> GetAllClientsAsync()
        {
            return await GetAsync<List<ClientProfileModel>>("/api/admin/clients");
        }

        /// <summary>
        /// استرجاع تفاصيل عميل محدد
        /// </summary>
        public async Task<ApiResponse<ClientDetailsModel>> GetClientDetailsAsync(Guid id)
        {
            return await GetAsync<ClientDetailsModel>($"/api/admin/clients/{id}");
        }

        /// <summary>
        /// تعديل بيانات عميل محدد
        /// </summary>
        public async Task<ApiResponse> UpdateClientAsync(Guid id, AdminUpdateClientModel model)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/admin/clients/{id}", content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions)
                   ?? new ApiResponse { Success = response.IsSuccessStatusCode, Message = response.ReasonPhrase };
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور للعميل
        /// </summary>
        public async Task<ApiResponse> ResetClientPasswordAsync(Guid id, string tempPassword)
        {
            return await PostAsync($"/api/admin/reset-client-password?clientId={id}&tempPassword={Uri.EscapeDataString(tempPassword)}", null);
        }

        /// <summary>
        /// حظر أو فك حظر حساب عميل
        /// </summary>
        public async Task<ApiResponse> ToggleBlockClientAsync(BlockClientRequestModel model)
        {
            return await PostAsync("/api/admin/toggle-block-client", model);
        }

        #endregion

        #region إدارة الأجهزة (Device Management)

        public async Task<ApiResponse<List<ClientDeviceModel>>> GetClientDevicesAsync(Guid clientId)
        {
            return await GetAsync<List<ClientDeviceModel>>($"/api/admin/clients/{clientId}/devices");
        }

        public async Task<ApiResponse> ApproveDeviceAsync(Guid deviceId)
        {
            return await PostAsync($"/api/admin/devices/{deviceId}/approve", null);
        }

        public async Task<ApiResponse> SetMainDeviceAsync(Guid deviceId)
        {
            return await PostAsync($"/api/admin/devices/{deviceId}/set-main", null);
        }

        public async Task<ApiResponse> DeleteDeviceAsync(Guid deviceId)
        {
            AttachBearerToken();
            var response = await _httpClient.DeleteAsync($"/api/admin/devices/{deviceId}");
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions)
                   ?? new ApiResponse { Success = response.IsSuccessStatusCode, Message = response.ReasonPhrase };
        }

        #endregion

        #region إدارة الموظفين والصلاحيات (Staff Management)

        public async Task<ApiResponse<List<StaffUserModel>>> GetAllStaffAsync()
        {
            return await GetAsync<List<StaffUserModel>>("/api/admin/staff");
        }

        public async Task<ApiResponse<StaffUserModel>> CreateStaffAsync(CreateStaffModel model)
        {
            return await PostWithResponseAsync<StaffUserModel>("/api/admin/staff/create", model);
        }

        public async Task<ApiResponse> UpdateStaffRoleAsync(UpdateStaffRoleModel model)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("/api/admin/staff/role", content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions)
                   ?? new ApiResponse { Success = response.IsSuccessStatusCode, Message = response.ReasonPhrase };
        }

        public async Task<ApiResponse> ToggleStaffStatusAsync(Guid staffId)
        {
            return await PostAsync($"/api/admin/staff/{staffId}/toggle-status", null);
        }

        #endregion

        #region سجل الأنشطة والتدقيق (Audit Logs)

        public async Task<ApiResponse<List<AuditLogModel>>> GetAuditLogsAsync()
        {
            return await GetAsync<List<AuditLogModel>>("/api/admin/audit-logs");
        }

        #endregion

        #region إدارة العملات
        /// <summary>
        /// استرجاع كافة العملات
        /// </summary>
        public async Task<ApiResponse<List<CurrencyModel>>> GetAllCurrenciesAsync()
        {
            return await GetAsync<List<CurrencyModel>>("/api/admin/currencies");
        }

        /// <summary>
        /// إضافة عملة جديدة للنظام
        /// </summary>
        public async Task<ApiResponse<CurrencyModel>> CreateCurrencyAsync(CurrencyCreateViewModel model)
        {
            return await PostWithResponseAsync<CurrencyModel>("/api/admin/currencies/create", model);
        }

        /// <summary>
        /// تعديل بيانات عملة
        /// </summary>
        public async Task<ApiResponse<CurrencyModel>> UpdateCurrencyAsync(int id, CurrencyEditViewModel model)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/admin/currencies/{id}", content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse<CurrencyModel>>(json, _jsonOptions)
                   ?? new ApiResponse<CurrencyModel> { Success = false, Message = "خطأ في تعديل العملة" };
        }

        /// <summary>
        /// تفعيل أو تعطيل عملة
        /// </summary>
        public async Task<ApiResponse> ToggleCurrencyStatusAsync(int id)
        {
            return await PostAsync($"/api/admin/currencies/{id}/toggle-status", null);
        }

        #endregion

        #region نقاط البيع والعمليات
        /// <summary>
        /// استرجاع كافة نقاط البيع
        /// </summary>
        public async Task<ApiResponse<List<AdminPosPointModel>>> GetAllPosPointsAsync()
        {
            return await GetAsync<List<AdminPosPointModel>>("/api/admin/pos-points");
        }

        /// <summary>
        /// استرجاع سجل العمليات المالي
        /// </summary>
        public async Task<ApiResponse<List<TransactionModel>>> GetAllTransactionsAsync(TransactionFilterModel? filter = null)
        {
            var query = new StringBuilder("/api/admin/transactions?");
            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
                    query.Append($"currencyCode={Uri.EscapeDataString(filter.CurrencyCode)}&");
                if (!string.IsNullOrWhiteSpace(filter.Type))
                    query.Append($"type={Uri.EscapeDataString(filter.Type)}&");
                if (filter.FromDate.HasValue)
                    query.Append($"fromDate={filter.FromDate.Value:yyyy-MM-dd}&");
                if (filter.ToDate.HasValue)
                    query.Append($"toDate={filter.ToDate.Value:yyyy-MM-dd}&");
                query.Append($"pageSize={filter.PageSize}&");
            }

            return await GetAsync<List<TransactionModel>>(query.ToString().TrimEnd('&', '?'));
        }

        #endregion

        #region إدارة الرسوم والعمولات المصرفية

        /// <summary>
        /// استرجاع كافة إعدادات الرسوم والعمولات
        /// </summary>
        public async Task<ApiResponse<List<FeeSettingViewModel>>> GetAllFeesAsync()
        {
            return await GetAsync<List<FeeSettingViewModel>>("/api/admin/fees");
        }

        /// <summary>
        /// استرجاع إعداد رسوم محدد بالمعرف
        /// </summary>
        public async Task<ApiResponse<FeeSettingViewModel>> GetFeeByIdAsync(int id)
        {
            return await GetAsync<FeeSettingViewModel>($"/api/admin/fees/{id}");
        }

        /// <summary>
        /// إنشاء قاعدة ورسوم جديدة لعملية وعملة محددة
        /// </summary>
        public async Task<ApiResponse<FeeSettingViewModel>> CreateFeeAsync(CreateFeeSettingViewModel model)
        {
            return await PostWithResponseAsync<FeeSettingViewModel>("/api/admin/fees", model);
        }

        /// <summary>
        /// تعديل إعداد الرسوم والعمولة لعملية محددة
        /// </summary>
        public async Task<ApiResponse<FeeSettingViewModel>> UpdateFeeAsync(int id, EditFeeSettingViewModel model)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/admin/fees/{id}", content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse<FeeSettingViewModel>>(json, _jsonOptions)
                   ?? new ApiResponse<FeeSettingViewModel> { Success = false, Message = "خطأ في تعديل الرسوم" };
        }

        /// <summary>
        /// حذف قاعدة ورسوم مخصصة
        /// </summary>
        public async Task<ApiResponse> DeleteFeeAsync(int id)
        {
            return await DeleteAsync($"/api/admin/fees/{id}");
        }

        /// <summary>
        /// معاينة وحساب الرسوم التقديرية والمبلغ الإجمالي
        /// </summary>
        public async Task<ApiResponse<FeePreviewViewModel>> CalculateFeePreviewAsync(string type, decimal amount, string currency = "YER")
        {
            return await GetAsync<FeePreviewViewModel>($"/api/admin/fees/preview?type={Uri.EscapeDataString(type)}&amount={amount}&currency={Uri.EscapeDataString(currency)}");
        }

        #endregion

        #region خدمات موظف الصندوق والسحب والإيداع (Teller Operations)

        /// <summary>
        /// البحث والاستعلام عن العميل لموظف الصندوق
        /// </summary>
        public async Task<ApiResponse<TellerClientSummaryModel>> SearchClientForTellerAsync(string query)
        {
            return await GetAsync<TellerClientSummaryModel>($"/api/teller/search?query={Uri.EscapeDataString(query)}");
        }

        /// <summary>
        /// تنفيذ إيداع وتغذية رصيد العميل بأي عملة معتمدة
        /// </summary>
        public async Task<ApiResponse<TransactionModel>> TellerDepositAsync(TellerDepositRequestModel model)
        {
            return await PostWithResponseAsync<TransactionModel>("/api/teller/deposit", model);
        }

        /// <summary>
        /// تنفيذ سحب نقدي من حساب العميل
        /// </summary>
        public async Task<ApiResponse<TransactionModel>> TellerWithdrawAsync(TellerWithdrawalRequestModel model)
        {
            return await PostWithResponseAsync<TransactionModel>("/api/teller/withdraw", model);
        }

        /// <summary>
        /// فتح وإضافة محفظة جديدة للعميل بعملة محددة
        /// </summary>
        public async Task<ApiResponse<WalletModel>> TellerAddWalletAsync(TellerAddWalletModel model)
        {
            return await PostWithResponseAsync<WalletModel>("/api/teller/add-wallet", model);
        }

        /// <summary>
        /// استرجاع سجل آخر عمليات الصندوق
        /// </summary>
        public async Task<ApiResponse<List<TransactionModel>>> GetTellerRecentOperationsAsync()
        {
            return await GetAsync<List<TransactionModel>>("/api/teller/recent-operations");
        }

        #endregion

        #region دوال مساعدة خاصة لإرسال طلبات الـ HTTP وإرفاق التوكن
        /// <summary>
        /// إرفاق توكن الـ JWT من الجلسة في ترويسة الطلب
        /// </summary>
        private void AttachBearerToken()
        {
            var token = _httpContextAccessor.HttpContext?.User?.FindFirst("jwt_token")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                AttachBearerToken();
                var response = await _httpClient.GetAsync(endpoint);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResponse<T> { Success = false, Message = $"خطأ في استجابة الخادم: {response.StatusCode}" };
                }

                return JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions)
                       ?? new ApiResponse<T> { Success = false, Message = "فشل في قراءة البيانات" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Message = $"تعذر الاتصال بالـ API: {ex.Message}" };
            }
        }

        private async Task<ApiResponse> PostAsync(string endpoint, object? payload)
        {
            try
            {
                AttachBearerToken();
                HttpContent? content = null;
                if (payload != null)
                {
                    content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                }
                else
                {
                    content = new StringContent("{}", Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.PostAsync(endpoint, content);
                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions)
                       ?? new ApiResponse { Success = response.IsSuccessStatusCode, Message = response.ReasonPhrase };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = $"تعذر الاتصال بالـ API: {ex.Message}" };
            }
        }

        private async Task<ApiResponse<T>> PostWithResponseAsync<T>(string endpoint, object payload)
        {
            try
            {
                AttachBearerToken();
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions)
                       ?? new ApiResponse<T> { Success = false, Message = "فشل استلام الاستجابة" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Message = $"تعذر الاتصال بالـ API: {ex.Message}" };
            }
        }

        private async Task<ApiResponse> DeleteAsync(string endpoint)
        {
            try
            {
                AttachBearerToken();
                var response = await _httpClient.DeleteAsync(endpoint);
                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions)
                       ?? new ApiResponse { Success = response.IsSuccessStatusCode, Message = response.ReasonPhrase };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = $"تعذر الاتصال بالـ API: {ex.Message}" };
            }
        }

        #endregion
    }
}
