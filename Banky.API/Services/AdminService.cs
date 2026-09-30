using Banky.API.Data;
using Banky.API.DTOs;
using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Services
{
    /// <summary>
    /// خدمة العمليات الإدارية ولوحة التحكم
    /// تشمل مراجعة وتوثيق الهويات، إدارة المستخدمين والمحافظ، الأجهزة، الموظفين، وسجل التدقيق وإدارة العملات
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        #region إحصائيات الداشبورد العامة

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
                .Include(t => t.SenderClient)
                .Include(t => t.ReceiverClient)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    TransactionNumber = t.TransactionNumber,
                    Type = t.Type,
                    TypeNameAr = t.Type == "TransferByPhone" ? "تحويل لمشترك" : t.Type == "PosPayment" ? "دفع لنقطة بيع" : t.Type == "SelfExchange" ? "تحويل بين حساباتي" : "إيداع",
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
                    SenderClientId = t.SenderClientId,
                    SenderRealFullName = t.SenderClient != null ? t.SenderClient.FullName : (t.SenderDisplayName ?? ""),
                    SenderRealPhone = t.SenderClient != null ? t.SenderClient.Phone : (t.SenderDisplayPhone ?? ""),
                    ReceiverClientId = t.ReceiverClientId,
                    ReceiverRealFullName = t.ReceiverClient != null ? t.ReceiverClient.FullName : (t.ReceiverDisplayName ?? ""),
                    ReceiverRealPhone = t.ReceiverClient != null ? t.ReceiverClient.Phone : "",
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

        #endregion

        #region مراجعة وتوثيق الهويات (KYC)

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

        public async Task<(bool Success, string Message)> ReviewKycAsync(KycReviewDto dto, Guid? adminId = null, string adminName = "Admin")
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

                await LogActivityAsync(adminId, adminName, "Admin", "ApproveKyc", "Client", client.Id.ToString(), $"تمت الموافقة على توثيق هوية العميل: {client.FullName}");
                return (true, $"تم قبول وتوثيق حساب العميل ({client.FullName}) بنجاح");
            }
            else
            {
                client.KycStatus = "Rejected";
                client.KycRejectionReason = dto.RejectionReason?.Trim() ?? "الوثائق المرفقة غير واضحة أو غير مطابقة للبيانات";
                client.KycReviewedAt = DateTime.UtcNow;
                client.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await LogActivityAsync(adminId, adminName, "Admin", "RejectKyc", "Client", client.Id.ToString(), $"تم رفض توثيق هوية العميل: {client.FullName}. السبب: {client.KycRejectionReason}");
                return (true, $"تم رفض توثيق حساب العميل ({client.FullName}). السبب: {client.KycRejectionReason}");
            }
        }

        #endregion

        #region إدارة وتعديل العملاء

        public async Task<List<ClientProfileDto>> GetAllClientsAsync()
        {
            var clients = await _context.Clients
                .Where(c => c.Role == "Client")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return clients.Select(c => MapToProfileDto(c)).ToList();
        }

        public async Task<ClientDetailsDto?> GetClientDetailsAsync(Guid clientId)
        {
            var client = await _context.Clients
                .Include(c => c.Wallets)
                    .ThenInclude(w => w.Currency)
                .Include(c => c.PosPoints)
                .Include(c => c.Devices)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null) return null;

            var clientProfile = MapToProfileDto(client);

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

            var devices = client.Devices.Select(d => new ClientDeviceDto
            {
                Id = d.Id,
                ClientId = d.ClientId,
                DeviceId = d.DeviceId,
                DeviceName = d.DeviceName,
                MainDevice = d.MainDevice,
                IsApproved = d.IsApproved,
                ApprovedBy = d.ApprovedBy,
                ApprovedAt = d.ApprovedAt,
                IpAddress = d.IpAddress,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).OrderByDescending(d => d.MainDevice).ThenByDescending(d => d.CreatedAt).ToList();

            var transactions = await _context.Transactions
                .Include(t => t.PosPoint)
                .Include(t => t.SenderClient)
                .Include(t => t.ReceiverClient)
                .Where(t => t.SenderClientId == clientId || t.ReceiverClientId == clientId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(25)
                .ToListAsync();

            var currencies = await _context.Currencies.ToDictionaryAsync(c => c.Code, c => c.Symbol);

            var trxDtos = transactions.Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                Type = t.Type,
                TypeNameAr = t.Type == "TransferByPhone" ? (t.ReceiverClientId == clientId ? "حوالة واردة" : "حوالة صادرة") : (t.Type == "PosPayment" ? "دفع لنقطة بيع" : t.Type == "SelfExchange" ? "تحويل بين حساباتي" : "إيداع"),
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
                SenderClientId = t.SenderClientId,
                SenderRealFullName = t.SenderClient?.FullName ?? t.SenderDisplayName,
                SenderRealPhone = t.SenderClient?.Phone ?? t.SenderDisplayPhone,
                ReceiverClientId = t.ReceiverClientId,
                ReceiverRealFullName = t.ReceiverClient?.FullName ?? t.ReceiverDisplayName,
                ReceiverRealPhone = t.ReceiverClient?.Phone ?? "",
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
                Devices = devices,
                RecentTransactions = trxDtos
            };
        }

        public async Task<(bool Success, string Message)> UpdateClientAsync(Guid clientId, AdminUpdateClientDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "العميل غير موجود");

            // التحقق من تكرار البريد أو الهاتف لمستخدم آخر
            if (await _context.Clients.AnyAsync(c => c.Id != clientId && c.Email.ToLower() == dto.Email.Trim().ToLower()))
            {
                return (false, "البريد الإلكتروني مسجل مسبقاً لمستخدم آخر");
            }

            if (await _context.Clients.AnyAsync(c => c.Id != clientId && c.Phone == dto.Phone.Trim()))
            {
                return (false, "رقم الهاتف مسجل مسبقاً لمستخدم آخر");
            }

            client.FullName = dto.FullName.Trim();
            client.Email = dto.Email.Trim().ToLower();
            client.Phone = dto.Phone.Trim();
            client.KycStatus = dto.KycStatus;
            client.IsBlocked = dto.IsBlocked;
            client.BlockedMessage = dto.IsBlocked ? dto.BlockedMessage : null;
            client.HideFullName = dto.HideFullName;
            client.HidePhoneOnPos = dto.HidePhoneOnPos;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "UpdateClient", "Client", client.Id.ToString(), $"تم تعديل بيانات العميل: {client.FullName}");
            return (true, "تم تحديث بيانات العميل بنجاح");
        }

        #endregion

        #region إدارة الأجهزة (Device Management)

        public async Task<List<ClientDeviceDto>> GetClientDevicesAsync(Guid clientId)
        {
            var devices = await _context.Devices
                .Where(d => d.ClientId == clientId)
                .OrderByDescending(d => d.MainDevice)
                .ThenByDescending(d => d.CreatedAt)
                .ToListAsync();

            return devices.Select(d => new ClientDeviceDto
            {
                Id = d.Id,
                ClientId = d.ClientId,
                DeviceId = d.DeviceId,
                DeviceName = d.DeviceName,
                MainDevice = d.MainDevice,
                IsApproved = d.IsApproved,
                ApprovedBy = d.ApprovedBy,
                ApprovedAt = d.ApprovedAt,
                IpAddress = d.IpAddress,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();
        }

        public async Task<(bool Success, string Message)> ApproveDeviceAsync(Guid deviceId, Guid? adminId = null, string adminName = "Admin")
        {
            var device = await _context.Devices.Include(d => d.Client).FirstOrDefaultAsync(d => d.Id == deviceId);
            if (device == null) return (false, "الجهاز غير موجود");

            device.IsApproved = true;
            device.ApprovedBy = adminName;
            device.ApprovedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "ApproveDevice", "Device", device.Id.ToString(), $"تمت الموافقة على دخول الجهاز ({device.DeviceName ?? device.DeviceId}) للعميل {device.Client?.FullName}");
            return (true, $"تمت الموافقة على الجهاز ({device.DeviceName ?? "جهاز العميل"}) بنجاح");
        }

        public async Task<(bool Success, string Message)> SetMainDeviceAsync(Guid deviceId, Guid? adminId = null, string adminName = "Admin")
        {
            var device = await _context.Devices.Include(d => d.Client).FirstOrDefaultAsync(d => d.Id == deviceId);
            if (device == null) return (false, "الجهاز غير موجود");

            // إلغاء تعيين الجهاز الرئيسي لجميع أجهزة العميل الأخرى
            var otherDevices = await _context.Devices.Where(d => d.ClientId == device.ClientId).ToListAsync();
            foreach (var d in otherDevices)
            {
                d.MainDevice = false;
            }

            device.MainDevice = true;
            device.IsApproved = true;
            device.ApprovedBy = adminName;
            device.ApprovedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "SetMainDevice", "Device", device.Id.ToString(), $"تم تعيين الجهاز ({device.DeviceName ?? device.DeviceId}) كجهاز رئيسي للعميل {device.Client?.FullName}");
            return (true, $"تم تعيين الجهاز ({device.DeviceName ?? "الجهاز"}) كجهاز رئيسي معتمد للعميل");
        }

        public async Task<(bool Success, string Message)> DeleteDeviceAsync(Guid deviceId, Guid? adminId = null, string adminName = "Admin")
        {
            var device = await _context.Devices.Include(d => d.Client).FirstOrDefaultAsync(d => d.Id == deviceId);
            if (device == null) return (false, "الجهاز غير موجود");

            var clientName = device.Client?.FullName ?? "";
            var devName = device.DeviceName ?? device.DeviceId ?? "Device";

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "DeleteDevice", "Device", deviceId.ToString(), $"تم حذف ارتباط الجهاز ({devName}) للعميل {clientName}");
            return (true, "تم حذف ارتباط الجهاز بنجاح");
        }

        #endregion

        #region إدارة الموظفين والصلاحيات (Staff Management)

        public async Task<List<StaffUserDto>> GetAllStaffAsync()
        {
            var staff = await _context.Clients
                .Where(c => c.Role != "Client")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return staff.Select(s => new StaffUserDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Phone = s.Phone,
                Role = s.Role,
                RoleNameAr = s.Role switch
                {
                    "Admin" => "مدير عام بكامل الصلاحيات",
                    "KycOfficer" => "مسؤول توثيق الهويات",
                    "CurrencyOfficer" => "مسؤول العملات والصرف",
                    "Auditor" => "مدقق العمليات المالية",
                    _ => s.Role
                },
                IsBlocked = s.IsBlocked,
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        public async Task<(bool Success, string Message, StaffUserDto? Data)> CreateStaffAsync(CreateStaffDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            if (await _context.Clients.AnyAsync(c => c.Email.ToLower() == dto.Email.Trim().ToLower()))
            {
                return (false, "البريد الإلكتروني مسجل مسبقاً", null);
            }

            if (await _context.Clients.AnyAsync(c => c.Phone == dto.Phone.Trim()))
            {
                return (false, "رقم الهاتف مسجل مسبقاً", null);
            }

            var staff = new Client
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                KycStatus = "Approved",
                HideFullName = false,
                HidePhoneOnPos = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Clients.AddAsync(staff);
            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "CreateStaff", "Staff", staff.Id.ToString(), $"تم إنشاء حساب موظف جديد: {staff.FullName} برتبة ({staff.Role})");

            var result = new StaffUserDto
            {
                Id = staff.Id,
                FullName = staff.FullName,
                Email = staff.Email,
                Phone = staff.Phone,
                Role = staff.Role,
                RoleNameAr = staff.Role == "Admin" ? "مدير عام" : staff.Role == "KycOfficer" ? "مسؤول توثيق الهويات" : staff.Role == "CurrencyOfficer" ? "مسؤول العملات" : "مدقق العمليات",
                IsBlocked = staff.IsBlocked,
                CreatedAt = staff.CreatedAt
            };

            return (true, $"تم إنشاء حساب الموظف ({staff.FullName}) بنجاح", result);
        }

        public async Task<(bool Success, string Message)> UpdateStaffRoleAsync(UpdateStaffRoleDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            var staff = await _context.Clients.FindAsync(dto.StaffId);
            if (staff == null || staff.Role == "Client") return (false, "الموظف غير موجود");

            var oldRole = staff.Role;
            staff.Role = dto.NewRole;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "UpdateStaffRole", "Staff", staff.Id.ToString(), $"تم تعديل دور الموظف {staff.FullName} من ({oldRole}) إلى ({staff.Role})");
            return (true, $"تم تحديث دور وصلاحية الموظف ({staff.FullName}) إلى ({staff.Role})");
        }

        public async Task<(bool Success, string Message)> ToggleStaffStatusAsync(Guid staffId, Guid? adminId = null, string adminName = "Admin")
        {
            var staff = await _context.Clients.FindAsync(staffId);
            if (staff == null || staff.Role == "Client") return (false, "الموظف غير موجود");

            staff.IsBlocked = !staff.IsBlocked;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "ToggleStaffStatus", "Staff", staff.Id.ToString(), $"تم {(staff.IsBlocked ? "تجميد" : "تفعيل")} حساب الموظف: {staff.FullName}");
            return (true, $"تم {(staff.IsBlocked ? "تجميد" : "تفعيل")} حساب الموظف ({staff.FullName}) بنجاح");
        }

        #endregion

        #region سجل الأنشطة والتدقيق (Audit Logs)

        public async Task<List<AuditLogResponseDto>> GetAuditLogsAsync()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync();

            return logs.Select(l => new AuditLogResponseDto
            {
                Id = l.Id,
                AdminId = l.AdminId,
                AdminName = l.AdminName,
                AdminRole = l.AdminRole,
                ActionType = l.ActionType,
                ActionTypeNameAr = l.ActionType switch
                {
                    "ApproveKyc" => "قبول توثيق هوية",
                    "RejectKyc" => "رفض توثيق هوية",
                    "UpdateClient" => "تعديل بيانات عميل",
                    "ResetPassword" => "إعادة تعيين كلمة مرور",
                    "BlockClient" => "حظر عميل",
                    "UnblockClient" => "فك حظر عميل",
                    "ApproveDevice" => "الموافقة على جهاز",
                    "SetMainDevice" => "تعيين جهاز رئيسي",
                    "DeleteDevice" => "حذف جهاز",
                    "CreateStaff" => "إضافة موظف",
                    "UpdateStaffRole" => "تعديل رتبة موظف",
                    "ToggleStaffStatus" => "تغيير حالة موظف",
                    "CreateCurrency" => "إضافة عملة جديدة",
                    "UpdateCurrency" => "تعديل عملة وسعر صرف",
                    "ToggleCurrency" => "تغيير حالة عملة",
                    _ => l.ActionType
                },
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Details = l.Details,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task LogActivityAsync(Guid? adminId, string adminName, string adminRole, string actionType, string entityName, string? entityId, string details, string? ipAddress = null)
        {
            try
            {
                var log = new AuditLog
                {
                    AdminId = adminId,
                    AdminName = !string.IsNullOrWhiteSpace(adminName) ? adminName : "Admin",
                    AdminRole = !string.IsNullOrWhiteSpace(adminRole) ? adminRole : "Admin",
                    ActionType = actionType,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // منع توقف العمليات في حال فشل تسجيل التدقيق
            }
        }

        #endregion

        #region نقاط البيع والعمليات وإدارة العملات

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
                MerchantEmail = p.Owner?.Email ?? string.Empty,
                MerchantKycStatus = p.Owner?.KycStatus ?? string.Empty,
                TotalSalesAmount = p.Transactions.Where(t => t.Status == "Completed").Sum(t => t.Amount),
                TotalSalesCount = p.Transactions.Count(t => t.Status == "Completed"),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<List<TransactionResponseDto>> GetAllTransactionsAsync(TransactionFilterDto? filter = null)
        {
            var query = _context.Transactions
                .Include(t => t.PosPoint)
                .Include(t => t.SenderClient)
                .Include(t => t.ReceiverClient)
                .AsQueryable();

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
                .Take(filter?.PageSize ?? 200)
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
                    "SelfExchange" => "تحويل بين حساباتي",
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
                SenderClientId = t.SenderClientId,
                SenderRealFullName = t.SenderClient?.FullName ?? t.SenderDisplayName,
                SenderRealPhone = t.SenderClient?.Phone ?? t.SenderDisplayPhone,
                ReceiverClientId = t.ReceiverClientId,
                ReceiverRealFullName = t.ReceiverClient?.FullName ?? t.ReceiverDisplayName,
                ReceiverRealPhone = t.ReceiverClient?.Phone ?? "",
                PosName = t.PosPoint?.Name,
                Note = t.Note,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<(bool Success, string Message, string TempPassword)> ResetClientPasswordAsync(Guid clientId, string? customTempPassword = null, Guid? adminId = null, string adminName = "Admin")
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return (false, "العميل غير موجود", string.Empty);

            var tempPassword = !string.IsNullOrWhiteSpace(customTempPassword) ? customTempPassword.Trim() : "123456";

            client.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            client.MustChangePassword = true;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "Admin", "ResetPassword", "Client", client.Id.ToString(), $"تمت إعادة تعيين كلمة المرور للعميل: {client.FullName} لكلمة مؤقتة");
            return (true, $"تمت إعادة تعيين كلمة المرور للعميل ({client.FullName}) إلى كلمة المرور المؤقتة: {tempPassword}", tempPassword);
        }

        public async Task<(bool Success, string Message)> ToggleBlockClientAsync(BlockClientDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            var client = await _context.Clients.FindAsync(dto.ClientId);
            if (client == null) return (false, "العميل غير موجود");

            client.IsBlocked = dto.IsBlocked;
            client.BlockedMessage = dto.IsBlocked ? (dto.BlockedMessage ?? "تم حظر الحساب لمخالفة شروط الاستخدام") : null;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var action = dto.IsBlocked ? "BlockClient" : "UnblockClient";
            await LogActivityAsync(adminId, adminName, "Admin", action, "Client", client.Id.ToString(), $"تم {(dto.IsBlocked ? "حظر" : "فك حظر")} العميل: {client.FullName}");
            return (true, dto.IsBlocked ? $"تم حظر حساب العميل ({client.FullName})" : $"تم إلغاء حظر حساب العميل ({client.FullName}) بنجاح");
        }

        public async Task<List<Currency>> GetAllCurrenciesAsync()
        {
            return await _context.Currencies.OrderBy(c => c.Id).ToListAsync();
        }

        public async Task<(bool Success, string Message, Currency? Data)> CreateCurrencyAsync(CurrencyCreateDto dto, Guid? adminId = null, string adminName = "Admin")
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

            await LogActivityAsync(adminId, adminName, "CurrencyOfficer", "CreateCurrency", "Currency", currency.Id.ToString(), $"تمت إضافة عملة جديدة: {currency.NameAr} ({currency.Code}) بسعر صرف {currency.ExchangeRate}");
            return (true, $"تم إضافة عملة ({currency.NameAr} - {currency.Code}) بنجاح", currency);
        }

        public async Task<(bool Success, string Message, Currency? Data)> UpdateCurrencyAsync(int id, CurrencyUpdateDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null) return (false, "العملة غير موجودة", null);

            var oldRate = currency.ExchangeRate;
            currency.NameAr = dto.NameAr.Trim();
            currency.NameEn = dto.NameEn.Trim();
            currency.Symbol = dto.Symbol.Trim();
            currency.IsActive = dto.IsActive;
            currency.ExchangeRate = dto.ExchangeRate;

            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "CurrencyOfficer", "UpdateCurrency", "Currency", currency.Id.ToString(), $"تم تعديل عملة {currency.NameAr} ({currency.Code}) - سعر الصرف تغير من {oldRate} إلى {currency.ExchangeRate}");
            return (true, $"تم تحديث بيانات عملة ({currency.NameAr}) بنجاح", currency);
        }

        public async Task<(bool Success, string Message)> ToggleCurrencyStatusAsync(int id, Guid? adminId = null, string adminName = "Admin")
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null) return (false, "العملة غير موجودة");

            currency.IsActive = !currency.IsActive;
            await _context.SaveChangesAsync();

            await LogActivityAsync(adminId, adminName, "CurrencyOfficer", "ToggleCurrency", "Currency", currency.Id.ToString(), $"تم {(currency.IsActive ? "تفعيل" : "تعطيل")} عملة {currency.NameAr} ({currency.Code})");
            return (true, $"تم {(currency.IsActive ? "تفعيل" : "تعطيل")} عملة ({currency.NameAr}) بنجاح");
        }

        #endregion

        #region إدارة الرسوم والعمولات (Fee Settings Implementation)

        /// <summary>
        /// استرجاع كافة إعدادات الرسوم والعمولات لكافة العمليات في النظام
        /// </summary>
        public async Task<List<FeeSettingResponseDto>> GetAllFeeSettingsAsync()
        {
            var fees = await _context.FeeSettings
                .OrderBy(f => f.Id)
                .ToListAsync();

            return fees.Select(f => MapToFeeSettingResponse(f)).ToList();
        }

        /// <summary>
        /// استرجاع إعداد رسوم محدد بالمعرف
        /// </summary>
        public async Task<FeeSettingResponseDto?> GetFeeSettingByIdAsync(int id)
        {
            var fee = await _context.FeeSettings.FindAsync(id);
            if (fee == null) return null;
            return MapToFeeSettingResponse(fee);
        }

        /// <summary>
        /// إنشاء قاعدة ورسوم جديدة لعملية وعملة محددة مع التوثيق في سجل التدقيق
        /// </summary>
        public async Task<(bool Success, string Message, FeeSettingResponseDto? Data)> CreateFeeSettingAsync(CreateFeeSettingDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            var curr = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? null : dto.CurrencyCode.Trim().ToUpper();

            // التحقق من عدم تكرار نفس العملية مع نفس العملة
            var exists = await _context.FeeSettings.AnyAsync(f => f.OperationType == dto.OperationType && f.CurrencyCode == curr);
            if (exists)
            {
                var targetText = string.IsNullOrEmpty(curr) ? "كافة العملات (عام)" : $"عملة ({curr})";
                return (false, $"توجد بالفعل سياسة رسوم مسجلة لنفس العملية لـ {targetText}", null);
            }

            // التحقق من وجود العملة إذا تم تحديد عملة خاصة
            if (!string.IsNullOrEmpty(curr))
            {
                var currencyExists = await _context.Currencies.AnyAsync(c => c.Code == curr);
                if (!currencyExists)
                {
                    return (false, $"العملة المحددة ({curr}) غير موجودة في النظام", null);
                }
            }

            var fee = new FeeSetting
            {
                OperationType = dto.OperationType,
                CurrencyCode = curr,
                NameAr = dto.NameAr.Trim(),
                Description = dto.Description?.Trim(),
                FeeType = dto.FeeType,
                Percentage = dto.Percentage,
                FixedAmount = dto.FixedAmount,
                MinFee = dto.MinFee,
                MaxFee = dto.MaxFee,
                IsActive = dto.IsActive,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminName
            };

            _context.FeeSettings.Add(fee);
            await _context.SaveChangesAsync();

            // توثيق العملية في سجل التدقيق
            await LogActivityAsync(
                adminId,
                adminName,
                "Admin",
                "CreateFeeSetting",
                "FeeSetting",
                fee.Id.ToString(),
                $"تم إنشاء سياسة رسوم جديدة ({fee.NameAr}) للعملية ({fee.OperationType}) والعملة ({fee.CurrencyCode ?? "الكل"}): النوع={fee.FeeType}, النسبة={fee.Percentage}%, المبلغ={fee.FixedAmount}"
            );

            return (true, $"تم إنشاء سياسة الرسوم ({fee.NameAr}) بنجاح", MapToFeeSettingResponse(fee));
        }

        /// <summary>
        /// تعديل سياسة ورسوم عملية محددة من قبل مدير النظام مع التوثيق في سجل التدقيق
        /// </summary>
        public async Task<(bool Success, string Message, FeeSettingResponseDto? Data)> UpdateFeeSettingAsync(int id, UpdateFeeSettingDto dto, Guid? adminId = null, string adminName = "Admin")
        {
            var fee = await _context.FeeSettings.FindAsync(id);
            if (fee == null)
            {
                return (false, "إعداد الرسوم غير موجود في النظام", null);
            }

            var oldPercentage = fee.Percentage;
            var oldFixed = fee.FixedAmount;
            var oldActive = fee.IsActive;

            if (!string.IsNullOrWhiteSpace(dto.NameAr))
            {
                fee.NameAr = dto.NameAr.Trim();
            }
            fee.FeeType = dto.FeeType;
            fee.Percentage = dto.Percentage;
            fee.FixedAmount = dto.FixedAmount;
            fee.MinFee = dto.MinFee;
            fee.MaxFee = dto.MaxFee;
            fee.IsActive = dto.IsActive;
            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                fee.Description = dto.Description.Trim();
            }
            fee.UpdatedAt = DateTime.UtcNow;
            fee.UpdatedBy = adminName;

            await _context.SaveChangesAsync();

            // توثيق التعديل في سجل التدقيق
            await LogActivityAsync(
                adminId,
                adminName,
                "Admin",
                "UpdateFeeSetting",
                "FeeSetting",
                fee.Id.ToString(),
                $"تم تعديل رسوم ({fee.NameAr}): النوع={fee.FeeType}, النسبة={fee.Percentage}% (سابقاً {oldPercentage}%), المبلغ الثابت={fee.FixedAmount} (سابقاً {oldFixed}), الحالة={(fee.IsActive ? "مفعل" : "معطل")}"
            );

            return (true, $"تم تحديث إعدادات رسوم ({fee.NameAr}) بنجاح", MapToFeeSettingResponse(fee));
        }

        /// <summary>
        /// حذف قاعدة رسوم مخصصة من قبل المدير مع التوثيق في سجل التدقيق
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteFeeSettingAsync(int id, Guid? adminId = null, string adminName = "Admin")
        {
            var fee = await _context.FeeSettings.FindAsync(id);
            if (fee == null)
            {
                return (false, "إعداد الرسوم غير موجود");
            }

            var feeName = fee.NameAr;
            var opType = fee.OperationType;
            var curr = fee.CurrencyCode;

            _context.FeeSettings.Remove(fee);
            await _context.SaveChangesAsync();

            // توثيق الحذف في سجل التدقيق
            await LogActivityAsync(
                adminId,
                adminName,
                "Admin",
                "DeleteFeeSetting",
                "FeeSetting",
                id.ToString(),
                $"تم حذف سياسة الرسوم ({feeName}) للعملية ({opType}) والعملة ({curr ?? "الكل"})"
            );

            return (true, $"تم حذف سياسة الرسوم ({feeName}) بنجاح");
        }

        /// <summary>
        /// جلب قاعدة الرسوم الفعالة لنوع عملية وعملة محددة
        /// </summary>
        public async Task<FeeSetting?> GetFeeRuleAsync(string operationType, string? currencyCode = null)
        {
            // أولاً البحث عن إعداد مخصص لنفس العملة
            if (!string.IsNullOrWhiteSpace(currencyCode))
            {
                var specificFee = await _context.FeeSettings
                    .FirstOrDefaultAsync(f => f.OperationType == operationType && f.CurrencyCode == currencyCode.ToUpper() && f.IsActive);

                if (specificFee != null) return specificFee;
            }

            // في حال عدم وجود إعداد خاص بالعملة، نأخذ الإعداد العام
            var generalFee = await _context.FeeSettings
                .FirstOrDefaultAsync(f => f.OperationType == operationType && (f.CurrencyCode == null || f.CurrencyCode == "") && f.IsActive);

            return generalFee;
        }

        /// <summary>
        /// معاينة وحساب الرسوم والمبلغ الإجمالي المخصوم لتطبيق الهاتف ولوحة التحكم
        /// </summary>
        public async Task<FeeCalculationPreviewDto> CalculateFeePreviewAsync(string operationType, decimal amount, string currencyCode)
        {
            var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode.ToUpper());
            var symbol = currency?.Symbol ?? currencyCode;

            var feeRule = await GetFeeRuleAsync(operationType, currencyCode);

            if (feeRule == null || !feeRule.IsActive)
            {
                return new FeeCalculationPreviewDto
                {
                    OperationType = operationType,
                    OriginalAmount = amount,
                    CurrencyCode = currencyCode.ToUpper(),
                    CurrencySymbol = symbol,
                    FeeAmount = 0.00m,
                    TotalAmountToDebit = amount,
                    EffectivePercentage = 0.00m,
                    FeeRuleDescription = "هذه العملية معفاة من الرسوم والعمولات",
                    IsFeeWaived = true
                };
            }

            var calculatedFee = feeRule.CalculateFee(amount);
            var totalDebit = amount + calculatedFee;
            var effectivePerc = amount > 0 ? (calculatedFee / amount) * 100m : 0m;

            string scopeText = string.IsNullOrEmpty(feeRule.CurrencyCode)
                ? "سياسة عامة (كافة العملات)"
                : $"سياسة مخصصة لعملة ({feeRule.CurrencyCode})";

            string feeDesc;
            if (feeRule.FeeType == "Percentage")
                feeDesc = $"{feeRule.NameAr} [{scopeText}] - عمولة {feeRule.Percentage:0.##}%";
            else if (feeRule.FeeType == "Fixed")
                feeDesc = $"{feeRule.NameAr} [{scopeText}] - رسوم مقطوعة {feeRule.FixedAmount:N2} {symbol}";
            else
                feeDesc = $"{feeRule.NameAr} [{scopeText}] - عمولة {feeRule.Percentage:0.##}% + {feeRule.FixedAmount:N2} {symbol}";

            if (feeRule.MinFee > 0) feeDesc += $" (حد أدنى {feeRule.MinFee:N2})";
            if (feeRule.MaxFee > 0) feeDesc += $" (حد أقصى {feeRule.MaxFee:N2})";

            return new FeeCalculationPreviewDto
            {
                OperationType = operationType,
                OriginalAmount = amount,
                CurrencyCode = currencyCode.ToUpper(),
                CurrencySymbol = symbol,
                FeeAmount = calculatedFee,
                TotalAmountToDebit = totalDebit,
                EffectivePercentage = Math.Round(effectivePerc, 2),
                FeeRuleDescription = feeDesc,
                IsFeeWaived = calculatedFee == 0
            };
        }

        private static FeeSettingResponseDto MapToFeeSettingResponse(FeeSetting fee)
        {
            string opNameAr = fee.OperationType switch
            {
                "TransferByPhone" => "تحويل لمشترك برقم الهاتف",
                "SelfExchange" => "الصرف والمصارفة بين الحسابات",
                "PosPayment" => "الدفع والمشتريات عبر نقاط البيع (POS)",
                "Deposit" => "إيداع وتغذية الرصيد",
                "Withdrawal" => "السحب النقدي",
                _ => fee.OperationType
            };

            string feeTypeNameAr = fee.FeeType switch
            {
                "Percentage" => "نسبة مئوية (%)",
                "Fixed" => "مبلغ ثابت مقطوع",
                "PercentageAndFixed" => "نسبة مئوية + مبلغ ثابت",
                _ => fee.FeeType
            };

            return new FeeSettingResponseDto
            {
                Id = fee.Id,
                OperationType = fee.OperationType,
                OperationTypeNameAr = opNameAr,
                CurrencyCode = fee.CurrencyCode,
                NameAr = fee.NameAr,
                Description = fee.Description,
                FeeType = fee.FeeType,
                FeeTypeNameAr = feeTypeNameAr,
                Percentage = fee.Percentage,
                FixedAmount = fee.FixedAmount,
                MinFee = fee.MinFee,
                MaxFee = fee.MaxFee,
                IsActive = fee.IsActive,
                UpdatedAt = fee.UpdatedAt,
                UpdatedBy = fee.UpdatedBy
            };
        }

        #endregion

        #region دالة مساعدة

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

        #endregion
    }
}
