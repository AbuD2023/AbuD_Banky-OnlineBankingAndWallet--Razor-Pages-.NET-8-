using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم استعراض وتتبع الحركات والعمليات المالية
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// استرجاع سجل العمليات الخاصة بالعميل (الحوالات الصادرة والواردة، مدفوعات نقاط البيع، والإيداعات)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterDto filter)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var transactions = await _transactionService.GetClientTransactionsAsync(clientId.Value, filter);
            return Ok(new { success = true, data = transactions });
        }

        /// <summary>
        /// استرجاع تفاصيل حركة مالية معينة
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionDetails(Guid id)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var details = await _transactionService.GetTransactionDetailsAsync(clientId.Value, id);
            if (details == null) return NotFound(new { success = false, message = "العملية غير موجودة" });

            return Ok(new { success = true, data = details });
        }

        #region دالة مساعدة

        private Guid? GetCurrentClientId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var guid))
            {
                return guid;
            }
            return null;
        }

        #endregion
    }
}
