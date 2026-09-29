using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم عمليات الإدارة ولوحة التحكم (Admin Dashboard API)
    /// يتطلب صلاحية المدير (Admin) لتنفيذ العمليات الإدارية
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// استرجاع تقرير وإحصائيات لوحة التحكم الشاملة
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(new { success = true, data = stats });
        }

        /// <summary>
        /// استرجاع قائمة طلبات التوثيق المعلقة بالهوية لمراجعتها
        /// </summary>
        [HttpGet("kyc-pending")]
        public async Task<IActionResult> GetPendingKyc()
        {
            var list = await _adminService.GetPendingKycRequestsAsync();
            return Ok(new { success = true, data = list });
        }

        /// <summary>
        /// اتخاذ قرار مراجعة التوثيق (قبول أو رفض مع إبداء السبب)
        /// </summary>
        [HttpPost("review-kyc")]
        public async Task<IActionResult> ReviewKyc([FromBody] KycReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات المراجعة غير مكتملة", errors = ModelState });
            }

            var result = await _adminService.ReviewKycAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

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
        /// استرجاع التفاصيل الشاملة لعميل محدد مع محافظه ونقاط بيعه وآخر عملياته
        /// </summary>
        [HttpGet("clients/{id}")]
        public async Task<IActionResult> GetClientDetails(Guid id)
        {
            var details = await _adminService.GetClientDetailsAsync(id);
            if (details == null) return NotFound(new { success = false, message = "العميل غير موجود" });

            return Ok(new { success = true, data = details });
        }

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
        /// استرجاع سجل العمليات والحركات المالية الكاملة في النظام
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] TransactionFilterDto? filter)
        {
            var list = await _adminService.GetAllTransactionsAsync(filter);
            return Ok(new { success = true, data = list });
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور لمستخدم بواسطة الإدارة وتوليد كلمة مرور مؤقتة بسيطة
        /// وإجبار العميل على تغييرها فور تسجيل دخوله
        /// </summary>
        [HttpPost("reset-client-password")]
        public async Task<IActionResult> ResetClientPassword([FromQuery] Guid clientId, [FromQuery] string? tempPassword = "123456")
        {
            var result = await _adminService.ResetClientPasswordAsync(clientId, tempPassword);
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
        public async Task<IActionResult> ToggleBlockClient([FromBody] BlockClientDto dto)
        {
            var result = await _adminService.ToggleBlockClientAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// استرجاع كافة العملات المسجلة
        /// </summary>
        [HttpGet("currencies")]
        public async Task<IActionResult> GetCurrencies()
        {
            var currencies = await _adminService.GetAllCurrenciesAsync();
            return Ok(new { success = true, data = currencies });
        }

        /// <summary>
        /// إضافة عملة جديدة إلى النظام
        /// </summary>
        [HttpPost("currencies/create")]
        public async Task<IActionResult> CreateCurrency([FromBody] CurrencyCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات العملة غير مكتملة", errors = ModelState });
            }

            var result = await _adminService.CreateCurrencyAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تعديل بيانات عملة موجودة
        /// </summary>
        [HttpPut("currencies/{id}")]
        public async Task<IActionResult> UpdateCurrency(int id, [FromBody] CurrencyUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التعديل غير مكتملة", errors = ModelState });
            }

            var result = await _adminService.UpdateCurrencyAsync(id, dto);
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
        public async Task<IActionResult> ToggleCurrencyStatus(int id)
        {
            var result = await _adminService.ToggleCurrencyStatusAsync(id);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }
    }
}
