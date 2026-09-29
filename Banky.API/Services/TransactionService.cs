using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة تتبع وسجل المعاملات والعمليات المصرفية
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// استرجاع الحركات المالية للعميل مع الفلاتر وتسميات العملات والأنواع باللغة العربية
        /// </summary>
        public async Task<List<TransactionResponseDto>> GetClientTransactionsAsync(Guid clientId, TransactionFilterDto? filter = null)
        {
            var query = _context.Transactions
                .Include(t => t.PosPoint)
                .Where(t => t.SenderClientId == clientId || t.ReceiverClientId == clientId);

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
                {
                    query = query.Where(t => t.CurrencyCode == filter.CurrencyCode.ToUpper());
                }

                if (!string.IsNullOrWhiteSpace(filter.Type))
                {
                    query = query.Where(t => t.Type == filter.Type);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);
                }
            }

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Take(filter?.PageSize ?? 50)
                .ToListAsync();

            var currencies = await _context.Currencies.ToDictionaryAsync(c => c.Code, c => c.Symbol);

            return transactions.Select(t =>
            {
                bool isIncoming = t.ReceiverClientId == clientId;
                string typeNameAr = t.Type switch
                {
                    "TransferByPhone" => isIncoming ? "حوالة واردة من مشترك" : "حوالة صادرة لمشترك",
                    "PosPayment" => isIncoming ? "مبيعات نقطة بيع واردة" : "دفع مشتريات عبر POS",
                    "Deposit" => "إيداع وتغذية رصيد",
                    "Withdrawal" => "سحب نقدي",
                    _ => t.Type
                };

                string statusNameAr = t.Status switch
                {
                    "Completed" => "ناجحة",
                    "Pending" => "قيد الانتظار",
                    "Failed" => "فاشلة",
                    _ => t.Status
                };

                return new TransactionResponseDto
                {
                    Id = t.Id,
                    TransactionNumber = t.TransactionNumber,
                    Type = t.Type,
                    TypeNameAr = typeNameAr,
                    CurrencyCode = t.CurrencyCode,
                    CurrencySymbol = currencies.ContainsKey(t.CurrencyCode) ? currencies[t.CurrencyCode] : t.CurrencyCode,
                    Amount = t.Amount,
                    Fee = t.Fee,
                    TotalAmount = t.TotalAmount,
                    Status = t.Status,
                    StatusNameAr = statusNameAr,
                    SenderDisplayName = t.SenderDisplayName,
                    SenderDisplayPhone = t.SenderDisplayPhone,
                    ReceiverDisplayName = t.ReceiverDisplayName,
                    PosName = t.PosPoint?.Name,
                    Note = t.Note,
                    IsIncoming = isIncoming,
                    CreatedAt = t.CreatedAt
                };
            }).ToList();
        }

        /// <summary>
        /// استرجاع تفاصيل حركة مالية معينة
        /// </summary>
        public async Task<TransactionResponseDto?> GetTransactionDetailsAsync(Guid clientId, Guid transactionId)
        {
            var t = await _context.Transactions
                .Include(t => t.PosPoint)
                .FirstOrDefaultAsync(t => t.Id == transactionId && (t.SenderClientId == clientId || t.ReceiverClientId == clientId));

            if (t == null) return null;

            bool isIncoming = t.ReceiverClientId == clientId;
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == t.CurrencyCode);

            return new TransactionResponseDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                Type = t.Type,
                TypeNameAr = t.Type switch
                {
                    "TransferByPhone" => isIncoming ? "حوالة واردة من مشترك" : "حوالة صادرة لمشترك",
                    "PosPayment" => isIncoming ? "مبيعات نقطة بيع" : "دفع مشتريات عبر POS",
                    "Deposit" => "إيداع وتغذية رصيد",
                    _ => t.Type
                },
                CurrencyCode = t.CurrencyCode,
                CurrencySymbol = currency?.Symbol ?? t.CurrencyCode,
                Amount = t.Amount,
                Fee = t.Fee,
                TotalAmount = t.TotalAmount,
                Status = t.Status,
                StatusNameAr = t.Status == "Completed" ? "ناجحة" : t.Status,
                SenderDisplayName = t.SenderDisplayName,
                SenderDisplayPhone = t.SenderDisplayPhone,
                ReceiverDisplayName = t.ReceiverDisplayName,
                PosName = t.PosPoint?.Name,
                Note = t.Note,
                IsIncoming = isIncoming,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
