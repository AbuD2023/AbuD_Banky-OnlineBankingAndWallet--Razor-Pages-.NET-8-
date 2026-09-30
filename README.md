# AbuD-Banky — Online Banking & Wallet (Razor Pages, .NET 8)

نسخة عربية / Arabic version below

---

## نبذة سريعة
مشروع "AbuD-Banky" هو نظام ويب تعليمي/تطبيقي لإدارة الحسابات البنكية والمحفظة الرقمية، مبني باستخدام `ASP.NET Core Razor Pages` و`.NET 8`. المشروع يظهر مثالاً عملياً على كيفية ربط واجهة المستخدم (Razor Pages) بواجهة API منفصلة لإدارة العمليات المالية.

## الميزات
- واجهة ويب مبنية بـ `Razor Pages` قابلة للتوسع.
- API منفصلة للتعامل مع العمليات والمحفظة.
- إعدادات مرنة عبر `appsettings.json` (انظر `ApiSettings:BaseUrl`).
- جاهز للتشغيل محلياً ونشر (Docker / Azure) مع تعديلات بسيطة.

## Quick overview (English)
AbuD-Banky is a sample banking & wallet application using `ASP.NET Core Razor Pages` and `.NET 8`. It demonstrates a front-end Razor Pages site (`Banky.Web`) communicating with an API (`Banky.API`).

---

## المتطلبات / Requirements
- .NET 8 SDK
- (اختياري) قاعدة بيانات: SQL Server / SQLite حسب تكوين المشروع
- `dotnet` CLI

---

## تشغيل محلي سريع (Quick start)
1. استنساخ المستودع:
   ```bash
   git clone <repo-url>
   cd Banky
   ```
2. تشغيل الـ API (إذا كان موجودًا في الحل):
   ```bash
   dotnet run --project Banky.API
   ```
   افتراضيًا عنوان API يمكن تغييره في `Banky.Web/appsettings.json` تحت `ApiSettings:BaseUrl`.

3. تشغيل الواجهة (Razor Pages):
   ```bash
   dotnet run --project Banky.Web
   ```

4. افتح المستعرض وتوجه إلى العنوان المعروض (عادة `https://localhost:5xxx`).

---

## إعدادات مهمة
- `Banky.Web/appsettings.json` يحتوي على `ApiSettings:BaseUrl` لتوجيه طلبات الويب إلى خدمة الـ API.
- استخدم متغيرات البيئة أو `secrets` لتخزين connection strings وبيانات حساسة قبل النشر.

---

## تطوير ومساهمة
- اقرأ `CONTRIBUTING.md` لسياسة المساهمة وخطوات إعداد بيئة التطوير.
- اتبع نمط الفروع: `feature/<short>`, `fix/<short>`.

---

## تحسين الاكتشاف على GitHub (SEO tips)
- استخدم وصفًا قصيرًا واضحًا في حقل description في GitHub: "AbuD-Banky — Razor Pages banking & wallet example (ASP.NET Core, .NET 8)".
- أضف Topics عبر واجهة GitHub: `dotnet`, `dotnet8`, `aspnet-core`, `razor-pages`, `csharp`, `webapi`, `fintech`, `banking`, `wallet`, `tutorial`.
- أضف لقطات شاشة/ملف `social preview` لجذب الزوار.

---

## License
MIT — انظر ملف `LICENSE`.

---

## Contact / Demo
أضف هنا رابط العرض التجريبي أو تعليمات تشغيل البيانات التجريبية إذا رغبت في عرض نسخة تشغيل مبسطة.


---

# English section (short)

## What is AbuD-Banky?
Sample banking and wallet application built with `ASP.NET Core Razor Pages` and `.NET 8`. Demonstrates front-end Razor Pages with a separate API service.

## Quick Start (short)
- Clone repository
- `dotnet run --project Banky.API` (if API exists)
- `dotnet run --project Banky.Web`
- Adjust `Banky.Web/appsettings.json` -> `ApiSettings:BaseUrl` if needed.

## Topics to add on GitHub
`dotnet`, `dotnet8`, `aspnet-core`, `razor-pages`, `csharp`, `webapi`, `fintech`, `banking`, `wallet`, `tutorial`



