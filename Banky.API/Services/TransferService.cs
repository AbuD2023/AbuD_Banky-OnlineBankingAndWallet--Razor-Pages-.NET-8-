using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة تنفيذ التحويلات المالية والمدفوعات
    /// تدير عمليات التحويل المباشر برقم الهاتف، الدفع لنقاط البيع، وتطبيق معايير الخصوصية (إخفاء الاسم بالرموز والرقم البديل)
    /// </summary>
    public class TransferService : ITransferService
    {
        private readonly AppDbContext _context;

        public TransferService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// الاستعلام عن المستلم برقم الهاتف مع تطبيق ميزة إخفاء الاسم بالحروف الأولى إذا كانت مفعلة لدى المستلم
        /// </summary>
        public async Task<(bool Success, string Message, RecipientLookupResponseDto? Data)> LookupRecipientByPhoneAsync(Guid senderId, string phone, string currencyCode)
        {
            var cleanPhone = phone.Trim();

            // 1. البحث عن المستلم
            var receiver = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Phone == cleanPhone);

            if (receiver == null)
            {
                return (false, "رقم الهاتف غير مسجل في تطبيق Banky", null);
            }

            if (receiver.Id == senderId)
            {
                return (false, "لا يمكنك التحويل لنفسك", null);
            }

            if (receiver.IsBlocked)
            {
                return (false, "حساب المستلم محظور حالياً ولا يمكن استلام مبالغ عليه", null);
            }

            if (receiver.KycStatus != "Approved")
            {
                return (false, "حساب المستلم لم يتم توثيقه وقبوله بعد من قبل الإدارة", null);
            }

            // تطبيق ميزة إخفاء الاسم (إذا كان المستلم قد فعل خيار إخفاء الاسم، نعيد الحروف الأولى من اسمه الرباعي فقط)
            var displayName = receiver.HideFullName ? receiver.GetMaskedName() : receiver.FullName;

            var hasWalletInCurrency = receiver.Wallets.Any(w => w.CurrencyCode == currencyCode.ToUpper() && w.IsActive);

            var response = new RecipientLookupResponseDto
            {
                ClientId = receiver.Id,
                DisplayName = displayName,
                Phone = receiver.Phone,
                IsNameMasked = receiver.HideFullName,
                HasActiveWalletInCurrency = hasWalletInCurrency
            };

            return (true, "تم العثور على المستلم", response);
        }

        /// <summary>
        /// الاستعلام عن نقطة بيع عبر كود الـ POS
        /// </summary>
        public async Task<(bool Success, string Message, PosLookupResponseDto? Data)> LookupPosByCodeAsync(string posCode)
        {
            var cleanCode = posCode.Trim().ToUpper();

            var pos = await _context.PosPoints
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.PosCode == cleanCode && p.IsActive);

            if (pos == null)
            {
                return (false, "كود نقطة البيع غير صحيح أو غير مفعل", null);
            }

            var merchantName = pos.Owner?.HideFullName == true ? pos.Owner.GetMaskedName() : (pos.Owner?.FullName ?? string.Empty);

            var response = new PosLookupResponseDto
            {
                PosId = pos.Id,
                Name = pos.Name,
                PosCode = pos.PosCode,
                Category = pos.Category,
                Address = pos.Address,
                MerchantDisplayName = merchantName,
                IsActive = pos.IsActive
            };

            return (true, "تم العثور على نقطة البيع", response);
        }

        /// <summary>
        /// تنفيذ التحويل المالي لمشترك برقم الهاتف مع التحقق من الرصيد والتوثيق والخصم والإيداع اللحظي
        /// </summary>
        public async Task<(bool Success, string Message, TransactionResponseDto? Data)> TransferByPhoneAsync(Guid senderId, TransferByPhoneDto dto)
        {
            var sender = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Id == senderId);

            if (sender == null) return (false, "العميل المرسل غير موجود", null);

            // 1. التحقق من توثيق حساب المرسل
            if (sender.KycStatus != "Approved")
            {
                return (false, "لا يمكن تنفيذ التحويلات لأن حسابك لم يتم توثيقه وقبوله بعد من قبل الإدارة", null);
            }

            if (sender.IsBlocked)
            {
                return (false, "حسابك محظور من تنفيذ العمليات المالية", null);
            }

            var currencyCode = dto.CurrencyCode.Trim().ToUpper();

            // 2. التحقق من محفظة المرسل ورصيده
            var senderWallet = sender.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (senderWallet == null)
            {
                return (false, $"ليس لديك محفظة نشطة بعملة ({currencyCode})", null);
            }

            if (senderWallet.Balance < dto.Amount)
            {
                return (false, $"رصيدك غير كافٍ. رصيدك الحالي: {senderWallet.Balance:N2} {currencyCode} والمبلغ المطلوب: {dto.Amount:N2} {currencyCode}", null);
            }

            // 3. التحقق من حساب المستلم
            var receiver = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Phone == dto.ReceiverPhone.Trim());

            if (receiver == null)
            {
                return (false, "المستلم غير مسجل في النظام", null);
            }

            if (receiver.Id == sender.Id)
            {
                return (false, "لا يمكنك التحويل لنفسك", null);
            }

            if (receiver.KycStatus != "Approved")
            {
                return (false, "حساب المستلم غير موثق بعد ولا يمكنه استلام مبالغ", null);
            }

            // 4. جلب أو فتح محفظة للمستلم بنفس العملة إن لم تكن لديه
            var receiverWallet = receiver.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (receiverWallet == null)
            {
                receiverWallet = new Wallet
                {
                    ClientId = receiver.Id,
                    AccountNumber = Wallet.GenerateAccountNumber(),
                    CurrencyCode = currencyCode,
                    Balance = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(receiverWallet);
            }

            // 5. تنفيذ الخصم والإيداع
            senderWallet.Balance -= dto.Amount;
            senderWallet.UpdatedAt = DateTime.UtcNow;

            receiverWallet.Balance += dto.Amount;
            receiverWallet.UpdatedAt = DateTime.UtcNow;

            // 6. تجهيز أسماء العرض بناءً على خيارات الخصوصية للمرسل والمستلم
            var senderDisplay = sender.HideFullName ? sender.GetMaskedName() : sender.FullName;
            var receiverDisplay = receiver.HideFullName ? receiver.GetMaskedName() : receiver.FullName;

            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode);

            // 7. تسجيل الحركة المالية في قاعدة البيانات
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "TransferByPhone",
                SenderClientId = sender.Id,
                ReceiverClientId = receiver.Id,
                CurrencyCode = currencyCode,
                Amount = dto.Amount,
                Fee = 0.00m,
                TotalAmount = dto.Amount,
                Status = "Completed",
                SenderDisplayName = senderDisplay,
                SenderDisplayPhone = sender.Phone,
                ReceiverDisplayName = receiverDisplay,
                Note = dto.Note ?? $"تحويل مالي إلى {receiverDisplay}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(trx);
            await _context.SaveChangesAsync();

            var response = new TransactionResponseDto
            {
                Id = trx.Id,
                TransactionNumber = trx.TransactionNumber,
                Type = trx.Type,
                TypeNameAr = "تحويل لمشترك",
                CurrencyCode = trx.CurrencyCode,
                CurrencySymbol = currency?.Symbol ?? trx.CurrencyCode,
                Amount = trx.Amount,
                Fee = trx.Fee,
                TotalAmount = trx.TotalAmount,
                Status = trx.Status,
                StatusNameAr = "مكتملة",
                SenderDisplayName = senderDisplay,
                SenderDisplayPhone = sender.Phone,
                ReceiverDisplayName = receiverDisplay,
                Note = trx.Note,
                IsIncoming = false,
                CreatedAt = trx.CreatedAt
            };

            return (true, $"تم تحويل {dto.Amount:N2} {currency?.Symbol} بنجاح إلى {receiverDisplay}", response);
        }

        /// <summary>
        /// تنفيذ الشراء والدفع لنقطة بيع (POS) مع تطبيق ميزة إخفاء رقم الهاتف واستخدام الرقم البديل
        /// </summary>
        public async Task<(bool Success, string Message, TransactionResponseDto? Data)> PayToPosAsync(Guid senderId, PosPaymentDto dto)
        {
            var sender = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Id == senderId);

            if (sender == null) return (false, "العميل غير موجود", null);

            // 1. التحقق من توثيق حساب العميل
            if (sender.KycStatus != "Approved")
            {
                return (false, "لا يمكن الدفع لنقاط البيع لأن حسابك لم يتم توثيقه وقبوله بعد من قبل الإدارة", null);
            }

            if (sender.IsBlocked)
            {
                return (false, "حسابك محظور من تنفيذ العمليات", null);
            }

            var currencyCode = dto.CurrencyCode.Trim().ToUpper();

            // 2. التحقق من محفظة العميل ورصيده
            var senderWallet = sender.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (senderWallet == null)
            {
                return (false, $"ليس لديك محفظة نشطة بعملة ({currencyCode})", null);
            }

            if (senderWallet.Balance < dto.Amount)
            {
                return (false, $"رصيدك غير كافٍ. رصيدك الحالي: {senderWallet.Balance:N2} والمطلوب: {dto.Amount:N2}", null);
            }

            // 3. التحقق من نقطة البيع وصاحبها التاجر
            var pos = await _context.PosPoints
                .Include(p => p.Owner)
                .ThenInclude(o => o!.Wallets)
                .FirstOrDefaultAsync(p => p.PosCode == dto.PosCode.Trim().ToUpper() && p.IsActive);

            if (pos == null || pos.Owner == null)
            {
                return (false, "نقطة البيع غير موجودة أو غير نشطة حالياً", null);
            }

            var merchant = pos.Owner;

            // 4. جلب أو فتح محفظة للتاجر لاستقبال المبلغ
            var merchantWallet = merchant.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (merchantWallet == null)
            {
                merchantWallet = new Wallet
                {
                    ClientId = merchant.Id,
                    AccountNumber = Wallet.GenerateAccountNumber(),
                    CurrencyCode = currencyCode,
                    Balance = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(merchantWallet);
            }

            // 5. خصم المبلغ من العميل وإيداعه للتاجر
            senderWallet.Balance -= dto.Amount;
            senderWallet.UpdatedAt = DateTime.UtcNow;

            merchantWallet.Balance += dto.Amount;
            merchantWallet.UpdatedAt = DateTime.UtcNow;

            // 6. تطبيق ميزات الخصوصية:
            // - إخفاء الاسم إن كان مفعل
            var senderDisplayName = sender.HideFullName ? sender.GetMaskedName() : sender.FullName;
            
            // - إخفاء رقم الهاتف عند الدفع لنقطة بيع واستخدام الرقم البديل المولد
            string senderDisplayPhone;
            if (sender.HidePhoneOnPos)
            {
                if (string.IsNullOrWhiteSpace(sender.PosAliasPhone))
                {
                    sender.PosAliasPhone = Client.GenerateAliasPhone();
                }
                senderDisplayPhone = sender.PosAliasPhone;
            }
            else
            {
                senderDisplayPhone = sender.Phone;
            }

            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode);

            // 7. تسجيل حركة الدفع لنقطة البيع
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "PosPayment",
                SenderClientId = sender.Id,
                ReceiverClientId = merchant.Id,
                PosPointId = pos.Id,
                CurrencyCode = currencyCode,
                Amount = dto.Amount,
                Fee = 0.00m,
                TotalAmount = dto.Amount,
                Status = "Completed",
                SenderDisplayName = senderDisplayName,
                SenderDisplayPhone = senderDisplayPhone, // الرقم البديل أو الحقيقي وفق الخصوصية
                ReceiverDisplayName = $"{pos.Name} ({merchant.FullName})",
                Note = dto.Note ?? $"دفع مشتريات لنقطة بيع {pos.Name}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(trx);
            await _context.SaveChangesAsync();

            var response = new TransactionResponseDto
            {
                Id = trx.Id,
                TransactionNumber = trx.TransactionNumber,
                Type = trx.Type,
                TypeNameAr = "دفع لنقطة بيع",
                CurrencyCode = trx.CurrencyCode,
                CurrencySymbol = currency?.Symbol ?? trx.CurrencyCode,
                Amount = trx.Amount,
                Fee = trx.Fee,
                TotalAmount = trx.TotalAmount,
                Status = trx.Status,
                StatusNameAr = "مكتملة",
                SenderDisplayName = senderDisplayName,
                SenderDisplayPhone = senderDisplayPhone,
                ReceiverDisplayName = pos.Name,
                PosName = pos.Name,
                Note = trx.Note,
                IsIncoming = false,
                CreatedAt = trx.CreatedAt
            };

            return (true, $"تم دفع مبلغ {dto.Amount:N2} {currency?.Symbol} بنجاح لنقطة بيع ({pos.Name})", response);
        }
    }
}
