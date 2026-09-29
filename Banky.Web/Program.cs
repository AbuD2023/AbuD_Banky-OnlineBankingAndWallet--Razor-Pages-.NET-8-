using Banky.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. تسجيل خدمات الاتصال بالـ API و HttpContext
// ==========================================
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IBankyApiClient, BankyApiClient>();

// ==========================================
// 2. إعداد المصادقة عبر ملفات تعريف الارتباط (Cookie Auth) للمسؤولين
// ==========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "BankyAdminAuthCookie";
    });

// ==========================================
// 3. تفعيل نظام MVC وعرض الصفحات (Razor Views)
// ==========================================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==========================================
// 4. إعداد خط معالجة الطلبات (HTTP Pipeline)
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
