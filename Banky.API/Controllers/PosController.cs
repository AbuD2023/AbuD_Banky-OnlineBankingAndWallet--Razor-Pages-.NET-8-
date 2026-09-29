using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم نقاط البيع للتجار والعملاء
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PosController : ControllerBase
    {
        private readonly IPosService _posService;

        public PosController(IPosService posService)
        {
            _posService = posService;
        }

        /// <summary>
        /// إنشاء نقطة بيع جديدة للعميل
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePos([FromBody] CreatePosDto dto)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات نقطة البيع غير مكتملة", errors = ModelState });
            }

            var result = await _posService.CreatePosAsync(clientId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// استرجاع قائمة نقاط البيع المملوكة للعميل الحالي مع إحصائيات المبيعات
        /// </summary>
        [HttpGet("my-points")]
        public async Task<IActionResult> GetMyPoints()
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var points = await _posService.GetClientPosPointsAsync(clientId.Value);
            return Ok(new { success = true, data = points });
        }

        /// <summary>
        /// استرجاع تفاصيل نقطة بيع معينة
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPosDetails(Guid id)
        {
            var pos = await _posService.GetPosByIdAsync(id);
            if (pos == null) return NotFound(new { success = false, message = "نقطة البيع غير موجودة" });

            return Ok(new { success = true, data = pos });
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
