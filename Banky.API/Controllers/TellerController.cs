using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// وحدة التحكم لخدمات الصندوق والسحب والإيداع (Teller API Controller)
    /// مخصصة لموظفي الصندوق والمدراء لتنفيذ عمليات السحب والإيداع والاستعلام عن العملاء وفتح المحافظ
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TellerController : ControllerBase
    {
        private readonly ITellerService _tellerService;

        public TellerController(ITellerService tellerService)
        {
            _tellerService = tellerService;
        }

        /// <summary>
        /// الاستعلام والبحث عن العميل برقم الهاتف، البريد، أو رقم الحساب
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Teller,TellerDeposit,TellerWithdrawal,Auditor")]
        public async Task<IActionResult> SearchClient([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { success = false, message = "يرجى إدخال معيار البحث" });
            }

            var client = await _tellerService.SearchClientAsync(query);
            if (client == null)
            {
                return NotFound(new { success = false, message = "لم يتم العثور على أي عميل مطابق للمعيار المدخل" });
            }

            return Ok(new { success = true, data = client });
        }

        /// <summary>
        /// تنفيذ عملية إيداع وتغذية رصيد للعميل بأي عملة معتمدة
        /// </summary>
        [HttpPost("deposit")]
        [Authorize(Roles = "Admin,Teller,TellerDeposit")]
        public async Task<IActionResult> Deposit([FromBody] TellerDepositRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات الإيداع غير مكتملة", errors = ModelState });
            }

            var tellerId = GetCurrentTellerId();
            var tellerName = GetCurrentTellerName();

            var result = await _tellerService.DepositAsync(dto, tellerId, tellerName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تنفيذ عملية سحب نقدي من محفظة العميل
        /// </summary>
        [HttpPost("withdraw")]
        [Authorize(Roles = "Admin,Teller,TellerWithdrawal")]
        public async Task<IActionResult> Withdraw([FromBody] TellerWithdrawalRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات السحب غير مكتملة", errors = ModelState });
            }

            var tellerId = GetCurrentTellerId();
            var tellerName = GetCurrentTellerName();

            var result = await _tellerService.WithdrawAsync(dto, tellerId, tellerName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// فتح وإضافة محفظة جديدة للعميل بعملة محددة
        /// </summary>
        [HttpPost("add-wallet")]
        [Authorize(Roles = "Admin,Teller,TellerDeposit,TellerWithdrawal")]
        public async Task<IActionResult> AddWallet([FromBody] TellerAddWalletDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات المحفظة غير مكتملة", errors = ModelState });
            }

            var tellerId = GetCurrentTellerId();
            var tellerName = GetCurrentTellerName();

            var result = await _tellerService.AddWalletAsync(dto, tellerId, tellerName);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// استرجاع سجل آخر العمليات المنفذة في الصندوق
        /// </summary>
        [HttpGet("recent-operations")]
        [Authorize(Roles = "Admin,Teller,TellerDeposit,TellerWithdrawal,Auditor")]
        public async Task<IActionResult> GetRecentOperations()
        {
            var tellerId = GetCurrentTellerId();
            var operations = await _tellerService.GetTellerRecentOperationsAsync(tellerId);
            return Ok(new { success = true, data = operations });
        }

        #region دوال مساعدة

        private Guid? GetCurrentTellerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var g) ? g : null;
        }

        private string GetCurrentTellerName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "موظف الصندوق";
        }

        #endregion
    }
}
