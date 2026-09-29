using Banky.API.DTOs;
using Banky.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.API.Controllers
{
    /// <summary>
    /// متحكم عمليات المصادقة والحسابات وإعدادات الأمان والخصوصية
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// تسجيل حساب جديد في النظام
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "بيانات التسجيل غير مكتملة أو غير صحيحة", errors = ModelState });
            }

            var result = await _authService.RegisterAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// تسجيل الدخول للعميل أو المسؤول
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "يرجى إدخال اسم المستخدم وكلمة المرور", errors = ModelState });
            }

            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// استرجاع بيانات الملف الشخصي للمستخدم الحالي
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var profile = await _authService.GetProfileAsync(clientId.Value);
            if (profile == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

            return Ok(new { success = true, data = profile });
        }

        /// <summary>
        /// رفع وثائق التوثيق KYC (صورة البطاقة الشخصية الوجهين الأمامي والخلفي)
        /// </summary>
        [Authorize]
        [HttpPost("submit-kyc")]
        public async Task<IActionResult> SubmitKyc([FromBody] KycSubmissionDto dto)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var result = await _authService.SubmitKycAsync(clientId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// طلب استعادة كلمة المرور (نسيت كلمة المرور)
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, demoToken = result.DemoToken });
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور بواسطة رمز التحقق
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// تغيير كلمة المرور (سواء كان اختيارياً أو إجبارياً بعد إعادة التعيين من الإدارة)
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var result = await _authService.ChangePasswordAsync(clientId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// تحديث إعدادات الخصوصية والأمان (إخفاء الاسم، إخفاء الهاتف على POS، البصمة)
        /// </summary>
        [Authorize]
        [HttpPost("update-privacy")]
        public async Task<IActionResult> UpdatePrivacy([FromBody] UpdatePrivacyDto dto)
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var result = await _authService.UpdatePrivacyAsync(clientId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        /// <summary>
        /// توليد وتحديث الرقم البديل لنقاط البيع
        /// </summary>
        [Authorize]
        [HttpPost("regenerate-pos-alias")]
        public async Task<IActionResult> RegeneratePosAlias()
        {
            var clientId = GetCurrentClientId();
            if (clientId == null) return Unauthorized(new { success = false, message = "غير مصرح" });

            var result = await _authService.RegeneratePosAliasAsync(clientId.Value);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = "تعذر توليد الرقم البديل" });
            }

            return Ok(new { success = true, message = "تم توليد الرقم البديل بنجاح", newAlias = result.NewAlias });
        }

        #region دالة مساعدة لجلب معرف العميل الحالي من الـ Claims

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
