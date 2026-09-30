using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم عمليات الإدارة ولوحة التحكم (Admin Dashboard API)
    /// يدعم صلاحيات الموظفين (Admin, KycOfficer, CurrencyOfficer, Auditor)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,KycOfficer,CurrencyOfficer,Auditor,Teller,TellerDeposit,TellerWithdrawal")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        #region إحصائيات لوحة التحكم

        /// <summary>
        /// استرجاع تقرير وإحصائيات لوحة التحكم الشاملة
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(new { success = true, data = stats });
        }

        #endregion

        #region مراجعة وتوثيق الهويات (KYC)

        /// <summary>
        /// استرجاع قائمة طلبات التوثيق المعلقة بالهوية لمراجعتها
        /// </summary>
        [HttpGet("kyc-pending")]
        [Authorize(Roles = "Admin,KycOfficer")]
        public async Task<IActionResult> GetPendingKyc()
        {
            var list = await _adminService.GetPendingKycRequestsAsync();
            return Ok(new { success = true, data = list });
        }

        /// <summary>
        /// اتخاذ قرار مراجعة التوثيق (قبول أو رفض مع إبداء السبب)
        /// </summary>
        [HttpPost("review-kyc")]
        [Authorize(Roles = "Admin,KycOfficer")]
        public async Task<IActionResult> ReviewKyc([FromBody] KycReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات المراجعة غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.ReviewKycAsync(dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region إدارة وتعديل العملاء

        /// <summary>
        /// استرجاع قائمة العملاء المسجلين في النظام
        /// </summary>
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _adminService.GetAllClientsAsync();
            return Ok(new { success = true, data = clients });
        }

        /// <summary>
        /// استرجاع التفاصيل الشاملة لعميل محدد مع محافظه ونقاط بيعه وآخر عملياته وأجهزته
        /// </summary>
        [HttpGet("clients/{id}")]
        public async Task<IActionResult> GetClientDetails(Guid id)
        {
            var details = await _adminService.GetClientDetailsAsync(id);
            if (details == null) return NotFound(new { success = false, message = "العميل غير موجود" });

            return Ok(new { success = true, data = details });
        }

        /// <summary>
        /// تعديل بيانات العميل من قبل الإدارة
        /// </summary>
        [HttpPut("clients/{id}")]
        [Authorize(Roles = "Admin,KycOfficer")]
        public async Task<IActionResult> UpdateClient(Guid id, [FromBody] AdminUpdateClientDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التعديل غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.UpdateClientAsync(id, dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور لمستخدم بواسطة الإدارة
        /// </summary>
        [HttpPost("reset-client-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetClientPassword([FromQuery] Guid clientId, [FromQuery] string? tempPassword = "123456")
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.ResetClientPasswordAsync(clientId, tempPassword, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, tempPassword = result.TempPassword });
        }

        /// <summary>
        /// حظر أو فك حظر حساب عميل
        /// </summary>
        [HttpPost("toggle-block-client")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleBlockClient([FromBody] BlockClientDto dto)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.ToggleBlockClientAsync(dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region إدارة أجهزة العميل (Device Management)

        /// <summary>
        /// استرجاع قائمة أجهزة عميل محدد
        /// </summary>
        [HttpGet("clients/{clientId}/devices")]
        public async Task<IActionResult> GetClientDevices(Guid clientId)
        {
            var list = await _adminService.GetClientDevicesAsync(clientId);
            return Ok(new { success = true, data = list });
        }

        /// <summary>
        /// موافقة الإدارة على تسجيل الدخول من جهاز
        /// </summary>
        [HttpPost("devices/{deviceId}/approve")]
        [Authorize(Roles = "Admin,KycOfficer")]
        public async Task<IActionResult> ApproveDevice(Guid deviceId)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.ApproveDeviceAsync(deviceId, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// تعيين جهاز كجهاز رئيسي معتمد للعميل
        /// </summary>
        [HttpPost("devices/{deviceId}/set-main")]
        [Authorize(Roles = "Admin,KycOfficer")]
        public async Task<IActionResult> SetMainDevice(Guid deviceId)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.SetMainDeviceAsync(deviceId, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// حذف أو فك ارتباط جهاز
        /// </summary>
        [HttpDelete("devices/{deviceId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDevice(Guid deviceId)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.DeleteDeviceAsync(deviceId, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region إدارة الموظفين والصلاحيات (Staff Management)

        /// <summary>
        /// استرجاع كافة الموظفين
        /// </summary>
        [HttpGet("staff")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllStaff()
        {
            var staff = await _adminService.GetAllStaffAsync();
            return Ok(new { success = true, data = staff });
        }

        /// <summary>
        /// إضافة موظف جديد
        /// </summary>
        [HttpPost("staff/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات الموظف غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.CreateStaffAsync(dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تعديل دور ورتبة موظف
        /// </summary>
        [HttpPut("staff/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStaffRole([FromBody] UpdateStaffRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "البيانات غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.UpdateStaffRoleAsync(dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// تجميد أو تفعيل حساب موظف
        /// </summary>
        [HttpPost("staff/{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStaffStatus(Guid id)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.ToggleStaffStatusAsync(id, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region سجل الأنشطة والتدقيق (Audit Logs)

        /// <summary>
        /// استرجاع سجل التدقيق والأنشطة الإدارية
        /// </summary>
        [HttpGet("audit-logs")]
        [Authorize(Roles = "Admin,Auditor")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _adminService.GetAuditLogsAsync();
            return Ok(new { success = true, data = logs });
        }

        #endregion

        #region نقاط البيع والعمليات

        /// <summary>
        /// استرجاع كافة نقاط البيع المسجلة في النظام
        /// </summary>
        [HttpGet("pos-points")]
        public async Task<IActionResult> GetAllPosPoints()
        {
            var list = await _adminService.GetAllPosPointsAsync();
            return Ok(new { success = true, data = list });
        }

        /// <summary>
        /// استرجاع سجل العمليات والحركات المالية الكاملة في النظام (مع الأسماء الحقيقية)
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] TransactionFilterDto? filter)
        {
            var list = await _adminService.GetAllTransactionsAsync(filter);
            return Ok(new { success = true, data = list });
        }

        #endregion

        #region إدارة العملات

        /// <summary>
        /// استرجاع كافة العملات المسجلة
        /// </summary>
        [HttpGet("currencies")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrencies()
        {
            var currencies = await _adminService.GetAllCurrenciesAsync();
            return Ok(new { success = true, data = currencies });
        }

        /// <summary>
        /// إضافة عملة جديدة إلى النظام
        /// </summary>
        [HttpPost("currencies/create")]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        public async Task<IActionResult> CreateCurrency([FromBody] CurrencyCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات العملة غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.CreateCurrencyAsync(dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تعديل بيانات عملة موجودة وتحديث سعر الصرف
        /// </summary>
        [HttpPut("currencies/{id}")]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        public async Task<IActionResult> UpdateCurrency(int id, [FromBody] CurrencyUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التعديل غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.UpdateCurrencyAsync(id, dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تفعيل أو تعطيل عملة
        /// </summary>
        [HttpPost("currencies/{id}/toggle-status")]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        public async Task<IActionResult> ToggleCurrencyStatus(int id)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.ToggleCurrencyStatusAsync(id, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region إدارة الرسوم والعمولات المصرفية (Fee Settings Endpoints)

        /// <summary>
        /// استرجاع كافة إعدادات الرسوم والعمولات لكافة العمليات
        /// </summary>
        [HttpGet("fees")]
        [Authorize(Roles = "Admin,Auditor")]
        public async Task<IActionResult> GetAllFees()
        {
            var fees = await _adminService.GetAllFeeSettingsAsync();
            return Ok(new { success = true, data = fees });
        }

        /// <summary>
        /// استرجاع إعداد رسوم محدد بالمعرف
        /// </summary>
        [HttpGet("fees/{id}")]
        [Authorize(Roles = "Admin,Auditor")]
        public async Task<IActionResult> GetFeeById(int id)
        {
            var fee = await _adminService.GetFeeSettingByIdAsync(id);
            if (fee == null)
            {
                return NotFound(new { success = false, message = "إعداد الرسوم غير موجود" });
            }

            return Ok(new { success = true, data = fee });
        }

        /// <summary>
        /// إضافة قاعدة ورسوم جديدة لعملية وعملة محددة
        /// </summary>
        [HttpPost("fees")]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        public async Task<IActionResult> CreateFee([FromBody] CreateFeeSettingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات الإدخال غير مكتملة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.CreateFeeSettingAsync(dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تعديل إعدادات الرسوم والعمولة لعملية محددة
        /// </summary>
        [HttpPut("fees/{id}")]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        public async Task<IActionResult> UpdateFee(int id, [FromBody] UpdateFeeSettingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التعديل غير صحيحة", errors = ModelState });
            }

            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.UpdateFeeSettingAsync(id, dto, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// حذف سياسة ورسوم مخصصة
        /// </summary>
        [HttpDelete("fees/{id}")]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        public async Task<IActionResult> DeleteFee(int id)
        {
            var adminId = GetCurrentAdminId();
            var adminName = GetCurrentAdminName();

            var result = await _adminService.DeleteFeeSettingAsync(id, adminId, adminName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// معاينة وحساب الرسوم التقديرية والمبلغ الإجمالي
        /// </summary>
        [HttpGet("fees/preview")]
        [AllowAnonymous]
        public async Task<IActionResult> CalculateFeePreview([FromQuery] string type, [FromQuery] decimal amount, [FromQuery] string currency = "YER")
        {
            if (string.IsNullOrWhiteSpace(type) || amount <= 0)
            {
                return BadRequest(new { success = false, message = "نوع العملية والمبلغ مطلوبان بشكل صحيح" });
            }

            var preview = await _adminService.CalculateFeePreviewAsync(type, amount, currency);
            return Ok(new { success = true, data = preview });
        }

        #endregion

        #region دوال مساعدة

        private Guid? GetCurrentAdminId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var g) ? g : null;
        }

        private string GetCurrentAdminName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";
        }

        #endregion
    }
}
