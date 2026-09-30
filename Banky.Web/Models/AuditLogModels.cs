namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج سجل التدقيق والأنشطة الإدارية في لوحة التحكم
    /// </summary>
    public class AuditLogModel
    {
        public Guid Id { get; set; }
        public Guid? AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string AdminRole { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionTypeNameAr { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
