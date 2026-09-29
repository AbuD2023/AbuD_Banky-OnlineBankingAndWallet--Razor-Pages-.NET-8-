import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/app_theme.dart';
import 'core/auth_provider.dart';
import 'core/theme_provider.dart';
import 'screens/splash_screen.dart';

/// نقطة الدخول الرئيسية لتطبيق Banky للهاتف المحمول (Main Entrypoint)
void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const BankyApp());
}

/// التطبيق الرئيسي BankyApp مع تفعيل مزودات الحالة وإعدادات اللغة العربية والمظهر
class BankyApp extends StatelessWidget {
  const BankyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        // 1. مزود إدارة المظهر والوضع الداكن/الفاتح
        ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
        // 2. مزود إدارة المستخدم والمصادقة والمحافظ
        ChangeNotifierProvider<AuthProvider>(create: (_) => AuthProvider()),
      ],
      child: Consumer<ThemeProvider>(
        builder: (context, themeProvider, _) {
          return MaterialApp(
            title: 'Banky Wallet',
            debugShowCheckedModeBanner: false,

            // ضبط اتجاه التطبيق بالكامل من اليمين إلى اليسار (RTL) باللغة العربية
            locale: const Locale('ar', 'YE'),

            // السمات والمظهر
            theme: AppTheme.lightTheme,
            darkTheme: AppTheme.darkTheme,
            themeMode: themeProvider.themeMode,

            // شاشة البداية والتحميل
            home: const Directionality(
              textDirection: TextDirection.rtl,
              child: SplashScreen(),
            ),
          );
        },
      ),
    );
  }
}
