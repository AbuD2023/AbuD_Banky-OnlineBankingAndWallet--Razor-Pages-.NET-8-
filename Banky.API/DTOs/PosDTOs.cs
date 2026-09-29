using System.ComponentModel.DataAnnotations;

namespace Banky.API.DTOs
{
    /// <summary>
    /// نموذج إنشاء نقطة بيع جديدة للعميل
    /// </summary>
    public class CreatePosDto
    {
        /// <summary>
        /// اسم المتجر أو نقطة البيع
        /// </summary>
        [Required(ErrorMessage = "اسم نقطة البيع مطلوب")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// العنوان أو الموقع
        /// </summary>
        [MaxLength(255)]
        public string? Address { get; set; }

        /// <summary>
        /// تصنيف النشاط التجاري
        /// </summary>
        [MaxLength(100)]
        public string? Category { get; set; }
    }

    /// <summary>
    /// استجابة بيانات نقطة البيع
    /// </summary>
    public class PosResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PosCode { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalReceivedAmount { get; set; }
        public int TotalTransactionsCount { get; set; }
    }
}
