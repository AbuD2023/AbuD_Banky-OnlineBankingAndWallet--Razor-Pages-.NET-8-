using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم مراقبة نقاط البيع (POS) في لوحة التحكم عبر الـ API
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class PosController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public PosController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// استعراض قائمة كافة نقاط البيع مع تفاصيل التجار والمبيعات
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAllPosPointsAsync();
            var list = response.Data ?? new List<AdminPosPointModel>();
            return View(list);
        }
    }
}
