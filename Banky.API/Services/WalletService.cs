using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة إدارة المحافظ المالية
    /// مسؤولة عن استعراض المحافظ، إنشاء محافظ بعملات متعددة، وتنفيذ عمليات الإيداع وتغذية الرصيد
    /// </summary>
    public class WalletService : IWalletService
    {
        private readonly AppDbContext _context;

        public WalletService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// استرجاع محافظ العميل مع تفاصيل العملة والرموز
        /// </summary>
        public async Task<List<WalletResponseDto>> GetClientWalletsAsync(Guid clientId)
        {
            var wallets = await _context.Wallets
                .Include(w => w.Currency)
                .Where(w => w.ClientId == clientId && w.IsActive)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            return wallets.Select(w => new WalletResponseDto
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
        }

        /// <summary>
        /// إنشاء محفظة جديدة للعميل بعملة محددة مع التحقق من توثيق الحساب
        /// </summary>
        public async Task<(bool Success, string Message, WalletResponseDto? Data)> CreateWalletAsync(Guid clientId, CreateWalletDto dto)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "العميل غير موجود", null);

            // 1. التحقق من توثيق الحساب KYC (لا يمكن إنشاء محافظ إضافية إلا بعد الموافقة)
            if (client.KycStatus != "Approved")
            {
                return (false, "لا يمكن إنشاء محافظ جديدة إلا بعد توثيق الحساب والموافقة عليه من قبل الإدارة", null);
            }

            var currencyCode = dto.CurrencyCode.Trim().ToUpper();

            // 2. التحقق من أن العملة موجودة ونشطة في النظام
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == currencyCode && c.IsActive);

            if (currency == null)
            {
                return (false, "العملة المطلوبة غير متوفرة أو غير مفعلة في النظام حالياً", null);
            }

            // 3. التحقق من عدم وجود محفظة سابقة للعميل بهذه العملة
            var existingWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.ClientId == clientId && w.CurrencyCode == currencyCode);

            if (existingWallet != null)
            {
                return (false, $"لديك بالفعل محفظة نشطة بعملة ({currency.NameAr})", null);
            }

            // 4. إنشاء المحفظة الجديدة
            var wallet = new Wallet
            {
                ClientId = clientId,
                AccountNumber = Wallet.GenerateAccountNumber(),
                CurrencyCode = currencyCode,
                Balance = 0.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();

            var response = new WalletResponseDto
            {
                Id = wallet.Id,
                AccountNumber = wallet.AccountNumber,
                CurrencyCode = wallet.CurrencyCode,
                CurrencyNameAr = currency.NameAr,
                CurrencyNameEn = currency.NameEn,
                CurrencySymbol = currency.Symbol,
                Balance = wallet.Balance,
                IsActive = wallet.IsActive,
                CreatedAt = wallet.CreatedAt
            };

            return (true, $"تم إنشاء محفظة ({currency.NameAr}) بنجاح برقم حساب {wallet.AccountNumber}", response);
        }

        /// <summary>
        /// إيداع وتغذية رصيد في محفظة معينة وتسجيل حركة الإيداع
        /// </summary>
        public async Task<(bool Success, string Message, WalletResponseDto? Data)> DepositAsync(Guid? clientId, DepositDto dto)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Currency)
                .Include(w => w.Client)
                .FirstOrDefaultAsync(w => w.AccountNumber == dto.AccountNumber.Trim() && w.IsActive);

            if (wallet == null)
            {
                return (false, "رقم الحساب غير صحيح أو المحفظة غير نشطة", null);
            }

            // التحقق من توثيق حساب العميل
            if (wallet.Client != null && wallet.Client.KycStatus != "Approved")
            {
                return (false, "لا يمكن تغذية المحفظة لأن حساب العميل لم يتم توثيقه وقبوله بعد من قبل الإدارة", null);
            }

            // تحديث رصيد المحفظة
            wallet.Balance += dto.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // تسجيل حركة الإيداع في سجل المعاملات
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "Deposit",
                SenderClientId = null, // إيداع مباشر
                ReceiverClientId = wallet.ClientId,
                CurrencyCode = wallet.CurrencyCode,
                Amount = dto.Amount,
                Fee = 0.00m,
                TotalAmount = dto.Amount,
                Status = "Completed",
                ReceiverDisplayName = wallet.Client?.FullName,
                Note = dto.Note ?? "إيداع وتغذية رصيد في المحفظة",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(trx);
            await _context.SaveChangesAsync();

            var response = new WalletResponseDto
            {
                Id = wallet.Id,
                AccountNumber = wallet.AccountNumber,
                CurrencyCode = wallet.CurrencyCode,
                CurrencyNameAr = wallet.Currency?.NameAr ?? wallet.CurrencyCode,
                CurrencyNameEn = wallet.Currency?.NameEn ?? wallet.CurrencyCode,
                CurrencySymbol = wallet.Currency?.Symbol ?? wallet.CurrencyCode,
                Balance = wallet.Balance,
                IsActive = wallet.IsActive,
                CreatedAt = wallet.CreatedAt
            };

            return (true, $"تم إيداع مبلغ {dto.Amount:N2} {wallet.Currency?.Symbol} بنجاح. الرصيد الجديد: {wallet.Balance:N2}", response);
        }

        /// <summary>
        /// استرجاع العملات المفعلة التي لا يمتلك العميل محفظة بها بعد
        /// </summary>
        public async Task<List<CurrencyResponseDto>> GetAvailableCurrenciesForClientAsync(Guid clientId)
        {
            var existingCurrencyCodes = await _context.Wallets
                .Where(w => w.ClientId == clientId && w.IsActive)
                .Select(w => w.CurrencyCode)
                .ToListAsync();

            var available = await _context.Currencies
                .Where(c => c.IsActive && !existingCurrencyCodes.Contains(c.Code))
                .OrderBy(c => c.Id)
                .ToListAsync();

            return available.Select(c => new CurrencyResponseDto
            {
                Id = c.Id,
                Code = c.Code,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Symbol = c.Symbol,
                IsActive = c.IsActive,
                ExchangeRate = c.ExchangeRate
            }).ToList();
        }
    }
}
