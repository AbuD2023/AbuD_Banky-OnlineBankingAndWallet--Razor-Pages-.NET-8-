using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم تسجيل الدخول والخروج للوحة التحكم الإدارية عبر الـ API
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public AccountController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// عرض صفحة تسجيل الدخول للإدارة
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// معالجة تسجيل دخول المسؤول بالاتصال مع الـ API وحفظ التوكن في الـ Claims
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. طلب تسجيل الدخول من خادم الـ API
            var response = await _apiClient.LoginAsync(model);

            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "بيانات الدخول غير صحيحة أو ليس لديك صلاحية مدير النظام");
                return View(model);
            }

            var authData = response.Data;
            var role = authData.User.Role;

            // التحقق من أن المستخدم يمتلك صلاحية موظف أو مسؤول وليس عميل عادي
            var allowedRoles = new[] { "Admin", "Teller", "TellerDeposit", "TellerWithdrawal", "KycOfficer", "CurrencyOfficer", "Auditor" };
            if (!allowedRoles.Contains(role))
            {
                ModelState.AddModelError(string.Empty, "هذا الحساب غير مصرح له بالدخول إلى لوحة التحكم الإدارية أو الصندوق");
                return View(model);
            }

            // 2. إنشاء الجلسة في الويب وتخزين الـ JWT Token والدور الحقيقي في الـ Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, authData.User.Id.ToString()),
                new Claim(ClaimTypes.Name, authData.User.FullName),
                new Claim(ClaimTypes.Email, authData.User.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim("jwt_token", authData.Token) // حفظ التوكن لإرساله مع كل طلب للـ API
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // التوجيه التلقائي المخصص حسب دور الموظف:
            if (role == "Teller" || role == "TellerDeposit" || role == "TellerWithdrawal")
            {
                return RedirectToAction("Index", "Teller");
            }
            else if (role == "KycOfficer")
            {
                return RedirectToAction("Index", "Kyc");
            }
            else if (role == "CurrencyOfficer")
            {
                return RedirectToAction("Index", "Currencies");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>
        /// تسجيل الخروج من لوحة التحكم
        /// </summary>
        [HttpPost]
        //[Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
