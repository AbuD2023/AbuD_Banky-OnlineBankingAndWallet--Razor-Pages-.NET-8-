namespace Banky.Web.Models
{
    /// <summary>
    /// نموذج استجابة الـ API العامة
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// هل تمت العملية بنجاح
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// رسالة العملية من الـ API
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// البيانات المعادة مع الاستجابة
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// كلمة المرور المؤقتة في حال إعادة التعيين
        /// </summary>
        public string? TempPassword { get; set; }
    }

    /// <summary>
    /// استجابة بدون بيانات
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
