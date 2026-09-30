using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم استعراض سجل الأنشطة والتدقيق للإدارة
    /// </summary>
    [Authorize(Roles = "Admin,Auditor")]
    public class AuditLogsController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public AuditLogsController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAuditLogsAsync();
            var logs = response.Data ?? new List<AuditLogModel>();
            return View(logs);
        }
    }
}
