using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم إدارة ومراجعة طلبات توثيق الهوية KYC عبر الـ API
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class KycController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public KycController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// استعراض قائمة طلبات التوثيق المعلقة بالصور من الـ API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetPendingKycRequestsAsync();
            var list = response.Data ?? new List<RecentKycRequestModel>();

            return View(list);
        }

        /// <summary>
        /// اتخاذ قرار مراجعة التوثيق (قبول أو رفض) عبر إرسال الطلب للـ API
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(Guid clientId, bool isApproved, string? rejectionReason)
        {
            var model = new KycReviewRequestModel
            {
                ClientId = clientId,
                IsApproved = isApproved,
                RejectionReason = rejectionReason
            };

            var response = await _apiClient.ReviewKycAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تحديث حالة التوثيق بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تحديث حالة التوثيق";
            }

            return RedirectToAction("Index");
        }
    }
}
