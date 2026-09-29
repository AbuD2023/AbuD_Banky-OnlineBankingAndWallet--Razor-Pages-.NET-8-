using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة إدارة نقاط البيع (POS Service)
    /// تتيح للعملاء إنشاء نقاط بيع خاصة بهم لاستقبال المدفوعات واستعراض المبيعات
    /// </summary>
    public class PosService : IPosService
    {
        private readonly AppDbContext _context;

        public PosService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// إنشاء نقطة بيع جديدة وتوليد كود فريد لها
        /// </summary>
        public async Task<(bool Success, string Message, PosResponseDto? Data)> CreatePosAsync(Guid clientId, CreatePosDto dto)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "العميل غير موجود", null);

            // 1. التحقق من توثيق حساب العميل
            if (client.KycStatus != "Approved")
            {
                return (false, "لا يمكن إنشاء نقطة بيع إلا بعد توثيق الحساب والموافقة عليه من قبل الإدارة", null);
            }

            // 2. توليد كود فريد لنقطة البيع
            string posCode;
            do
            {
                posCode = PosPoint.GeneratePosCode();
            } while (await _context.PosPoints.AnyAsync(p => p.PosCode == posCode));

            // 3. حفظ نقطة البيع في قاعدة البيانات
            var pos = new PosPoint
            {
                ClientId = clientId,
                Name = dto.Name.Trim(),
                PosCode = posCode,
                Address = dto.Address?.Trim(),
                Category = dto.Category?.Trim() ?? "متجر عام",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PosPoints.AddAsync(pos);
            await _context.SaveChangesAsync();

            var response = new PosResponseDto
            {
                Id = pos.Id,
                Name = pos.Name,
                PosCode = pos.PosCode,
                Address = pos.Address,
                Category = pos.Category,
                IsActive = pos.IsActive,
                CreatedAt = pos.CreatedAt,
                TotalReceivedAmount = 0.00m,
                TotalTransactionsCount = 0
            };

            return (true, $"تم إنشاء نقطة البيع ({pos.Name}) بنجاح بكود: {pos.PosCode}", response);
        }

        /// <summary>
        /// استرجاع قائمة نقاط البيع المملوكة للعميل مع إحصائيات المبيعات المستلمة
        /// </summary>
        public async Task<List<PosResponseDto>> GetClientPosPointsAsync(Guid clientId)
        {
            var posList = await _context.PosPoints
                .Include(p => p.Transactions)
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return posList.Select(p => new PosResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                PosCode = p.PosCode,
                Address = p.Address,
                Category = p.Category,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                TotalReceivedAmount = p.Transactions.Where(t => t.Status == "Completed").Sum(t => t.Amount),
                TotalTransactionsCount = p.Transactions.Count(t => t.Status == "Completed")
            }).ToList();
        }

        /// <summary>
        /// جلب تفاصيل نقطة بيع معينة بواسطة المعرف
        /// </summary>
        public async Task<PosResponseDto?> GetPosByIdAsync(Guid posId)
        {
            var pos = await _context.PosPoints
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == posId);

            if (pos == null) return null;

            return new PosResponseDto
            {
                Id = pos.Id,
                Name = pos.Name,
                PosCode = pos.PosCode,
                Address = pos.Address,
                Category = pos.Category,
                IsActive = pos.IsActive,
                CreatedAt = pos.CreatedAt,
                TotalReceivedAmount = pos.Transactions.Where(t => t.Status == "Completed").Sum(t => t.Amount),
                TotalTransactionsCount = pos.Transactions.Count(t => t.Status == "Completed")
            };
        }
    }
}
