using Banky.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banky.API.Data
{
    /// <summary>
    /// سياق قاعدة البيانات الرئيسي لتطبيق Banky (AppDbContext)
    /// يدير الكيانات والعلاقات والاتصال بقاعدة بيانات SQL Server
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// منشئ السياق مع تمرير خيارات الإعداد
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// جدول العملاء والمستخدمين
        /// </summary>
        public DbSet<Client> Clients => Set<Client>();

        /// <summary>
        /// جدول العملات المعتمدة
        /// </summary>
        public DbSet<Currency> Currencies => Set<Currency>();

        /// <summary>
        /// جدول المحافظ المالية للعملاء
        /// </summary>
        public DbSet<Wallet> Wallets => Set<Wallet>();

        /// <summary>
        /// جدول نقاط البيع والمتاجر
        /// </summary>
        public DbSet<PosPoint> PosPoints => Set<PosPoint>();

        /// <summary>
        /// جدول الحركات والعمليات المالية والتحويلات
        /// </summary>
        public DbSet<Transaction> Transactions => Set<Transaction>();

        /// <summary>
        /// جدول أجهزة المستخدمين
        /// </summary>
        public DbSet<Device> Devices => Set<Device>();

        /// <summary>
        /// تكوين العلاقات والفهارس وقواعد البيانات عبر Fluent API
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. إعدادات جدول العملاء (Clients)
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();
                entity.HasIndex(c => c.KycStatus);
            });

            // 2. إعدادات جدول العملات (Currencies)
            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasIndex(c => c.Code).IsUnique();
            });

            // 3. إعدادات جدول المحافظ (Wallets)
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasIndex(w => w.AccountNumber).IsUnique();

                entity.HasOne(w => w.Client)
                      .WithMany(c => c.Wallets)
                      .HasForeignKey(w => w.ClientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Currency)
                      .WithMany(c => c.Wallets)
                      .HasPrincipalKey(c => c.Code)
                      .HasForeignKey(w => w.CurrencyCode)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. إعدادات جدول نقاط البيع (PosPoints)
            modelBuilder.Entity<PosPoint>(entity =>
            {
                entity.HasIndex(p => p.PosCode).IsUnique();

                entity.HasOne(p => p.Owner)
                      .WithMany(c => c.PosPoints)
                      .HasForeignKey(p => p.ClientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. إعدادات جدول العمليات والتحويلات (Transactions)
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(t => t.TransactionNumber).IsUnique();
                entity.HasIndex(t => t.Type);
                entity.HasIndex(t => t.CreatedAt);

                entity.HasOne(t => t.SenderClient)
                      .WithMany()
                      .HasForeignKey(t => t.SenderClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ReceiverClient)
                      .WithMany()
                      .HasForeignKey(t => t.ReceiverClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.PosPoint)
                      .WithMany(p => p.Transactions)
                      .HasForeignKey(t => t.PosPointId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 6. إعدادات جدول الأجهزة (Devices)
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasOne(d => d.Client)
                      .WithMany(c => c.Devices)
                      .HasForeignKey(d => d.ClientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
