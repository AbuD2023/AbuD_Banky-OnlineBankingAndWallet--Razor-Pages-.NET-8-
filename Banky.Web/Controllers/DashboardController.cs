using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم الصفحة الرئيسية للوحة التحكم والإحصائيات عبر استدعاء الـ API
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public DashboardController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// عرض لوحة الإحصائيات الشاملة من الـ API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetDashboardStatsAsync();
            var stats = response.Data ?? new AdminDashboardStatsModel();

            return View(stats);
        }
    }
}
