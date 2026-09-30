using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Data
{
    /// <summary>
    /// فئة تهيئة وتغذية قاعدة البيانات بالبيانات الأولية (Seed Data)
    /// تقوم بإنشاء العملات الأساسية، حساب المسؤول العام، والمستخدمين التجريبيين
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// دالة التهيئة الرئيسية التي تستدعى عند بدء تشغيل التطبيق
        /// </summary>
        public static async Task InitializeAsync(AppDbContext context)
        {
            // إنشاء قاعدة البيانات والجداول إن لم تكن موجودة
            await context.Database.EnsureCreatedAsync();

            // 1. إضافة العملات الافتراضية المعتمدة
            if (!await context.Currencies.AnyAsync())
            {
                var currencies = new List<Currency>
                {
                    new Currency
                    {
                        Code = "YER",
                        NameAr = "ريال يمني",
                        NameEn = "Yemeni Rial",
                        Symbol = "ر.ي",
                        IsActive = true,
                        ExchangeRate = 530.00m,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Currency
                    {
                        Code = "SAR",
                        NameAr = "ريال سعودي",
                        NameEn = "Saudi Riyal",
                        Symbol = "ر.س",
                        IsActive = true,
                        ExchangeRate = 3.75m,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Currency
                    {
                        Code = "USD",
                        NameAr = "دولار أمريكي",
                        NameEn = "US Dollar",
                        Symbol = "$",
                        IsActive = true,
                        ExchangeRate = 1.00m,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Currencies.AddRangeAsync(currencies);
                await context.SaveChangesAsync();
            }

            // 2. إنشاء حساب المسؤول العام (Admin) الافتراضي
            if (!await context.Clients.AnyAsync(c => c.Role == "Admin"))
            {
                var adminClient = new Client
                {
                    Id = Guid.NewGuid(),
                    FullName = "مدير النظام العام",
                    Email = "admin@banky.com",
                    Phone = "777000000",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                    Role = "Admin",
                    KycStatus = "Approved",
                    HideFullName = false,
                    HidePhoneOnPos = false,
                    IsBiometricEnabled = false,
                    MustChangePassword = false,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Clients.AddAsync(adminClient);
                await context.SaveChangesAsync();
            }

            // 3. إضافة عميلين تجريبيين ومحافظ ونقاط بيع للاختبار الفوري
            if (!await context.Clients.AnyAsync(c => c.Email == "ahmed@banky.com"))
            {
                // عميل 1: أحمد محمد علي حسن (موثق)
                var client1 = new Client
                {
                    Id = Guid.NewGuid(),
                    FullName = "أحمد محمد علي حسن",
                    Email = "ahmed@banky.com",
                    Phone = "771234567",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = "Client",
                    KycStatus = "Approved",
                    HideFullName = false,
                    HidePhoneOnPos = true,
                    PosAliasPhone = Client.GenerateAliasPhone(),
                    CreatedAt = DateTime.UtcNow,
                };

                // محفظة بالريال اليمني
                var walletYer1 = new Wallet
                {
                    ClientId = client1.Id,
                    AccountNumber = "7710001001",
                    CurrencyCode = "YER",
                    Balance = 250000.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // محفظة بالريال السعودي
                var walletSar1 = new Wallet
                {
                    ClientId = client1.Id,
                    AccountNumber = "7710001002",
                    CurrencyCode = "SAR",
                    Balance = 1500.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // محفظة بالدولار الأمريكي
                var walletUsd1 = new Wallet
                {
                    ClientId = client1.Id,
                    AccountNumber = "7710001003",
                    CurrencyCode = "USD",
                    Balance = 400.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // نقطة بيع خاصة بالعميل 1
                var pos1 = new PosPoint
                {
                    ClientId = client1.Id,
                    Name = "سوبرماركت الأمل الحديث",
                    PosCode = "POS-100200",
                    Address = "صنعاء - شارع حدة",
                    Category = "بقالة وسوبرماركت",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // عميل 2: سامي عبدالله محمد فاروق (موثق)
                var client2 = new Client
                {
                    Id = Guid.NewGuid(),
                    FullName = "سامي عبدالله محمد فاروق",
                    Email = "sami@banky.com",
                    Phone = "779876543",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = "Client",
                    KycStatus = "Approved",
                    HideFullName = true, // ميزة إخفاء الاسم مفعله
                    HidePhoneOnPos = false,
                    PosAliasPhone = Client.GenerateAliasPhone(),
                    CreatedAt = DateTime.UtcNow
                };

                var walletYer2 = new Wallet
                {
                    ClientId = client2.Id,
                    AccountNumber = "7720002001",
                    CurrencyCode = "YER",
                    Balance = 80000.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // عميل 3: مروان خالد (قيد مراجعة التوثيق Pending)
                var client3 = new Client
                {
                    Id = Guid.NewGuid(),
                    FullName = "مروان خالد صالح عثمان",
                    Email = "marwan@banky.com",
                    Phone = "775555444",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = "Client",
                    KycStatus = "PendingApproval",
                    KycSubmittedAt = DateTime.UtcNow.AddHours(-2),
                    HideFullName = false,
                    HidePhoneOnPos = false,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Clients.AddRangeAsync(client1, client2, client3);
                await context.Wallets.AddRangeAsync(walletYer1, walletSar1, walletUsd1, walletYer2);
                await context.PosPoints.AddAsync(pos1);
                await context.SaveChangesAsync();

                // إضافة حركة تجريبية سابقة
                var sampleTrx = new Transaction
                {
                    TransactionNumber = Transaction.GenerateTransactionNumber(),
                    Type = "TransferByPhone",
                    SenderClientId = client1.Id,
                    ReceiverClientId = client2.Id,
                    CurrencyCode = "YER",
                    Amount = 15000.00m,
                    Fee = 0.00m,
                    TotalAmount = 15000.00m,
                    Status = "Completed",
                    SenderDisplayName = client1.FullName,
                    SenderDisplayPhone = client1.Phone,
                    ReceiverDisplayName = client2.GetMaskedName(), // اسم مقنع بالحروف الأولى
                    Note = "سداد قيمة مشتريات",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                await context.Transactions.AddAsync(sampleTrx);
                await context.SaveChangesAsync();
            }

            // 4. إضافة إعدادات الرسوم والعمولات الافتراضية للعمليات المصرفية
            if (!await context.FeeSettings.AnyAsync())
            {
                var defaultFees = new List<FeeSetting>
                {
                    new FeeSetting
                    {
                        OperationType = "TransferByPhone",
                        CurrencyCode = null, // ينطبق على كل العملات كافتراضي
                        NameAr = "رسوم التحويل المالي بين المشتركين",
                        Description = "عمولة رمزية تقتطع من المرسل عند التحويل لمشترك آخر برقم الهاتف (0.5% بحد أقصى 500)",
                        FeeType = "Percentage",
                        Percentage = 0.5000m,
                        FixedAmount = 0.00m,
                        MinFee = 0.00m,
                        MaxFee = 500.00m,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "نظام التهيئة الافتراضي"
                    },
                    new FeeSetting
                    {
                        OperationType = "SelfExchange",
                        CurrencyCode = null,
                        NameAr = "عمولة الصرف والمصارفة بين الحسابات",
                        Description = "عمولة فارق الصرف والتحويل بين محافظ العميل الشخصية المختلفة (0.2%)",
                        FeeType = "Percentage",
                        Percentage = 0.2000m,
                        FixedAmount = 0.00m,
                        MinFee = 0.00m,
                        MaxFee = 0.00m,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "نظام التهيئة الافتراضي"
                    },
                    new FeeSetting
                    {
                        OperationType = "PosPayment",
                        CurrencyCode = null,
                        NameAr = "رسوم الدفع والشراء عبر نقاط البيع (POS)",
                        Description = "رسوم خدمة الشراء عبر نقاط البيع (معفاة افتراضياً للعميل 0% تشجيعاً للمدفوعات الرقمية)",
                        FeeType = "Percentage",
                        Percentage = 0.0000m,
                        FixedAmount = 0.00m,
                        MinFee = 0.00m,
                        MaxFee = 0.00m,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "نظام التهيئة الافتراضي"
                    },
                    new FeeSetting
                    {
                        OperationType = "Deposit",
                        CurrencyCode = null,
                        NameAr = "رسوم إيداع وتغذية الرصيد",
                        Description = "خدمة إيداع مجانية تماماً لتغذية المحافظ المالية",
                        FeeType = "Percentage",
                        Percentage = 0.0000m,
                        FixedAmount = 0.00m,
                        MinFee = 0.00m,
                        MaxFee = 0.00m,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "نظام التهيئة الافتراضي"
                    },
                    new FeeSetting
                    {
                        OperationType = "Withdrawal",
                        CurrencyCode = null,
                        NameAr = "رسوم السحب النقدي",
                        Description = "عمولة السحب النقدي عبر الوكلاء ونقاط الصرف المعتمدة (1% بحد أدنى 50)",
                        FeeType = "Percentage",
                        Percentage = 1.0000m,
                        FixedAmount = 0.00m,
                        MinFee = 50.00m,
                        MaxFee = 2000.00m,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "نظام التهيئة الافتراضي"
                    }
                };

                await context.FeeSettings.AddRangeAsync(defaultFees);
                await context.SaveChangesAsync();
            }
        }
    }
}
