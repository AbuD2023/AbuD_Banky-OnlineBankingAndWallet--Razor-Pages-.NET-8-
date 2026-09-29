using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم إدارة المحافظ المالية للعملاء والعمليات المرتبطة بها
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletsController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// استرجاع كافة المحافظ المالية للعميل المسجل دخوله
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyWallets()
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var wallets = await _walletService.GetClientWalletsAsync(clientId.Value);
            return Ok(new { success = true, data = wallets });
        }

        /// <summary>
        /// إنشاء / فتح محفظة مالية جديدة بعملة معتمدة (مثل YER, SAR, USD)
        /// يتطلب أن يكون الحساب موثقاً KYC
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletDto dto)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var result = await _walletService.CreateWalletAsync(clientId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// إيداع وتغذية رصيد في محفظة معينة
        /// </summary>
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositDto dto)
        {
            var clientId = GetCurrentClientId();

            var result = await _walletService.DepositAsync(clientId, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// استرجاع قائمة العملات المتاحة التي يمكن للعميل فتح محفظة جديدة بها
        /// </summary>
        [HttpGet("available-currencies")]
        public async Task<IActionResult> GetAvailableCurrencies()
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var currencies = await _walletService.GetAvailableCurrenciesForClientAsync(clientId.Value);
            return Ok(new { success = true, data = currencies });
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
