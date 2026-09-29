import 'package:flutter/material.dart';
import '../../services/api_service.dart';

/// شاشة استعادة كلمة المرور (Forgot Password Screen)
/// تتيح إرسال رمز التحقق إلى الهاتف أو البريد ثم إدخال كلمة مرور جديدة
class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _identifierController = TextEditingController();
  final _tokenController = TextEditingController();
  final _newPasswordController = TextEditingController();

  bool _isCodeSent = false;
  bool _isLoading = false;

  @override
  void dispose() {
    _identifierController.dispose();
    _tokenController.dispose();
    _newPasswordController.dispose();
    super.dispose();
  }

  /// طلب إرسال رمز التحقق
  Future<void> _handleSendCode() async {
    final identifier = _identifierController.text.trim();
    if (identifier.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال الهاتف أو البريد أولاً')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final result = await ApiService.forgotPassword(identifier);
    setState(() => _isLoading = false);

    if (!mounted) return;

    if (result['success'] == true) {
      setState(() {
        _isCodeSent = true;
        if (result['demoToken'] != null) {
          _tokenController.text = result['demoToken'].toString();
        }
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'تم إرسال رمز التحقق'),
          backgroundColor: Colors.green,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'فشل طلب الرمز'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  /// تعيين كلمة المرور الجديدة
  Future<void> _handleResetPassword() async {
    final identifier = _identifierController.text.trim();
    final token = _tokenController.text.trim();
    final newPassword = _newPasswordController.text;

    if (token.isEmpty || newPassword.length < 6) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال رمز التحقق وكلمة مرور من 6 أحرف على الأقل')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final result = await ApiService.resetPassword(
      identifier: identifier,
      resetToken: token,
      newPassword: newPassword,
    );
    setState(() => _isLoading = false);

    if (!mounted) return;

    if (result['success'] == true) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('تم إعادة تعيين كلمة المرور بنجاح! يمكنك الآن تسجيل الدخول.'),
          backgroundColor: Colors.green,
        ),
      );
      Navigator.pop(context);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'فشلت العملية'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('استعادة كلمة المرور'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'نسيت كلمة المرور؟',
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                'أدخل هاتفك أو بريدك الإلكتروني لاستلام رمز الاستعادة وإعادة تعيين كلمة المرور',
                style: TextStyle(
                  fontSize: 14,
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                ),
              ),
              const SizedBox(height: 28),

              Text(
                'رقم الهاتف أو البريد الإلكتروني',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 8),
              TextFormField(
                controller: _identifierController,
                enabled: !_isCodeSent,
                decoration: const InputDecoration(
                  hintText: '77XXXXXXX أو user@banky.com',
                  prefixIcon: Icon(Icons.person_outline),
                ),
              ),
              const SizedBox(height: 16),

              if (!_isCodeSent)
                ElevatedButton(
                  onPressed: _isLoading ? null : _handleSendCode,
                  child: _isLoading
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                        )
                      : const Text('إرسال رمز التحقق'),
                ),

              if (_isCodeSent) ...[
                Text(
                  'رمز التحقق (OTP)',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: theme.colorScheme.onSurface,
                  ),
                ),
                const SizedBox(height: 8),
                TextFormField(
                  controller: _tokenController,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                    hintText: 'أدخل رمز التحقق المكون من 6 أرقام',
                    prefixIcon: Icon(Icons.pin_outlined),
                  ),
                ),
                const SizedBox(height: 16),

                Text(
                  'كلمة المرور الجديدة',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: theme.colorScheme.onSurface,
                  ),
                ),
                const SizedBox(height: 8),
                TextFormField(
                  controller: _newPasswordController,
                  obscureText: true,
                  decoration: const InputDecoration(
                    hintText: '••••••••',
                    prefixIcon: Icon(Icons.lock_outline),
                  ),
                ),
                const SizedBox(height: 24),

                ElevatedButton(
                  onPressed: _isLoading ? null : _handleResetPassword,
                  child: _isLoading
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                        )
                      : const Text('تأكيد وإعادة تعيين كلمة المرور'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
