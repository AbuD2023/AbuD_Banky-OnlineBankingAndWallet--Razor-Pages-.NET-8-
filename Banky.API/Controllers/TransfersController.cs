using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم العمليات المصرفية والتحويلات المالية والدفع لنقاط البيع
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransfersController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        /// <summary>
        /// الاستعلام عن مستلم برقم هاتفه قبل تنفيذ التحويل للتحقق من الاسم وقواعد الخصوصية
        /// </summary>
        [HttpGet("lookup-recipient")]
        public async Task<IActionResult> LookupRecipient([FromQuery] string phone, [FromQuery] string currencyCode = "YER")
        {
            var senderId = GetCurrentClientId();
            if (senderId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest(new { success = false, message = "يرجى إدخال رقم الهاتف" });
            }

            var result = await _transferService.LookupRecipientByPhoneAsync(senderId.Value, phone, currencyCode);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// الاستعلام عن نقطة بيع بواسطة كود الـ POS
        /// </summary>
        [HttpGet("lookup-pos")]
        public async Task<IActionResult> LookupPos([FromQuery] string posCode)
        {
            if (string.IsNullOrWhiteSpace(posCode))
            {
                return BadRequest(new { success = false, message = "يرجى إدخال كود نقطة البيع" });
            }

            var result = await _transferService.LookupPosByCodeAsync(posCode);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تنفيذ التحويل المالي لمشترك عبر رقم الهاتف
        /// </summary>
        [HttpPost("by-phone")]
        public async Task<IActionResult> TransferByPhone([FromBody] TransferByPhoneDto dto)
        {
            var senderId = GetCurrentClientId();
            if (senderId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التحويل غير مكتملة", errors = ModelState });
            }

            var result = await _transferService.TransferByPhoneAsync(senderId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تنفيذ الدفع والشراء عبر نقطة بيع (POS) مع استخدام الرقم البديل عند تفعيل الخصوصية
        /// </summary>
        [HttpPost("pay-pos")]
        public async Task<IActionResult> PayToPos([FromBody] PosPaymentDto dto)
        {
            var senderId = GetCurrentClientId();
            if (senderId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات الدفع غير مكتملة", errors = ModelState });
            }

            var result = await _transferService.PayToPosAsync(senderId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// حساب سعر الصرف والمبلغ المستلم قبل تأكيد التحويل بين الحسابات
        /// </summary>
        [HttpGet("calculate-exchange")]
        public async Task<IActionResult> CalculateExchange([FromQuery] string fromCurrency, [FromQuery] string toCurrency, [FromQuery] decimal amount)
        {
            if (amount <= 0)
            {
                return BadRequest(new { success = false, message = "المبلغ يجب أن يكون أكبر من الصفر" });
            }

            var result = await _transferService.CalculateExchangeAsync(fromCurrency, toCurrency, amount);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تنفيذ التحويل والمصارفة بين محافظ العميل الشخصية
        /// </summary>
        [HttpPost("exchange-self")]
        public async Task<IActionResult> ExchangeSelf([FromBody] SelfExchangeDto dto)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التحويل غير مكتملة", errors = ModelState });
            }

            var result = await _transferService.ExchangeSelfAsync(clientId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
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
