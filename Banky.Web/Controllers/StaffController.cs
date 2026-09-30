using Banky.Web.Models;
using Banky.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banky.Web.Controllers
{
    /// <summary>
    /// متحكم إدارة الموظفين وصلاحياتهم في لوحة التحكم
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly IBankyApiClient _apiClient;

        public StaffController(IBankyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// استعراض قائمة الموظفين
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAllStaffAsync();
            var staff = response.Data ?? new List<StaffUserModel>();
            return View(staff);
        }

        /// <summary>
        /// إضافة موظف جديد
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى تعبئة كافة حقول بيانات الموظف بشكل صحيح";
                return RedirectToAction("Index");
            }

            var response = await _apiClient.CreateStaffAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم إنشاء حساب الموظف بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل إنشاء حساب الموظف";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// تعديل دور ورتبة الموظف
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(UpdateStaffRoleModel model)
        {
            var response = await _apiClient.UpdateStaffRoleAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تعديل صلاحية الموظف بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تعديل صلاحية الموظف";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// تجميد أو تفعيل حساب موظف
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var response = await _apiClient.ToggleStaffStatusAsync(id);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "تم تعديل حالة حساب الموظف بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message ?? "فشل تعديل حالة حساب الموظف";
            }

            return RedirectToAction("Index");
        }
    }
}
