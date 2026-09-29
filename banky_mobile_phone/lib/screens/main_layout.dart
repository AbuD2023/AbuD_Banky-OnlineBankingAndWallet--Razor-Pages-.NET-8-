import 'package:flutter/material.dart';
import 'home/home_screen.dart';
import 'pos/my_pos_screen.dart';
import 'wallets/wallets_screen.dart';
import 'settings/settings_screen.dart';

/// الشاشة الهيكلية الرئيسية (Main Layout)
/// تحتوي على شريط التنقل السفلي للتبديل بين شاشات التطبيق الأربع
class MainLayout extends StatefulWidget {
  const MainLayout({super.key});

  @override
  State<MainLayout> createState() => _MainLayoutState();
}

class _MainLayoutState extends State<MainLayout> {
  int _currentIndex = 0;

  final List<Widget> _screens = const [
    HomeScreen(),
    MyPosScreen(),
    WalletsScreen(),
    SettingsScreen(),
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: _screens,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (idx) => setState(() => _currentIndex = idx),
        indicatorColor: theme.primaryColor.withOpacity(0.15),
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home_rounded, color: theme.primaryColor),
            label: 'الرئيسية',
          ),
          NavigationDestination(
            icon: const Icon(Icons.storefront_outlined),
            selectedIcon: Icon(Icons.storefront_rounded, color: theme.primaryColor),
            label: 'نقاط البيع',
          ),
          NavigationDestination(
            icon: const Icon(Icons.account_balance_wallet_outlined),
            selectedIcon: Icon(Icons.account_balance_wallet_rounded, color: theme.primaryColor),
            label: 'المحافظ',
          ),
          NavigationDestination(
            icon: const Icon(Icons.settings_outlined),
            selectedIcon: Icon(Icons.settings_rounded, color: theme.primaryColor),
            label: 'الإعدادات',
          ),
        ],
      ),
    );
  }
}
