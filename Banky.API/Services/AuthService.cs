using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة المصادقة والتحقق من الهوية وإدارة الأمان
    /// تشمل عمليات التسجيل، الدخول، رفع وثائق KYC، واستعادة وتغيير كلمات المرور
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AuthService(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        /// <summary>
        /// تسجيل حساب عميل جديد مع إنشاء محفظة أساسية بالريال اليمني وتعيين حالة التوثيق كـ NotSubmitted
        /// </summary>
        public async Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto)
        {
            // 1. التحقق من عدم تكرار البريد الإلكتروني
            if (await _context.Clients.AnyAsync(c => c.Email.ToLower() == dto.Email.Trim().ToLower()))
            {
                return (false, "البريد الإلكتروني مسجل مسبقاً لمستخدم آخر", null);
            }

            // 2. التحقق من عدم تكرار رقم الهاتف
            if (await _context.Clients.AnyAsync(c => c.Phone == dto.Phone.Trim()))
            {
                return (false, "رقم الهاتف مسجل مسبقاً لمستخدم آخر", null);
            }

            // 3. إنشاء كائن العميل الجديد
            var client = new Client
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Client",
                KycStatus = "NotSubmitted",
                HideFullName = false,
                HidePhoneOnPos = false,
                PosAliasPhone = Client.GenerateAliasPhone(),
                IsBiometricEnabled = false,
                MustChangePassword = false,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Clients.AddAsync(client);

            // 4. إنشاء محفظة افتراضية أولى بالريال اليمني (YER)
            var defaultWallet = new Wallet
            {
                ClientId = client.Id,
                AccountNumber = Wallet.GenerateAccountNumber(),
                CurrencyCode = "YER",
                Balance = 0.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Wallets.AddAsync(defaultWallet);
            await _context.SaveChangesAsync();

            // 5. توليد التوكن وإرجاع بيانات الدخول المباشر
            var token = GenerateJwtToken(client);
            var profile = MapToProfileDto(client);

            var response = new AuthResponseDto
            {
                Token = token,
                User = profile,
                MustChangePassword = false,
                KycStatus = client.KycStatus,
                Message = "تم إنشاء الحساب بنجاح. يرجى توثيق حسابك برفع صورة الهوية لتتمكن من استخدام كافة ميزات التطبيق."
            };

            return (true, "تم إنشاء الحساب بنجاح", response);
        }

        /// <summary>
        /// تسجيل الدخول للعميل أو الإدارة مع فحص حالة الحظر وتوليد التوكن
        /// </summary>
        public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto)
        {
            var identifier = dto.Identifier.Trim().ToLower();

            // 1. البحث عن العميل بالبريد أو رقم الهاتف
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email.ToLower() == identifier || c.Phone == identifier);

            if (client == null)
            {
                return (false, "بيانات الدخول غير صحيحة (البريد أو الهاتف غير مسجل)", null);
            }

            // 2. التحقق من صحة كلمة المرور
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, client.PasswordHash))
            {
                return (false, "كلمة المرور غير صحيحة", null);
            }

            // 3. التحقق مما إذا كان الحساب محظوراً
            if (client.IsBlocked)
            {
                return (false, $"تم حظر هذا الحساب من قبل الإدارة: {client.BlockedMessage ?? "يرجى التواصل مع الدعم الفني"}", null);
            }

            // 4. تسجيل وتحديث بيانات الجهاز إن وجدت
            if (!string.IsNullOrWhiteSpace(dto.DeviceId))
            {
                var existingDevice = await _context.Devices
                    .FirstOrDefaultAsync(d => d.ClientId == client.Id && d.DeviceId == dto.DeviceId);

                if (existingDevice == null)
                {
                    var newDevice = new Device
                    {
                        ClientId = client.Id,
                        DeviceId = dto.DeviceId,
                        FcmToken = dto.FcmToken,
                        DeviceName = "Mobile Device",
                        MainDevice = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Devices.AddAsync(newDevice);
                }
                else
                {
                    existingDevice.FcmToken = dto.FcmToken ?? existingDevice.FcmToken;
                    existingDevice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            // 5. توليد التوكن وتجهيز الاستجابة
            var token = GenerateJwtToken(client);
            var profile = MapToProfileDto(client);

            string message = client.KycStatus switch
            {
                "Approved" => "تم تسجيل الدخول بنجاح",
                "PendingApproval" => "حسابك قيد مراجعة وثائق التوثيق من قبل الإدارة",
                "Rejected" => $"تم رفض توثيق حسابك: {client.KycRejectionReason}. يرجى إعادة رفع صور الهوية",
                _ => "حسابك غير موثق بعد، يرجى رفع صورة الهوية لتفعيل الخدمات المصرفية"
            };

            var response = new AuthResponseDto
            {
                Token = token,
                User = profile,
                MustChangePassword = client.MustChangePassword,
                KycStatus = client.KycStatus,
                Message = message
            };

            return (true, "تم تسجيل الدخول بنجاح", response);
        }

        /// <summary>
        /// استرجاع بيانات الملف الشخصي للعميل
        /// </summary>
        public async Task<ClientProfileDto?> GetProfileAsync(Guid clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            return client == null ? null : MapToProfileDto(client);
        }

        /// <summary>
        /// رفع وتحديث صور بطاقة الهوية الشخصية (الوجه الأمامي والخلفي) لتوثيق الحساب
        /// </summary>
        public async Task<(bool Success, string Message)> SubmitKycAsync(Guid clientId, KycSubmissionDto dto)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "المستخدم غير موجود");

            // حفظ الصور في المجلد المخصص للوثائق wwwroot/uploads/kyc
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "kyc");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            string frontPath = await SaveImageFileAsync(dto.IdFrontImageBase64OrPath, uploadsDir, $"front_{clientId}_{DateTime.UtcNow.Ticks}");
            string backPath = await SaveImageFileAsync(dto.IdBackImageBase64OrPath, uploadsDir, $"back_{clientId}_{DateTime.UtcNow.Ticks}");

            client.KycIdFront = frontPath;
            client.KycIdBack = backPath;
            client.KycStatus = "PendingApproval"; // تحويل الحالة إلى قيد مراجعة الإدارة
            client.KycSubmittedAt = DateTime.UtcNow;
            client.KycRejectionReason = null; // تصفير أي سبب رفض سابق
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "تم رفع وثائق التوثيق بنجاح وهي الآن قيد مراجعة الإدارة");
        }

        /// <summary>
        /// طلب استعادة كلمة المرور وتوليد كود تحقق مؤقت
        /// </summary>
        public async Task<(bool Success, string Message, string? DemoToken)> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var identifier = dto.Identifier.Trim().ToLower();
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email.ToLower() == identifier || c.Phone == identifier);

            if (client == null)
            {
                return (false, "البريد الإلكتروني أو رقم الهاتف غير مسجل بالنظام", null);
            }

            // توليد رمز تحقق عشوائي مكون من 6 أرقام صالح لمدة 15 دقيقة
            var random = new Random();
            var token = random.Next(100000, 999999).ToString();

            client.ResetPasswordToken = token;
            client.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            // في التطبيقات الواقعية يتم إرسال الرمز عبر SMS أو Email، ولغرض المشروع نعيد الرمز للاختبار السلس
            return (true, $"تم إرسال رمز التحقق بنجاح إلى هاتفك/بريدك: {token}", token);
        }

        /// <summary>
        /// إعادة تعيين كلمة المرور بواسطة رمز الاستعادة
        /// </summary>
        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var identifier = dto.Identifier.Trim().ToLower();
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email.ToLower() == identifier || c.Phone == identifier);

            if (client == null) return (false, "المستخدم غير موجود");

            if (string.IsNullOrWhiteSpace(client.ResetPasswordToken) ||
                client.ResetPasswordToken != dto.ResetToken.Trim())
            {
                return (false, "رمز التحقق غير صحيح");
            }

            if (client.ResetPasswordExpiry == null || client.ResetPasswordExpiry < DateTime.UtcNow)
            {
                return (false, "انتهت صلاحية رمز التحقق، يرجى طلب رمز جديد");
            }

            client.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            client.ResetPasswordToken = null;
            client.ResetPasswordExpiry = null;
            client.MustChangePassword = false;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "تم إعادة تعيين كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.");
        }

        /// <summary>
        /// تغيير كلمة المرور للمستخدم المسجل دخوله
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid clientId, ChangePasswordDto dto)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "المستخدم غير موجود");

            // إذا لم يكن التغيير إجبارياً وكان هناك كلمة مرور حالية مقدمة، نتحقق منها
            if (!client.MustChangePassword && !string.IsNullOrWhiteSpace(dto.CurrentPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, client.PasswordHash))
                {
                    return (false, "كلمة المرور الحالية غير صحيحة");
                }
            }

            client.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            client.MustChangePassword = false; // إلغاء راية التغيير الإجباري بعد إتمام التغيير بنجاح
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "تم تغيير كلمة المرور بنجاح");
        }

        /// <summary>
        /// تحديث إعدادات الخصوصية (إخفاء الاسم، إخفاء الهاتف على POS، البصمة)
        /// </summary>
        public async Task<(bool Success, string Message, ClientProfileDto? Data)> UpdatePrivacyAsync(Guid clientId, UpdatePrivacyDto dto)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "المستخدم غير موجود", null);

            client.HideFullName = dto.HideFullName;
            client.HidePhoneOnPos = dto.HidePhoneOnPos;
            client.IsBiometricEnabled = dto.IsBiometricEnabled;

            // إذا تم تفعيل إخفاء الهاتف ولم يكن هناك رقم بديل، نقوم بتوليده
            if (client.HidePhoneOnPos && string.IsNullOrWhiteSpace(client.PosAliasPhone))
            {
                client.PosAliasPhone = Client.GenerateAliasPhone();
            }

            client.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "تم تحديث إعدادات الخصوصية والأمان بنجاح", MapToProfileDto(client));
        }

        /// <summary>
        /// توليد رقم بديل جديد لنقاط البيع
        /// </summary>
        public async Task<(bool Success, string NewAlias)> RegeneratePosAliasAsync(Guid clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, string.Empty);

            var newAlias = Client.GenerateAliasPhone();
            client.PosAliasPhone = newAlias;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, newAlias);
        }

        /// <summary>
        /// توليد رمز الـ JWT المشفر للمستخدم
        /// </summary>
        public string GenerateJwtToken(Client client)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "banky_super_secret_jwt_key_2026_very_strong_and_secure_key_123456789";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
                new Claim(ClaimTypes.Name, client.FullName),
                new Claim(ClaimTypes.Email, client.Email),
                new Claim(ClaimTypes.Role, client.Role),
                new Claim("phone", client.Phone),
                new Claim("kyc_status", client.KycStatus)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "Banky",
                audience: _configuration["Jwt:Audience"] ?? "BankyApp",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #region دوال مساعدة خاصة

        private static ClientProfileDto MapToProfileDto(Client client)
        {
            return new ClientProfileDto
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
        }

        private static async Task<string> SaveImageFileAsync(string base64OrPath, string uploadsDir, string fileNamePrefix)
        {
            if (string.IsNullOrWhiteSpace(base64OrPath)) return string.Empty;

            // إذا كان المسار عبارة عن رابط أو مسار موجود مسبقاً
            if (base64OrPath.StartsWith("/uploads/") || base64OrPath.StartsWith("http"))
            {
                return base64OrPath;
            }

            try
            {
                // تنظيف نص Base64
                var base64Data = base64OrPath;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                }

                var bytes = Convert.FromBase64String(base64Data);
                var fileName = $"{fileNamePrefix}.jpg";
                var fullPath = Path.Combine(uploadsDir, fileName);

                await File.WriteAllBytesAsync(fullPath, bytes);
                return $"/uploads/kyc/{fileName}";
            }
            catch
            {
                // في حال كان النص نصاً عادياً أو رابط
                return base64OrPath;
            }
        }

        #endregion
    }
}
