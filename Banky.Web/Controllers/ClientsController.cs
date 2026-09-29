using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم إدارة العملاء في لوحة التحكم عبر الـ API
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ClientsController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public ClientsController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// استعراض جدول العملاء مع البحث وفلترة التوثيق
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? kycStatus)
        {
            var response = await _apiClient.GetAllClientsAsync();
            var clients = response.Data ?? new List<ClientProfileModel>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                clients = clients.Where(c => c.FullName.ToLower().Contains(s) || c.Phone.Contains(s) || c.Email.ToLower().Contains(s)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(kycStatus) && kycStatus != "All")
            {
                clients = clients.Where(c => c.KycStatus == kycStatus).ToList();
            }

            ViewBag.Search = search;
            ViewBag.KycStatus = kycStatus;

            return View(clients);
        }

        /// <summary>
        /// استعراض الملف التفصيلي للعميل ومحافظه ونقاط بيعه وعملياته من الـ API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _apiClient.GetClientDetailsAsync(id);
            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message ?? "العميل غير موجود";
                return RedirectToAction("Index");
            }

            return View(response.Data);
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور للعميل عبر الـ API
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(Guid id, string? tempPassword = "123456", string? returnUrl = null)
        {
            var response = await _apiClient.ResetClientPasswordAsync(id, tempPassword ?? "123456");
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تمت إعادة تعيين كلمة المرور بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشلت إعادة تعيين كلمة المرور";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// حظر أو فك حظر حساب العميل عبر الـ API
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(Guid id, bool isBlocked, string? blockedMessage, string? returnUrl = null)
        {
            var model = new BlockClientRequestModel
            {
                ClientId = id,
                IsBlocked = isBlocked,
                BlockedMessage = blockedMessage
            };

            var response = await _apiClient.ToggleBlockClientAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تحديث حالة الحساب بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تحديث حالة الحساب";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", new { id });
        }
    }
}
