using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة العمليات الإدارية ولوحة التحكم
    /// تشمل مراجعة وتوثيق الهويات، إدارة المستخدمين والمحافظ، إعادة تعيين كلمات المرور، وإدارة العملات
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// استرجاع تقرير وإحصائيات لوحة التحكم العامة
        /// </summary>
        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            var totalClients = await _context.Clients.CountAsync(c => c.Role == "Client");
            var pendingKyc = await _context.Clients.CountAsync(c => c.KycStatus == "PendingApproval");
            var approvedKyc = await _context.Clients.CountAsync(c => c.KycStatus == "Approved");
            var totalPos = await _context.PosPoints.CountAsync();
            var totalTrx = await _context.Transactions.CountAsync();

            var yerTotal = await _context.Transactions
                .Where(t => t.CurrencyCode == "YER" && t.Status == "Completed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var sarTotal = await _context.Transactions
                .Where(t => t.CurrencyCode == "SAR" && t.Status == "Completed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var usdTotal = await _context.Transactions
                .Where(t => t.CurrencyCode == "USD" && t.Status == "Completed")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var recentKyc = await _context.Clients
                .Where(c => c.KycStatus == "PendingApproval")
                .OrderByDescending(c => c.KycSubmittedAt)
                .Take(5)
                .Select(c => new RecentKycRequestDto
                {
                    ClientId = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    KycStatus = c.KycStatus,
                    KycIdFront = c.KycIdFront,
                    KycIdBack = c.KycIdBack,
                    KycSubmittedAt = c.KycSubmittedAt
                })
                .ToListAsync();

            var recentTrx = await _context.Transactions
                .Include(t => t.PosPoint)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    TransactionNumber = t.TransactionNumber,
                    Type = t.Type,
                    TypeNameAr = t.Type == "TransferByPhone" ? "تحويل لمشترك" : t.Type == "PosPayment" ? "دفع لنقطة بيع" : "إيداع",
                    CurrencyCode = t.CurrencyCode,
                    CurrencySymbol = t.CurrencyCode,
                    Amount = t.Amount,
                    Fee = t.Fee,
                    TotalAmount = t.TotalAmount,
                    Status = t.Status,
                    StatusNameAr = t.Status == "Completed" ? "ناجحة" : t.Status,
                    SenderDisplayName = t.SenderDisplayName,
                    SenderDisplayPhone = t.SenderDisplayPhone,
                    ReceiverDisplayName = t.ReceiverDisplayName,
                    PosName = t.PosPoint != null ? t.PosPoint.Name : null,
                    Note = t.Note,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return new AdminDashboardStatsDto
            {
                TotalClientsCount = totalClients,
                PendingKycCount = pendingKyc,
                ApprovedKycCount = approvedKyc,
                TotalPosCount = totalPos,
                TotalTransactionsCount = totalTrx,
                TotalTransferredAmountYer = yerTotal,
                TotalTransferredAmountSar = sarTotal,
                TotalTransferredAmountUsd = usdTotal,
                RecentKycRequests = recentKyc,
                RecentTransactions = recentTrx
            };
        }

        /// <summary>
        /// استرجاع كافة طلبات التوثيق المعلقة قيد المراجعة مع صور الهوية
        /// </summary>
        public async Task<List<RecentKycRequestDto>> GetPendingKycRequestsAsync()
        {
            return await _context.Clients
                .Where(c => c.KycStatus == "PendingApproval")
                .OrderByDescending(c => c.KycSubmittedAt)
                .Select(c => new RecentKycRequestDto
                {
                    ClientId = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    KycStatus = c.KycStatus,
                    KycIdFront = c.KycIdFront,
                    KycIdBack = c.KycIdBack,
                    KycSubmittedAt = c.KycSubmittedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// اتخاذ قرار مراجعة التوثيق (قبول أو رفض مع إبداء السبب)
        /// </summary>
        public async Task<(bool Success, string Message)> ReviewKycAsync(KycReviewDto dto)
        {
            var client = await _context.Clients.FindAsync(dto.ClientId);
            if (client == null) return (false, "العميل غير موجود");

            if (dto.IsApproved)
            {
                client.KycStatus = "Approved";
                client.KycRejectionReason = null;
                client.KycReviewedAt = DateTime.UtcNow;
                client.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return (true, $"تم قبول وتوثيق حساب العميل ({client.FullName}) بنجاح ويمكنه الآن استخدام كافة ميزات التطبيق");
            }
            else
            {
                client.KycStatus = "Rejected";
                client.KycRejectionReason = dto.RejectionReason?.Trim() ?? "الوثائق المرفقة غير واضحة أو غير مطابقة للبيانات";
                client.KycReviewedAt = DateTime.UtcNow;
                client.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return (true, $"تم رفض توثيق حساب العميل ({client.FullName}). السبب: {client.KycRejectionReason}");
            }
        }

        /// <summary>
        /// استرجاع قائمة العملاء وتفاصيلهم
        /// </summary>
        public async Task<List<ClientProfileDto>> GetAllClientsAsync()
        {
            var clients = await _context.Clients
                .Where(c => c.Role == "Client")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return clients.Select(c => new ClientProfileDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                Role = c.Role,
                ProfileImage = c.ProfileImage,
                KycStatus = c.KycStatus,
                KycIdFront = c.KycIdFront,
                KycIdBack = c.KycIdBack,
                KycRejectionReason = c.KycRejectionReason,
                HideFullName = c.HideFullName,
                HidePhoneOnPos = c.HidePhoneOnPos,
                PosAliasPhone = c.PosAliasPhone,
                IsBiometricEnabled = c.IsBiometricEnabled,
                MustChangePassword = c.MustChangePassword,
                IsBlocked = c.IsBlocked,
                BlockedMessage = c.BlockedMessage,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// استرجاع تفاصيل عميل محدد ومحافظه ونقاط بيعه وعملياته
        /// </summary>
        public async Task<ClientDetailsDto?> GetClientDetailsAsync(Guid clientId)
        {
            var client = await _context.Clients
                .Include(c => c.Wallets)
                    .ThenInclude(w => w.Currency)
                .Include(c => c.PosPoints)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null) return null;

            var clientProfile = new ClientProfileDto
            {
                Id = client.Id,
                FullName = client.FullName,
                Email = client.Email,
                Phone = client.Phone,
                Role = client.Role,
                ProfileImage = client.ProfileImage,
                KycStatus = client.KycStatus,
                KycIdFront = client.KycIdFront,
                KycIdBack = client.KycIdBack,
                KycRejectionReason = client.KycRejectionReason,
                HideFullName = client.HideFullName,
                HidePhoneOnPos = client.HidePhoneOnPos,
                PosAliasPhone = client.PosAliasPhone,
                IsBiometricEnabled = client.IsBiometricEnabled,
                MustChangePassword = client.MustChangePassword,
                IsBlocked = client.IsBlocked,
                BlockedMessage = client.BlockedMessage,
                CreatedAt = client.CreatedAt
            };

            var wallets = client.Wallets.Select(w => new WalletResponseDto
            {
                Id = w.Id,
                AccountNumber = w.AccountNumber,
                CurrencyCode = w.CurrencyCode,
                CurrencyNameAr = w.Currency?.NameAr ?? w.CurrencyCode,
                CurrencyNameEn = w.Currency?.NameEn ?? w.CurrencyCode,
                CurrencySymbol = w.Currency?.Symbol ?? w.CurrencyCode,
                Balance = w.Balance,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt
            }).ToList();

            var posPoints = client.PosPoints.Select(p => new PosResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                PosCode = p.PosCode,
                Address = p.Address,
                Category = p.Category,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                TotalReceivedAmount = 0,
                TotalTransactionsCount = 0
            }).ToList();

            var transactions = await _context.Transactions
                .Include(t => t.PosPoint)
                .Where(t => t.SenderClientId == clientId || t.ReceiverClientId == clientId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();

            var currencies = await _context.Currencies.ToDictionaryAsync(c => c.Code, c => c.Symbol);

            var trxDtos = transactions.Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                Type = t.Type,
                TypeNameAr = t.Type == "TransferByPhone" ? (t.ReceiverClientId == clientId ? "حوالة واردة" : "حوالة صادرة") : (t.Type == "PosPayment" ? "دفع لنقطة بيع" : "إيداع"),
                CurrencyCode = t.CurrencyCode,
                CurrencySymbol = currencies.ContainsKey(t.CurrencyCode) ? currencies[t.CurrencyCode] : t.CurrencyCode,
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
                IsIncoming = t.ReceiverClientId == clientId,
                CreatedAt = t.CreatedAt
            }).ToList();

            return new ClientDetailsDto
            {
                Client = clientProfile,
                Wallets = wallets,
                PosPoints = posPoints,
                RecentTransactions = trxDtos
            };
        }

        /// <summary>
        /// استرجاع كافة نقاط البيع المسجلة في النظام
        /// </summary>
        public async Task<List<AdminPosPointDto>> GetAllPosPointsAsync()
        {
            var posPoints = await _context.PosPoints
                .Include(p => p.Owner)
                .Include(p => p.Transactions)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return posPoints.Select(p => new AdminPosPointDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                Name = p.Name,
                PosCode = p.PosCode,
                Address = p.Address,
                Category = p.Category,
                IsActive = p.IsActive,
                MerchantName = p.Owner?.FullName ?? string.Empty,
                MerchantPhone = p.Owner?.Phone ?? string.Empty,
                TotalSalesAmount = p.Transactions.Where(t => t.Status == "Completed").Sum(t => t.Amount),
                TotalSalesCount = p.Transactions.Count(t => t.Status == "Completed"),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// استرجاع كافة الحركات المالية في النظام مع إمكانية الفلترة
        /// </summary>
        public async Task<List<TransactionResponseDto>> GetAllTransactionsAsync(TransactionFilterDto? filter = null)
        {
            var query = _context.Transactions.Include(t => t.PosPoint).AsQueryable();

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
                .Take(filter?.PageSize ?? 100)
                .ToListAsync();

            var currencies = await _context.Currencies.ToDictionaryAsync(c => c.Code, c => c.Symbol);

            return transactions.Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                Type = t.Type,
                TypeNameAr = t.Type switch
                {
                    "TransferByPhone" => "تحويل لمشترك",
                    "PosPayment" => "دفع لنقطة بيع",
                    "Deposit" => "إيداع وتغذية رصيد",
                    _ => t.Type
                },
                CurrencyCode = t.CurrencyCode,
                CurrencySymbol = currencies.ContainsKey(t.CurrencyCode) ? currencies[t.CurrencyCode] : t.CurrencyCode,
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
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور للعميل بواسطة المسؤول وإجباره على تغييرها عند أول دخول
        /// </summary>
        public async Task<(bool Success, string Message, string TempPassword)> ResetClientPasswordAsync(Guid clientId, string? customTempPassword = null)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "العميل غير موجود", string.Empty);

            var tempPassword = !string.IsNullOrWhiteSpace(customTempPassword) ? customTempPassword.Trim() : "123456";

            client.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            client.MustChangePassword = true; // إجبار العميل على تغييرها فور تسجيل الدخول
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, $"تمت إعادة تعيين كلمة المرور للعميل ({client.FullName}) إلى كلمة المرور المؤقتة: {tempPassword}، وسيتم إجباره على تغييرها فور دخوله", tempPassword);
        }

        /// <summary>
        /// حظر أو فك حظر حساب العميل
        /// </summary>
        public async Task<(bool Success, string Message)> ToggleBlockClientAsync(BlockClientDto dto)
        {
            var client = await _context.Clients.FindAsync(dto.ClientId);
            if (client == null) return (false, "العميل غير موجود");

            client.IsBlocked = dto.IsBlocked;
            client.BlockedMessage = dto.IsBlocked ? (dto.BlockedMessage ?? "تم حظر الحساب لمخالفة شروط الاستخدام") : null;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, dto.IsBlocked ? $"تم حظر حساب العميل ({client.FullName})" : $"تم إلغاء حظر حساب العميل ({client.FullName}) بنجاح");
        }

        /// <summary>
        /// استرجاع كافة العملات
        /// </summary>
        public async Task<List<Currency>> GetAllCurrenciesAsync()
        {
            return await _context.Currencies.OrderBy(c => c.Id).ToListAsync();
        }

        /// <summary>
        /// إضافة عملة جديدة إلى النظام المالي
        /// </summary>
        public async Task<(bool Success, string Message, Currency? Data)> CreateCurrencyAsync(CurrencyCreateDto dto)
        {
            var code = dto.Code.Trim().ToUpper();
            if (await _context.Currencies.AnyAsync(c => c.Code == code))
            {
                return (false, "رمز العملة مضاف مسبقاً في النظام", null);
            }

            var currency = new Currency
            {
                Code = code,
                NameAr = dto.NameAr.Trim(),
                NameEn = dto.NameEn.Trim(),
                Symbol = dto.Symbol.Trim(),
                IsActive = dto.IsActive,
                ExchangeRate = dto.ExchangeRate,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Currencies.AddAsync(currency);
            await _context.SaveChangesAsync();

            return (true, $"تم إضافة عملة ({currency.NameAr} - {currency.Code}) بنجاح", currency);
        }

        /// <summary>
        /// تعديل بيانات عملة موجودة
        /// </summary>
        public async Task<(bool Success, string Message, Currency? Data)> UpdateCurrencyAsync(int id, CurrencyUpdateDto dto)
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null) return (false, "العملة غير موجودة", null);

            currency.NameAr = dto.NameAr.Trim();
            currency.NameEn = dto.NameEn.Trim();
            currency.Symbol = dto.Symbol.Trim();
            currency.IsActive = dto.IsActive;
            currency.ExchangeRate = dto.ExchangeRate;

            await _context.SaveChangesAsync();
            return (true, $"تم تحديث بيانات عملة ({currency.NameAr}) بنجاح", currency);
        }

        /// <summary>
        /// تفعيل أو تعطيل عملة في النظام
        /// </summary>
        public async Task<(bool Success, string Message)> ToggleCurrencyStatusAsync(int id)
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null) return (false, "العملة غير موجودة");

            currency.IsActive = !currency.IsActive;
            await _context.SaveChangesAsync();

            return (true, $"تم {(currency.IsActive ? "تفعيل" : "تعطيل")} عملة ({currency.NameAr}) بنجاح");
        }
    }
}
