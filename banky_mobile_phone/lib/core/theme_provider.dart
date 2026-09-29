import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// مزود حالة المظهر (Theme Provider)
/// يدير التبديل بين الوضع الفاتح، الداكن، أو التلقائي بحسب إعدادات نظام الهاتف
class ThemeProvider extends ChangeNotifier {
  static const String _themePrefKey = 'banky_app_theme_mode';
  ThemeMode _themeMode = ThemeMode.system;

  ThemeMode get themeMode => _themeMode;

  ThemeProvider() {
    _loadThemeFromPrefs();
  }

  /// استرجاع المظهر المحفوظ من الذاكرة المحلية
  Future<void> _loadThemeFromPrefs() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final savedTheme = prefs.getString(_themePrefKey);
      if (savedTheme == 'light') {
        _themeMode = ThemeMode.light;
      } else if (savedTheme == 'dark') {
        _themeMode = ThemeMode.dark;
      } else {
        _themeMode = ThemeMode.system;
      }
      notifyListeners();
    } catch (_) {}
  }

  /// تغيير وحفظ المظهر الجديد
  Future<void> setThemeMode(ThemeMode mode) async {
    _themeMode = mode;
    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      if (mode == ThemeMode.light) {
        await prefs.setString(_themePrefKey, 'light');
      } else if (mode == ThemeMode.dark) {
        await prefs.setString(_themePrefKey, 'dark');
      } else {
        await prefs.setString(_themePrefKey, 'system');
      }
    } catch (_) {}
  }
}
