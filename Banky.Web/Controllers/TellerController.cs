using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// وحدة التحكم بمكتب الصندوق والسحب والإيداع (Teller Desk Controller)
    /// مخصصة لموظفي الصندوق والمدراء للاستعلام عن حسابات العملاء، تغذية الحسابات بأي عملة، سحب المبالغ، وفتح المحافظ
    /// </summary>
    [Authorize(Roles = "Admin,Teller,TellerDeposit,TellerWithdrawal,Auditor")]
    public class TellerController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public TellerController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// الواجهة الرئيسية لمكتب الصندوق
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? query = null)
        {
            TellerClientSummaryModel? clientSummary = null;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchRes = await _apiClient.SearchClientForTellerAsync(query);
                if (searchRes.Success && searchRes.Data != null)
                {
                    clientSummary = searchRes.Data;
                }
                else
                {
                    TempData["ErrorMessage"] = searchRes.Message ?? "لم يتم العثور على أي عميل مطابق للبحث";
                }
            }

            // جلب العملات المعتمدة
            var currRes = await _apiClient.GetAllCurrenciesAsync();
            ViewBag.Currencies = currRes.Success && currRes.Data != null ? currRes.Data : new List<CurrencyModel>();

            // جلب آخر عمليات الصندوق
            var recentTrxRes = await _apiClient.GetTellerRecentOperationsAsync();
            ViewBag.RecentOperations = recentTrxRes.Success && recentTrxRes.Data != null ? recentTrxRes.Data : new List<TransactionModel>();

            ViewBag.SearchQuery = query;
            return View(clientSummary);
        }

        /// <summary>
        /// البحث عن العميل عبر AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchJson(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "يرجى إدخال معيار البحث" });
            }

            var response = await _apiClient.SearchClientForTellerAsync(query);
            if (!response.Success || response.Data == null)
            {
                return Json(new { success = false, message = response.Message ?? "العميل غير موجود" });
            }

            return Json(new { success = true, data = response.Data });
        }

        /// <summary>
        /// تنفيذ عملية إيداع وتغذية رصيد العميل
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Teller,TellerDeposit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(TellerDepositRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "بيانات الإيداع غير مكتملة أو غير صحيحة";
                return RedirectToAction(nameof(Index), new { query = model.ClientPhone ?? model.ClientId.ToString() });
            }

            var response = await _apiClient.TellerDepositAsync(model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تنفيذ عملية الإيداع بنجاح";
                if (response.Data != null)
                {
                    TempData["LastTrxJson"] = System.Text.Json.JsonSerializer.Serialize(response.Data);
                }
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشلت عملية الإيداع";
            }

            return RedirectToAction(nameof(Index), new { query = model.ClientPhone ?? model.ClientId.ToString() });
        }

        /// <summary>
        /// تنفيذ عملية سحب نقدي من حساب العميل
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Teller,TellerWithdrawal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(TellerWithdrawalRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "بيانات السحب غير مكتملة أو غير صحيحة";
                return RedirectToAction(nameof(Index), new { query = model.ClientPhone ?? model.ClientId.ToString() });
            }

            var response = await _apiClient.TellerWithdrawAsync(model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تنفيذ عملية السحب بنجاح";
                if (response.Data != null)
                {
                    TempData["LastTrxJson"] = System.Text.Json.JsonSerializer.Serialize(response.Data);
                }
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشلت عملية السحب";
            }

            return RedirectToAction(nameof(Index), new { query = model.ClientPhone ?? model.ClientId.ToString() });
        }

        /// <summary>
        /// فتح وإضافة محفظة جديدة للعميل بعملة محددة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Teller,TellerDeposit,TellerWithdrawal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWallet(TellerAddWalletModel model, string? redirectPhone = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى اختيار العملة لفتح المحفظة";
                return RedirectToAction(nameof(Index), new { query = redirectPhone ?? model.ClientId.ToString() });
            }

            var response = await _apiClient.TellerAddWalletAsync(model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم فتح المحفظة الجديدة بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل فتح المحفظة";
            }

            return RedirectToAction(nameof(Index), new { query = redirectPhone ?? model.ClientId.ToString() });
        }
    }
}
