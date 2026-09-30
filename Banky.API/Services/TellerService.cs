using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة موظف الصندوق والسحب والإيداع (Teller Service Implementation)
    /// </summary>
    public class TellerService : ITellerService
    {
        private readonly AppDbContext _context;
        private readonly IAdminService _adminService;

        public TellerService(AppDbContext context, IAdminService adminService)
        {
            _context = context;
            _adminService = adminService;
        }

        /// <summary>
        /// الاستعلام والبحث عن العميل برقم الهاتف أو البريد أو المعرف
        /// </summary>
        public async Task<TellerClientSummaryDto?> SearchClientAsync(string query)
        {
            var clean = query.Trim();

            Guid queryGuid = Guid.Empty;
            bool isGuid = Guid.TryParse(clean, out queryGuid);

            var client = await _context.Clients
                .Include(c => c.Wallets)
                    .ThenInclude(w => w.Currency)
                .Include(c => c.Devices)
                .FirstOrDefaultAsync(c =>
                    (isGuid && c.Id == queryGuid) ||
                    c.Phone == clean ||
                    c.Email.ToLower() == clean.ToLower() ||
                    c.Wallets.Any(w => w.AccountNumber == clean)
                );

            if (client == null) return null;

            // جلب أحدث العمليات المرتبطة بالعميل
            var recentTrx = await _context.Transactions
                .Where(t => t.SenderClientId == client.Id || t.ReceiverClientId == client.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            var walletDtos = client.Wallets.Select(w => new WalletResponseDto
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

            var trxDtos = recentTrx.Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                Type = t.Type,
                TypeNameAr = t.Type switch
                {
                    "Deposit" => "إيداع وتغذية",
                    "Withdrawal" => "سحب نقدي",
                    "TransferByPhone" => "تحويل لمشترك",
                    "SelfExchange" => "مصارفة بين الحسابات",
                    "PosPayment" => "دفع لنقطة بيع",
                    _ => t.Type
                },
                CurrencyCode = t.CurrencyCode,
                CurrencySymbol = t.CurrencyCode,
                Amount = t.Amount,
                Fee = t.Fee,
                TotalAmount = t.TotalAmount,
                Status = t.Status,
                StatusNameAr = "مكتملة",
                SenderDisplayName = t.SenderDisplayName,
                SenderDisplayPhone = t.SenderDisplayPhone,
                ReceiverDisplayName = t.ReceiverDisplayName,
                Note = t.Note,
                IsIncoming = t.ReceiverClientId == client.Id,
                CreatedAt = t.CreatedAt
            }).ToList();

            return new TellerClientSummaryDto
            {
                ClientId = client.Id,
                FullName = client.FullName,
                Phone = client.Phone,
                Email = client.Email,
                KycStatus = client.KycStatus,
                ProfileImage = client.ProfileImage,
                IsBlocked = client.IsBlocked,
                HideFullName = client.HideFullName,
                CreatedAt = client.CreatedAt,
                Wallets = walletDtos,
                RecentTellerTransactions = trxDtos
            };
        }

        /// <summary>
        /// تنفيذ عملية إيداع وتغذية رصيد للعميل بأي عملة معتمدة
        /// </summary>
        public async Task<(bool Success, string Message, TransactionResponseDto? Data)> DepositAsync(TellerDepositRequestDto dto, Guid? tellerId = null, string tellerName = "Teller")
        {
            var client = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Id == dto.ClientId);

            if (client == null)
            {
                return (false, "العميل غير موجود في النظام", null);
            }

            if (client.IsBlocked)
            {
                return (false, "حساب العميل محظور ولا يمكن تنفيذ إيداع له حالياً", null);
            }

            var currencyCode = dto.CurrencyCode.Trim().ToUpper();
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode && c.IsActive);
            if (currency == null)
            {
                return (false, $"العملة ({currencyCode}) غير معتمدة أو غير نشطة في النظام", null);
            }

            // احتساب رسوم الإيداع إن وجدت
            var feeRule = await _adminService.GetFeeRuleAsync("Deposit", currencyCode);
            decimal fee = feeRule?.CalculateFee(dto.Amount) ?? 0.00m;

            // جلب أو فتح محفظة للعميل بتلك العملة تلقائياً
            var wallet = client.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    ClientId = client.Id,
                    AccountNumber = Wallet.GenerateAccountNumber(),
                    CurrencyCode = currencyCode,
                    Balance = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(wallet);
            }

            // إيداع المبلغ الصافي في محفظة العميل
            wallet.Balance += dto.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // تسجيل حركة الإيداع
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "Deposit",
                ReceiverClientId = client.Id,
                CurrencyCode = currencyCode,
                Amount = dto.Amount,
                Fee = fee,
                TotalAmount = dto.Amount,
                Status = "Completed",
                SenderDisplayName = $"صندوق البنك ({tellerName})",
                SenderDisplayPhone = "BANK-TELLER",
                ReceiverDisplayName = client.FullName,
                Note = dto.Note ?? $"إيداع نقدي عبر الصندوق بواسطة {tellerName}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(trx);
            await _context.SaveChangesAsync();

            // توثيق العملية في سجل التدقيق
            await _adminService.LogActivityAsync(
                tellerId,
                tellerName,
                "Teller",
                "Deposit",
                "Transaction",
                trx.Id.ToString(),
                $"تم إيداع مبلغ {dto.Amount:N2} {currency.Symbol} لحساب العميل {client.FullName} ({client.Phone})"
            );

            var response = new TransactionResponseDto
            {
                Id = trx.Id,
                TransactionNumber = trx.TransactionNumber,
                Type = trx.Type,
                TypeNameAr = "إيداع وتغذية",
                CurrencyCode = trx.CurrencyCode,
                CurrencySymbol = currency.Symbol,
                Amount = trx.Amount,
                Fee = trx.Fee,
                TotalAmount = trx.TotalAmount,
                Status = trx.Status,
                StatusNameAr = "مكتملة",
                SenderDisplayName = trx.SenderDisplayName,
                SenderDisplayPhone = trx.SenderDisplayPhone,
                ReceiverDisplayName = client.FullName,
                Note = trx.Note,
                IsIncoming = true,
                CreatedAt = trx.CreatedAt
            };

            return (true, $"تم إيداع مبلغ {dto.Amount:N2} {currency.Symbol} بنجاح في حساب العميل ({client.FullName})", response);
        }

        /// <summary>
        /// تنفيذ عملية سحب نقدي من محفظة العميل
        /// </summary>
        public async Task<(bool Success, string Message, TransactionResponseDto? Data)> WithdrawAsync(TellerWithdrawalRequestDto dto, Guid? tellerId = null, string tellerName = "Teller")
        {
            var client = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Id == dto.ClientId);

            if (client == null)
            {
                return (false, "العميل غير موجود في النظام", null);
            }

            if (client.IsBlocked)
            {
                return (false, "حساب العميل محظور من تنفيذ السحوبات النقدية", null);
            }

            if (client.KycStatus != "Approved")
            {
                return (false, "لا يمكن السحب النقدي لأن حساب العميل لم يتم توثيقه وقبوله بعد (KYC)", null);
            }

            var currencyCode = dto.CurrencyCode.Trim().ToUpper();
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode && c.IsActive);
            if (currency == null)
            {
                return (false, $"العملة ({currencyCode}) غير معتمدة أو غير نشطة", null);
            }

            var wallet = client.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (wallet == null)
            {
                return (false, $"العميل لا يمتلك محفظة نشطة بعملة ({currencyCode})", null);
            }

            // احتساب رسوم السحب النقدي
            var feeRule = await _adminService.GetFeeRuleAsync("Withdrawal", currencyCode);
            decimal fee = feeRule?.CalculateFee(dto.Amount) ?? 0.00m;
            decimal totalDebit = dto.Amount + fee;

            if (wallet.Balance < totalDebit)
            {
                return (false, $"رصيد العميل غير كافٍ. المطلوب شامل الرسوم ({fee:N2}): {totalDebit:N2} {currency.Symbol} والرصيد المتاح: {wallet.Balance:N2} {currency.Symbol}", null);
            }

            // خصم المبلغ + الرسوم من محفظة العميل
            wallet.Balance -= totalDebit;
            wallet.UpdatedAt = DateTime.UtcNow;

            // تسجيل حركة السحب
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "Withdrawal",
                SenderClientId = client.Id,
                CurrencyCode = currencyCode,
                Amount = dto.Amount,
                Fee = fee,
                TotalAmount = totalDebit,
                Status = "Completed",
                SenderDisplayName = client.FullName,
                SenderDisplayPhone = client.Phone,
                ReceiverDisplayName = $"صندوق البنك ({tellerName})",
                Note = dto.Note ?? $"سحب نقدي عبر الصندوق بواسطة {tellerName}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(trx);
            await _context.SaveChangesAsync();

            // توثيق العملية في سجل التدقيق
            await _adminService.LogActivityAsync(
                tellerId,
                tellerName,
                "Teller",
                "Withdrawal",
                "Transaction",
                trx.Id.ToString(),
                $"تم سحب نقدي بمبلغ {dto.Amount:N2} {currency.Symbol} (رسوم: {fee:N2}) من حساب العميل {client.FullName} ({client.Phone})"
            );

            var response = new TransactionResponseDto
            {
                Id = trx.Id,
                TransactionNumber = trx.TransactionNumber,
                Type = trx.Type,
                TypeNameAr = "سحب نقدي",
                CurrencyCode = trx.CurrencyCode,
                CurrencySymbol = currency.Symbol,
                Amount = trx.Amount,
                Fee = trx.Fee,
                TotalAmount = trx.TotalAmount,
                Status = trx.Status,
                StatusNameAr = "مكتملة",
                SenderDisplayName = client.FullName,
                SenderDisplayPhone = client.Phone,
                ReceiverDisplayName = trx.ReceiverDisplayName,
                Note = trx.Note,
                IsIncoming = false,
                CreatedAt = trx.CreatedAt
            };

            return (true, $"تم تنفيذ السحب النقدي بمبلغ {dto.Amount:N2} {currency.Symbol} بنجاح للعميل ({client.FullName})", response);
        }

        /// <summary>
        /// فتح وإضافة محفظة جديدة للعميل بعملة محددة
        /// </summary>
        public async Task<(bool Success, string Message, WalletResponseDto? Data)> AddWalletAsync(TellerAddWalletDto dto, Guid? tellerId = null, string tellerName = "Teller")
        {
            var client = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Id == dto.ClientId);

            if (client == null)
            {
                return (false, "العميل غير موجود", null);
            }

            var currencyCode = dto.CurrencyCode.Trim().ToUpper();
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode && c.IsActive);
            if (currency == null)
            {
                return (false, "العملة المختارة غير معتمدة في النظام", null);
            }

            if (client.Wallets.Any(w => w.CurrencyCode == currencyCode && w.IsActive))
            {
                return (false, $"العميل يمتلك بالفعل محفظة نشطة بعملة ({currency.NameAr})", null);
            }

            var newWallet = new Wallet
            {
                ClientId = client.Id,
                AccountNumber = Wallet.GenerateAccountNumber(),
                CurrencyCode = currencyCode,
                Balance = 0.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Wallets.AddAsync(newWallet);
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(
                tellerId,
                tellerName,
                "Teller",
                "CreateWallet",
                "Wallet",
                newWallet.Id.ToString(),
                $"تم فتح محفظة جديدة بعملة {currency.NameAr} ({currency.Code}) للعميل {client.FullName}"
            );

            var response = new WalletResponseDto
            {
                Id = newWallet.Id,
                AccountNumber = newWallet.AccountNumber,
                CurrencyCode = newWallet.CurrencyCode,
                CurrencyNameAr = currency.NameAr,
                CurrencyNameEn = currency.NameEn,
                CurrencySymbol = currency.Symbol,
                Balance = newWallet.Balance,
                IsActive = newWallet.IsActive,
                CreatedAt = newWallet.CreatedAt
            };

            return (true, $"تم فتح محفظة ({currency.NameAr}) بنجاح للعميل", response);
        }

        /// <summary>
        /// استرجاع أحدث عمليات السحب والإيداع المنفذة عبر الصندوق
        /// </summary>
        public async Task<List<TransactionResponseDto>> GetTellerRecentOperationsAsync(Guid? tellerId = null)
        {
            var query = _context.Transactions
                .Include(t => t.SenderClient)
                .Include(t => t.ReceiverClient)
                .Where(t => t.Type == "Deposit" || t.Type == "Withdrawal")
                .OrderByDescending(t => t.CreatedAt)
                .Take(50);

            var transactions = await query.ToListAsync();

            return transactions.Select(t =>
            {
                var curr = _context.Currencies.FirstOrDefault(c => c.Code == t.CurrencyCode);
                return new TransactionResponseDto
                {
                    Id = t.Id,
                    TransactionNumber = t.TransactionNumber,
                    Type = t.Type,
                    TypeNameAr = t.Type == "Deposit" ? "إيداع وتغذية" : "سحب نقدي",
                    CurrencyCode = t.CurrencyCode,
                    CurrencySymbol = curr?.Symbol ?? t.CurrencyCode,
                    Amount = t.Amount,
                    Fee = t.Fee,
                    TotalAmount = t.TotalAmount,
                    Status = t.Status,
                    StatusNameAr = "مكتملة",
                    SenderDisplayName = t.SenderDisplayName,
                    SenderDisplayPhone = t.SenderDisplayPhone,
                    ReceiverDisplayName = t.ReceiverDisplayName,
                    Note = t.Note,
                    IsIncoming = t.Type == "Deposit",
                    CreatedAt = t.CreatedAt
                };
            }).ToList();
        }
    }
}
