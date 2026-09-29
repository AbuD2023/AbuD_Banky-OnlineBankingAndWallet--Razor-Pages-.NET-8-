using Banky.API.DTOs;

namespace Banky.API.Services
{
    /// <summary>
    /// واجهة خدمة إدارة نقاط البيع للعملاء والتجار
    /// </summary>
    public interface IPosService
    {
        /// <summary>
        /// إنشاء نقطة بيع جديدة للعميل
        /// </summary>
        Task<(bool Success, string Message, PosResponseDto? Data)> CreatePosAsync(Guid clientId, CreatePosDto dto);

        /// <summary>
        /// استرجاع نقاط البيع المملوكة للعميل الحالي مع إحصائيات المبيعات
        /// </summary>
        Task<List<PosResponseDto>> GetClientPosPointsAsync(Guid clientId);

        /// <summary>
        /// جلب تفاصيل نقطة بيع معينة
        /// </summary>
        Task<PosResponseDto?> GetPosByIdAsync(Guid posId);
    }
}
