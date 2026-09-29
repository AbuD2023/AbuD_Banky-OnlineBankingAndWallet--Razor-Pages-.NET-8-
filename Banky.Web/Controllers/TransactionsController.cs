using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم استعراض ومراقبة سجل العمليات المالية والتحويلات في لوحة التحكم عبر الـ API
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class TransactionsController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public TransactionsController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// استعراض جدول الحركات المالية مع الفلاتر
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? currencyCode, string? type, DateTime? fromDate, DateTime? toDate)
        {
            var filter = new TransactionFilterModel
            {
                CurrencyCode = currencyCode,
                Type = type,
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = 100
            };

            var response = await _apiClient.GetAllTransactionsAsync(filter);
            var list = response.Data ?? new List<TransactionModel>();

            ViewBag.CurrencyCode = currencyCode;
            ViewBag.Type = type;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(list);
        }
    }
}
