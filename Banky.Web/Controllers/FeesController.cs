using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// وحدة التحكم بإدارة الرسوم والعمولات المصرفية (Fees Management Controller)
    /// تتيح لمدير النظام استعراض سياسات الرسوم لكل عملية وتعديل النسب والحدود الأدنى والأعلى وتفعيلها
    /// مع توفير حاسبة اختبارية سريعة لمعاينة أثر التعديلات قبل حفظها
    /// </summary>
    /// <summary>
    /// وحدة التحكم بإدارة الرسوم والعمولات المصرفية (Fees Management Controller)
    /// تتيح لمدير النظام استعراض وضبط سياسات الرسوم لكل عملية وعملة بشكل منفصل أو عام
    /// مع توفير حاسبة اختبارية سريعة لمعاينة أثر التعديلات قبل حفظها
    /// </summary>
    [Authorize(Roles = "Admin,CurrencyOfficer,Auditor")]
    public class FeesController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public FeesController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// عرض الصفحة الرئيسية لجميع سياسات الرسوم والعمولات المصرفية مع فلاتر العملات
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? currency = null, string? opType = null)
        {
            var feesResponse = await _apiClient.GetAllFeesAsync();
            var currenciesResponse = await _apiClient.GetAllCurrenciesAsync();

            var fees = feesResponse.Success && feesResponse.Data != null ? feesResponse.Data : new List<FeeSettingViewModel>();
            var currencies = currenciesResponse.Success && currenciesResponse.Data != null ? currenciesResponse.Data : new List<CurrencyModel>();

            // تطبيق الفلترة إذا تم تحديدها
            if (!string.IsNullOrWhiteSpace(currency))
            {
                if (currency == "ALL_GENERAL")
                {
                    fees = fees.Where(f => string.IsNullOrEmpty(f.CurrencyCode)).ToList();
                }
                else
                {
                    fees = fees.Where(f => f.CurrencyCode?.Equals(currency, StringComparison.OrdinalIgnoreCase) == true).ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(opType))
            {
                fees = fees.Where(f => f.OperationType.Equals(opType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var viewModel = new FeeIndexViewModel
            {
                Fees = fees,
                Currencies = currencies,
                SelectedCurrencyFilter = currency,
                SelectedOperationFilter = opType
            };

            return View(viewModel);
        }

        /// <summary>
        /// إضافة قاعدة ورسوم جديدة لعملية وعملة محددة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateFeeSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى تعبئة كافة حقول السياسة الجديدة بشكل صحيح";
                return RedirectToAction(nameof(Index));
            }

            var response = await _apiClient.CreateFeeAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تمت إضافة سياسة الرسوم بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل إضافة سياسة الرسوم";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// جلب بيانات سياسة رسوم محددة لعرضها في نافذة التعديل المنبثقة عبر AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeeJson(int id)
        {
            var response = await _apiClient.GetFeeByIdAsync(id);
            if (!response.Success || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "الرسوم غير موجودة" });
            }

            return Json(new { success = true, data = response.Data });
        }

        /// <summary>
        /// حفظ التعديلات على سياسة رسوم محددة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditFeeSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى التحقق من صحة البيانات المدخلة";
                return RedirectToAction(nameof(Index));
            }

            var response = await _apiClient.UpdateFeeAsync(model.Id, model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تحديث إعدادات الرسوم بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تحديث إعدادات الرسوم";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// حذف سياسة ورسوم مخصصة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,CurrencyOfficer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _apiClient.DeleteFeeAsync(id);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم حذف سياسة الرسوم بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل حذف سياسة الرسوم";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// اختبار حاسبة الرسوم الفورية عبر AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CalculatePreview(string type, decimal amount, string currency = "YER")
        {
            if (string.IsNullOrWhiteSpace(type) || amount <= 0)
            {
                return Json(new { success = false, message = "يرجى إدخال العملية والمبلغ" });
            }

            var response = await _apiClient.CalculateFeePreviewAsync(type, amount, currency);
            if (!response.Success || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "تعذر احتساب الرسوم" });
            }

            return Json(new { success = true, data = response.Data });
        }
    }
}
