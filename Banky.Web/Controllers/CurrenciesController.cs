using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم إدارة العملات في لوحة التحكم عبر الـ API
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class CurrenciesController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public CurrenciesController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// استعراض قائمة العملات من الـ API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAllCurrenciesAsync();
            var list = response.Data ?? new List<CurrencyModel>();
            return View(list);
        }

        /// <summary>
        /// إضافة عملة جديدة عبر الـ API
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CurrencyCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى تعبئة كافة حقول العملة بشكل صحيح";
                return RedirectToAction("Index");
            }

            var response = await _apiClient.CreateCurrencyAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تمت إضافة العملة بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشلت إضافة العملة";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// تعديل بيانات عملة عبر الـ API
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CurrencyEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "بيانات التعديل غير صحيحة";
                return RedirectToAction("Index");
            }

            var response = await _apiClient.UpdateCurrencyAsync(id, model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تحديث العملة بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تحديث العملة";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// تفعيل أو تعطيل عملة عبر الـ API
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var response = await _apiClient.ToggleCurrencyStatusAsync(id);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تغيير حالة العملة بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تغيير حالة العملة";
            }

            return RedirectToAction("Index");
        }
    }
}
