import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../core/theme_provider.dart';
import '../kyc/kyc_upload_screen.dart';
import '../auth/login_screen.dart';

/// شاشة الإعدادات والخصوصية والأمان (Settings & Privacy Screen)
/// تشمل ميزات الخصوصية (إخفاء الاسم، إخفاء الهاتف على POS وتوليد الرقم البديل)، البصمة، وتغيير السمة وكلمة المرور
class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  bool _hideFullName = false;
  bool _hidePhoneOnPos = false;
  bool _isBiometric = false;
  bool _isInitialized = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_isInitialized) {
      final user = Provider.of<AuthProvider>(context, listen: false).user;
      if (user != null) {
        _hideFullName = user.hideFullName;
        _hidePhoneOnPos = user.hidePhoneOnPos;
        _isBiometric = user.isBiometricEnabled;
      }
      _isInitialized = true;
    }
  }

  /// حفظ تحديثات إعدادات الخصوصية
  Future<void> _savePrivacySettings() async {
    final auth = Provider.of<AuthProvider>(context, listen: false);
    final success = await auth.updatePrivacy(
      hideFullName: _hideFullName,
      hidePhoneOnPos: _hidePhoneOnPos,
      isBiometricEnabled: _isBiometric,
    );

    if (mounted && success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('تم تحديث إعدادات الخصوصية والأمان بنجاح'),
          backgroundColor: Colors.green,
        ),
      );
    }
  }

  /// فتح حوار تغيير كلمة المرور
  void _openChangePasswordDialog() {
    final currentPassController = TextEditingController();
    final newPassController = TextEditingController();
    final confirmPassController = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Row(
          children: [
            Icon(Icons.lock_reset_rounded, color: Colors.blue),
            SizedBox(width: 8),
            Text(
              'تغيير كلمة المرور',
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
            ),
          ],
        ),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: currentPassController,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'كلمة المرور الحالية',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: newPassController,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'كلمة المرور الجديدة',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: confirmPassController,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'تأكيد كلمة المرور الجديدة',
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('إلغاء'),
          ),
          ElevatedButton(
            onPressed: () async {
              if (newPassController.text.length < 6 ||
                  newPassController.text != confirmPassController.text) {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('كلمة المرور غير متطابقة أو أقل من 6 أحرف'),
                  ),
                );
                return;
              }

              Navigator.pop(ctx);
              final auth = Provider.of<AuthProvider>(context, listen: false);
              final success = await auth.changePassword(
                currentPassword: currentPassController.text,
                newPassword: newPassController.text,
              );

              if (mounted) {
                if (success) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('تم تغيير كلمة المرور بنجاح'),
                      backgroundColor: Colors.green,
                    ),
                  );
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(auth.errorMessage ?? 'فشل التغيير'),
                      backgroundColor: Colors.red,
                    ),
                  );
                }
              }
            },
            child: const Text('حفظ'),
          ),
        ],
      ),
    );
  }

  /// فتح حوار اختيار المظهر / الثيم
  void _openThemeSelector() {
    final themeProvider = Provider.of<ThemeProvider>(context, listen: false);

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Text(
          'اختيار مظهر التطبيق',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            RadioListTile<ThemeMode>(
              title: const Text('الوضع الفاتح (Light)'),
              value: ThemeMode.light,
              groupValue: themeProvider.themeMode,
              onChanged: (val) {
                if (val != null) themeProvider.setThemeMode(val);
                Navigator.pop(ctx);
              },
            ),
            RadioListTile<ThemeMode>(
              title: const Text('الوضع الداكن (Dark)'),
              value: ThemeMode.dark,
              groupValue: themeProvider.themeMode,
              onChanged: (val) {
                if (val != null) themeProvider.setThemeMode(val);
                Navigator.pop(ctx);
              },
            ),
            RadioListTile<ThemeMode>(
              title: const Text('تلقائي (بحسب إعدادات الجهاز)'),
              value: ThemeMode.system,
              groupValue: themeProvider.themeMode,
              onChanged: (val) {
                if (val != null) themeProvider.setThemeMode(val);
                Navigator.pop(ctx);
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = Provider.of<AuthProvider>(context);
    final themeProvider = Provider.of<ThemeProvider>(context);
    final user = auth.user;

    return Scaffold(
      appBar: AppBar(title: const Text('الإعدادات والخصوصية')),
      body: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        children: [
          // 1. بطاقة الملف الشخصي
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: theme.cardTheme.color,
              borderRadius: BorderRadius.circular(18),
            ),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 30,
                  backgroundColor: theme.primaryColor.withOpacity(0.15),
                  child: Icon(
                    Icons.person_rounded,
                    size: 34,
                    color: theme.primaryColor,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        user?.fullName ?? 'العميل',
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                        ),
                      ),
                      Text(
                        user?.phone ?? '',
                        style: const TextStyle(
                          color: Colors.grey,
                          fontSize: 13,
                          fontFamily: 'monospace',
                        ),
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          if (user?.isApproved == true)
                            const Badge(
                              label: Text('موثق'),
                              backgroundColor: Colors.green,
                            )
                          else if (user?.isPendingApproval == true)
                            const Badge(
                              label: Text('قيد المراجعة'),
                              backgroundColor: Colors.amber,
                            )
                          else
                            const Badge(
                              label: Text('غير موثق'),
                              backgroundColor: Colors.red,
                            ),
                        ],
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(
                    Icons.verified_user_outlined,
                    color: Colors.blue,
                  ),
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => const KycUploadScreen(),
                      ),
                    );
                  },
                  tooltip: 'وثائق التوثيق KYC',
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),

          // 2. قسم الخصوصية
          const Text(
            'إعدادات الخصوصية',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 15,
              color: Colors.blue,
            ),
          ),
          const SizedBox(height: 8),

          Container(
            decoration: BoxDecoration(
              color: theme.cardTheme.color,
              borderRadius: BorderRadius.circular(18),
            ),
            child: Column(
              children: [
                SwitchListTile(
                  title: const Text(
                    'إخفاء الاسم بالرموز والحروف الأولى',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                  subtitle: const Text(
                    'عند تفعيلها يظهر للطرف الآخر أول حرف من اسمك الرباعي فقط (مثال: أ.م.ع.ص) عند البحث والتحويل.',
                    style: TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                  value: _hideFullName,
                  onChanged: (val) {
                    setState(() => _hideFullName = val);
                    _savePrivacySettings();
                  },
                ),
                const Divider(height: 1),
                SwitchListTile(
                  title: const Text(
                    'إخفاء رقم الهاتف عند الدفع لنقطة بيع',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                  subtitle: const Text(
                    'عند الشراء من متجر أو نقطة بيع، يظهر للتاجر رقمك البديل المشفر بدلاً من رقم هاتفك الحقيقي.',
                    style: TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                  value: _hidePhoneOnPos,
                  onChanged: (val) {
                    setState(() => _hidePhoneOnPos = val);
                    _savePrivacySettings();
                  },
                ),
                if (user?.posAliasPhone != null) ...[
                  const Divider(height: 1),
                  ListTile(
                    title: const Text(
                      'الرقم البديل الحالي لنقاط البيع',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    subtitle: Text(
                      user!.posAliasPhone!,
                      style: const TextStyle(
                        fontFamily: 'monospace',
                        color: Colors.blue,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    trailing: TextButton.icon(
                      onPressed: () async {
                        final newAlias = await auth.regeneratePosAlias();
                        if (mounted && newAlias != null) {
                          if(context.mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(
                                'تم توليد رقم بديل جديد: $newAlias',
                              ),
                              backgroundColor: Colors.green,
                            ),
                          );
                          }
                        }
                      },
                      icon: const Icon(Icons.refresh, size: 16),
                      label: const Text('تجديد'),
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 20),

          // 3. قسم الأمان والمظهر
          const Text(
            'الأمان والمظهر العام',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 15,
              color: Colors.blue,
            ),
          ),
          const SizedBox(height: 8),

          Container(
            decoration: BoxDecoration(
              color: theme.cardTheme.color,
              borderRadius: BorderRadius.circular(18),
            ),
            child: Column(
              children: [
                SwitchListTile(
                  title: const Text(
                    'تسجيل الدخول ببصمة الإصبع',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                  subtitle: const Text(
                    'تفعيل الدخول البيومتري السريع للتطبيق',
                    style: TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                  value: _isBiometric,
                  onChanged: (val) {
                    setState(() => _isBiometric = val);
                    _savePrivacySettings();
                  },
                ),
                const Divider(height: 1),
                ListTile(
                  leading: const Icon(Icons.key_rounded, color: Colors.blue),
                  title: const Text(
                    'تغيير كلمة المرور',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                  trailing: const Icon(
                    Icons.arrow_forward_ios_rounded,
                    size: 16,
                  ),
                  onTap: _openChangePasswordDialog,
                ),
                const Divider(height: 1),
                ListTile(
                  leading: const Icon(
                    Icons.palette_outlined,
                    color: Colors.purple,
                  ),
                  title: const Text(
                    'المظهر والسمات',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                  subtitle: Text(
                    themeProvider.themeMode == ThemeMode.light
                        ? 'الوضع الفاتح'
                        : themeProvider.themeMode == ThemeMode.dark
                        ? 'الوضع الداكن'
                        : 'تلقائي (حسب الجهاز)',
                    style: const TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                  trailing: const Icon(
                    Icons.arrow_forward_ios_rounded,
                    size: 16,
                  ),
                  onTap: _openThemeSelector,
                ),
              ],
            ),
          ),
          const SizedBox(height: 28),

          // 4. تسجيل الخروج
          ElevatedButton.icon(
            onPressed: () async {
              await auth.logout();
              if (context.mounted) {
                Navigator.pushAndRemoveUntil(
                  context,
                  MaterialPageRoute(builder: (_) => const LoginScreen()),
                  (route) => false,
                );
              }
            },
            icon: const Icon(Icons.logout_rounded),
            label: const Text('تسجيل الخروج من الحساب'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.red.shade700,
              padding: const EdgeInsets.symmetric(vertical: 14),
            ),
          ),
          const SizedBox(height: 20),
        ],
      ),
    );
  }
}
