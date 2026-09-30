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

            // جلب إعداد الرسوم لعملية التحويل بين المشتركين
            var feeRule = await GetFeeRuleAsync("TransferByPhone", currencyCode);
            decimal estimatedFee = feeRule?.CalculateFee(1000m) ?? 0.00m; // تقدير افتراضي
            string feeDesc = feeRule != null && feeRule.IsActive
                ? (feeRule.FeeType == "Percentage" ? $"عمولة {feeRule.Percentage:0.##}%" : $"رسوم ثابتة {feeRule.FixedAmount:N2}")
                : "معفاة من الرسوم";

            var response = new RecipientLookupResponseDto
            {
                ClientId = receiver.Id,
                DisplayName = displayName,
                Phone = receiver.Phone,
                IsNameMasked = receiver.HideFullName,
                HasActiveWalletInCurrency = hasWalletInCurrency,
                EstimatedFee = estimatedFee,
                FeeDescription = feeDesc
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

            var feeRule = await GetFeeRuleAsync("PosPayment", null);
            string feeDesc = feeRule != null && feeRule.IsActive
                ? (feeRule.FeeType == "Percentage" ? $"عمولة {feeRule.Percentage:0.##}%" : $"رسوم ثابتة {feeRule.FixedAmount:N2}")
                : "خدمة دفع معفاة من الرسوم";

            var response = new PosLookupResponseDto
            {
                PosId = pos.Id,
                Name = pos.Name,
                PosCode = pos.PosCode,
                Category = pos.Category,
                Address = pos.Address,
                MerchantDisplayName = merchantName,
                IsActive = pos.IsActive,
                EstimatedFee = feeRule?.CalculateFee(1000m) ?? 0.00m,
                FeeDescription = feeDesc
            };

            return (true, "تم العثور على نقطة البيع", response);
        }

        /// <summary>
        /// تنفيذ التحويل المالي لمشترك برقم الهاتف مع التحقق من الرصيد والرسوم والتوثيق والخصم والإيداع اللحظي
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

            // 2. احتساب الرسوم المحددة من قبل الإدارة
            var feeRule = await GetFeeRuleAsync("TransferByPhone", currencyCode);
            decimal fee = feeRule?.CalculateFee(dto.Amount) ?? 0.00m;
            decimal totalDebit = dto.Amount + fee;

            // 3. التحقق من محفظة المرسل ورصيده (المبلغ + الرسوم)
            var senderWallet = sender.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (senderWallet == null)
            {
                return (false, $"ليس لديك محفظة نشطة بعملة ({currencyCode})", null);
            }

            if (senderWallet.Balance < totalDebit)
            {
                return (false, $"رصيدك غير كافٍ. المطلوب شامل الرسوم ({fee:N2}): {totalDebit:N2} {currencyCode} ورصيدك الحالي: {senderWallet.Balance:N2} {currencyCode}", null);
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

            // 5. تنفيذ الخصم والإيداع (خصم المبلغ + الرسوم من المرسل، وإيداع المبلغ الصافي للمستلم)
            senderWallet.Balance -= totalDebit;
            senderWallet.UpdatedAt = DateTime.UtcNow;

            receiverWallet.Balance += dto.Amount;
            receiverWallet.UpdatedAt = DateTime.UtcNow;

            // 6. تجهيز أسماء العرض بناءً على خيارات الخصوصية للمرسل والمستلم
            var senderDisplay = sender.HideFullName ? sender.GetMaskedName() : sender.FullName;
            var receiverDisplay = receiver.HideFullName ? receiver.GetMaskedName() : receiver.FullName;

            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode);

            // 7. تسجيل الحركة المالية في قاعدة البيانات مع الرسوم والمبلغ الإجمالي
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "TransferByPhone",
                SenderClientId = sender.Id,
                ReceiverClientId = receiver.Id,
                CurrencyCode = currencyCode,
                Amount = dto.Amount,
                Fee = fee,
                TotalAmount = totalDebit,
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

            return (true, $"تم تحويل {dto.Amount:N2} {currency?.Symbol} بنجاح إلى {receiverDisplay} (الرسوم: {fee:N2} {currency?.Symbol})", response);
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

            // 2. احتساب رسوم المشتريات والدفع لنقاط البيع المحددة من الإدارة
            var feeRule = await GetFeeRuleAsync("PosPayment", currencyCode);
            decimal fee = feeRule?.CalculateFee(dto.Amount) ?? 0.00m;
            decimal totalDebit = dto.Amount + fee;

            // 3. التحقق من محفظة العميل ورصيده (المبلغ + الرسوم)
            var senderWallet = sender.Wallets.FirstOrDefault(w => w.CurrencyCode == currencyCode && w.IsActive);
            if (senderWallet == null)
            {
                return (false, $"ليس لديك محفظة نشطة بعملة ({currencyCode})", null);
            }

            if (senderWallet.Balance < totalDebit)
            {
                return (false, $"رصيدك غير كافٍ. المطلوب شامل الرسوم ({fee:N2}): {totalDebit:N2} {currencyCode} والمتاح: {senderWallet.Balance:N2} {currencyCode}", null);
            }

            // 4. التحقق من نقطة البيع وصاحبها التاجر
            var pos = await _context.PosPoints
                .Include(p => p.Owner)
                .ThenInclude(o => o!.Wallets)
                .FirstOrDefaultAsync(p => p.PosCode == dto.PosCode.Trim().ToUpper() && p.IsActive);

            if (pos == null || pos.Owner == null)
            {
                return (false, "نقطة البيع غير موجودة أو غير نشطة حالياً", null);
            }

            var merchant = pos.Owner;

            // 5. جلب أو فتح محفظة للتاجر لاستقبال المبلغ
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

            // 6. خصم المبلغ الإجمالي من العميل وإيداع المبلغ للتاجر
            senderWallet.Balance -= totalDebit;
            senderWallet.UpdatedAt = DateTime.UtcNow;

            merchantWallet.Balance += dto.Amount;
            merchantWallet.UpdatedAt = DateTime.UtcNow;

            // 7. تطبيق ميزات الخصوصية:
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

            // 8. تسجيل حركة الدفع لنقطة البيع مع الرسوم والمبلغ الإجمالي
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "PosPayment",
                SenderClientId = sender.Id,
                ReceiverClientId = merchant.Id,
                PosPointId = pos.Id,
                CurrencyCode = currencyCode,
                Amount = dto.Amount,
                Fee = fee,
                TotalAmount = totalDebit,
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

            return (true, $"تم دفع مبلغ {dto.Amount:N2} {currency?.Symbol} بنجاح لنقطة بيع ({pos.Name}) (الرسوم: {fee:N2} {currency?.Symbol})", response);
        }

        /// <summary>
        /// حساب قيمة المصارفة وسعر الصرف بين عملتين مع احتساب العمولة المحددة
        /// </summary>
        public async Task<(bool Success, string Message, ExchangeCalculationResultDto? Data)> CalculateExchangeAsync(string fromCurrency, string toCurrency, decimal amount)
        {
            var fromCode = fromCurrency.Trim().ToUpper();
            var toCode = toCurrency.Trim().ToUpper();

            if (fromCode == toCode)
            {
                return (false, "العملة المصدر هي نفس العملة الهدف", null);
            }

            var currFrom = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == fromCode && c.IsActive);
            var currTo = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == toCode && c.IsActive);

            if (currFrom == null || currTo == null)
            {
                return (false, "إحدى العملتين غير مدعومة أو غير نشطة", null);
            }

            if (currFrom.ExchangeRate <= 0 || currTo.ExchangeRate <= 0)
            {
                return (false, "سعر الصرف غير مضبوط في النظام", null);
            }

            // حساب المعامل: (amount / currFrom.ExchangeRate) * currTo.ExchangeRate
            decimal effectiveRate = currTo.ExchangeRate / currFrom.ExchangeRate;
            decimal targetAmount = Math.Round(amount * effectiveRate, 2);

            // احتساب عمولة المصارفة
            var feeRule = await GetFeeRuleAsync("SelfExchange", fromCode);
            decimal fee = feeRule?.CalculateFee(amount) ?? 0.00m;
            decimal totalRequired = amount + fee;

            string feeDesc = feeRule != null && feeRule.IsActive
                ? (feeRule.FeeType == "Percentage" ? $"عمولة صرف {feeRule.Percentage:0.##}%" : $"رسوم ثابتة {feeRule.FixedAmount:N2}")
                : "معفاة من عمولة الصرف";

            var result = new ExchangeCalculationResultDto
            {
                FromCurrencyCode = fromCode,
                ToCurrencyCode = toCode,
                SourceAmount = amount,
                TargetAmount = targetAmount,
                ExchangeRate = effectiveRate,
                Fee = fee,
                TotalSourceAmountWithFee = totalRequired,
                FeeDescription = feeDesc
            };

            return (true, "تم حساب سعر الصرف بنجاح", result);
        }

        /// <summary>
        /// تنفيذ التحويل والمصارفة بين محافظ العميل الخاصة مع اقتطاع العمولة
        /// </summary>
        public async Task<(bool Success, string Message, TransactionResponseDto? Data)> ExchangeSelfAsync(Guid clientId, SelfExchangeDto dto)
        {
            var client = await _context.Clients
                .Include(c => c.Wallets)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null) return (false, "العميل غير موجود", null);

            if (client.KycStatus != "Approved")
            {
                return (false, "لا يمكن التحويل والمصارفة لأن حسابك لم يتم توثيقه وقبوله بعد من قبل الإدارة", null);
            }

            if (client.IsBlocked)
            {
                return (false, "حسابك محظور من تنفيذ العمليات المالية", null);
            }

            var fromCode = dto.FromCurrencyCode.Trim().ToUpper();
            var toCode = dto.ToCurrencyCode.Trim().ToUpper();

            if (fromCode == toCode)
            {
                return (false, "لا يمكن التحويل لنفس العملة، يرجى اختيار عملتين مختلفتين", null);
            }

            // التحقق من العملات وسعر الصرف
            var currFrom = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == fromCode && c.IsActive);
            var currTo = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == toCode && c.IsActive);

            if (currFrom == null || currTo == null)
            {
                return (false, "إحدى العملتين المختارتين غير مفعلة في النظام", null);
            }

            // احتساب عمولة المصارفة
            var feeRule = await GetFeeRuleAsync("SelfExchange", fromCode);
            decimal fee = feeRule?.CalculateFee(dto.Amount) ?? 0.00m;
            decimal totalDebit = dto.Amount + fee;

            // التحقق من محفظة المصدر
            var fromWallet = client.Wallets.FirstOrDefault(w => w.CurrencyCode == fromCode && w.IsActive);
            if (fromWallet == null)
            {
                return (false, $"ليس لديك محفظة نشطة بعملة ({fromCode})", null);
            }

            if (fromWallet.Balance < totalDebit)
            {
                return (false, $"رصيدك في محفظة ({fromCode}) غير كافٍ. المطلوب شامل العمولة ({fee:N2}): {totalDebit:N2} والمتاح: {fromWallet.Balance:N2}", null);
            }

            // حساب المبلغ الناتج
            decimal effectiveRate = currTo.ExchangeRate / currFrom.ExchangeRate;
            decimal targetAmount = Math.Round(dto.Amount * effectiveRate, 2);

            // جلب أو فتح محفظة الهدف للعميل
            var toWallet = client.Wallets.FirstOrDefault(w => w.CurrencyCode == toCode && w.IsActive);
            if (toWallet == null)
            {
                toWallet = new Wallet
                {
                    ClientId = client.Id,
                    AccountNumber = Wallet.GenerateAccountNumber(),
                    CurrencyCode = toCode,
                    Balance = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(toWallet);
            }

            // خصم من محفظة المصدر (المبلغ + العمولة) وإيداع في محفظة الهدف
            fromWallet.Balance -= totalDebit;
            fromWallet.UpdatedAt = DateTime.UtcNow;

            toWallet.Balance += targetAmount;
            toWallet.UpdatedAt = DateTime.UtcNow;

            // تسجيل الحركة المالية في قاعدة البيانات
            var trx = new Transaction
            {
                TransactionNumber = Transaction.GenerateTransactionNumber(),
                Type = "SelfExchange",
                SenderClientId = client.Id,
                ReceiverClientId = client.Id,
                CurrencyCode = fromCode,
                Amount = dto.Amount,
                Fee = fee,
                TotalAmount = totalDebit,
                Status = "Completed",
                SenderDisplayName = client.FullName,
                SenderDisplayPhone = client.Phone,
                ReceiverDisplayName = $"تحويل إلى محفظة {toCode} ({client.FullName})",
                Note = dto.Note ?? $"مصارفة وتحويل من {fromCode} إلى {toCode} (المبلغ المستلم: {targetAmount:N2} {currTo.Symbol})",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(trx);
            await _context.SaveChangesAsync();

            var response = new TransactionResponseDto
            {
                Id = trx.Id,
                TransactionNumber = trx.TransactionNumber,
                Type = trx.Type,
                TypeNameAr = "تحويل بين حساباتي",
                CurrencyCode = trx.CurrencyCode,
                CurrencySymbol = currFrom.Symbol,
                Amount = trx.Amount,
                Fee = trx.Fee,
                TotalAmount = trx.TotalAmount,
                Status = trx.Status,
                StatusNameAr = "مكتملة",
                SenderDisplayName = client.FullName,
                SenderDisplayPhone = client.Phone,
                ReceiverDisplayName = $"محفظة {toCode}",
                Note = trx.Note,
                IsIncoming = false,
                CreatedAt = trx.CreatedAt
            };

            return (true, $"تمت المصارفة والتحويل بنجاح! تم خصم {totalDebit:N2} {currFrom.Symbol} (شامل عمولة {fee:N2}) وإيداع {targetAmount:N2} {currTo.Symbol} في محفظتك.", response);
        }

        #region دالة مساعدة لجلب إعدادات الرسوم

        /// <summary>
        /// جلب قاعدة الرسوم الفعالة لنوع عملية وعملة محددة
        /// </summary>
        private async Task<FeeSetting?> GetFeeRuleAsync(string operationType, string? currencyCode)
        {
            if (!string.IsNullOrWhiteSpace(currencyCode))
            {
                var specific = await _context.FeeSettings
                    .FirstOrDefaultAsync(f => f.OperationType == operationType && f.CurrencyCode == currencyCode.ToUpper() && f.IsActive);
                if (specific != null) return specific;
            }

            return await _context.FeeSettings
                .FirstOrDefaultAsync(f => f.OperationType == operationType && (f.CurrencyCode == null || f.CurrencyCode == "") && f.IsActive);
        }

        #endregion
    }
}
