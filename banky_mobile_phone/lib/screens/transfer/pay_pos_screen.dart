import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../models/wallet_model.dart';
import '../../models/pos_model.dart';
import '../../services/api_service.dart';

/// شاشة الدفع والشراء عبر نقطة بيع (Pay POS Screen)
/// تطبق ميزة إخفاء رقم الهاتف واستخدام الرقم البديل المولد عند الشراء من نقاط البيع لحماية خصوصية العميل
class PayPosScreen extends StatefulWidget {
  const PayPosScreen({super.key});

  @override
  State<PayPosScreen> createState() => _PayPosScreenState();
}

class _PayPosScreenState extends State<PayPosScreen> {
  final _posCodeController = TextEditingController(text: 'POS-100200'); // كود نقطة بيع تجريبية
  final _amountController = TextEditingController();
  final _noteController = TextEditingController();

  WalletModel? _selectedWallet;
  PosLookupResult? _posResult;

  bool _isCheckingPos = false;
  bool _isPaying = false;
  String? _lookupError;

  @override
  void initState() {
    super.initState();
    final auth = Provider.of<AuthProvider>(context, listen: false);
    if (auth.wallets.isNotEmpty) {
      _selectedWallet = auth.wallets.first;
    }
  }

  @override
  void dispose() {
    _posCodeController.dispose();
    _amountController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  /// التحقق من كود نقطة البيع
  Future<void> _checkPos() async {
    final code = _posCodeController.text.trim().toUpperCase();
    if (code.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال كود نقطة البيع')),
      );
      return;
    }

    setState(() {
      _isCheckingPos = true;
      _lookupError = null;
      _posResult = null;
    });

    final result = await ApiService.lookupPos(code);
    setState(() => _isCheckingPos = false);

    if (result != null) {
      setState(() => _posResult = result);
    } else {
      setState(() => _lookupError = 'كود نقطة البيع غير صحيح أو غير مفعل');
    }
  }

  /// تنفيذ الدفع لنقطة البيع
  Future<void> _handlePayment() async {
    if (_posResult == null) {
      await _checkPos();
      if (_posResult == null) return;
    }

    final amount = double.tryParse(_amountController.text.trim());
    if (amount == null || amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال مبلغ دفع صحيح')),
      );
      return;
    }

    if (_selectedWallet == null || _selectedWallet!.balance < amount) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('رصيد المحفظة غير كافٍ. رصيدك الحالي: ${_selectedWallet?.balance.toStringAsFixed(2)} ${_selectedWallet?.currencySymbol}')),
      );
      return;
    }

    setState(() => _isPaying = true);

    final result = await ApiService.payToPos(
      posCode: _posCodeController.text.trim().toUpperCase(),
      currencyCode: _selectedWallet!.currencyCode,
      amount: amount,
      note: _noteController.text.trim().isNotEmpty ? _noteController.text.trim() : null,
    );

    setState(() => _isPaying = false);

    if (!mounted) return;

    if (result['success'] == true) {
      final auth = Provider.of<AuthProvider>(context, listen: false);
      await auth.refreshWallets();

      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green.withOpacity(0.12),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.check_circle_rounded, color: Colors.green, size: 48),
              ),
              const SizedBox(height: 16),
              const Text(
                'تم الدفع بنجاح!',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                'تم دفع مبلغ ${amount.toStringAsFixed(2)} ${_selectedWallet!.currencySymbol} لنقطة بيع (${_posResult?.name})',
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 14),
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(ctx);
                  Navigator.pop(context);
                },
                child: const Text('تم'),
              ),
            ],
          ),
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'فشل الدفع'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = Provider.of<AuthProvider>(context);
    final user = auth.user;

    return Scaffold(
      appBar: AppBar(
        title: const Text('دفع وشراء عبر نقطة بيع (POS)'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // إشعار الخصوصية وتوليد الرقم البديل
              if (user?.hidePhoneOnPos == true)
                Container(
                  margin: const EdgeInsets.only(bottom: 16),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.blue.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: Colors.blue.shade200),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.shield_rounded, color: Colors.blue, size: 24),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          'ميزة الخصوصية مفعلة: سيظهر للتاجر رقمك البديل (${user?.posAliasPhone ?? "ALIAS-000000"}) بدلاً من رقم هاتفك الحقيقي.',
                          style: const TextStyle(fontSize: 12, color: Colors.blue),
                        ),
                      ),
                    ],
                  ),
                ),

              // اختيار محفظة الدفع
              Text(
                'اختر المحفظة للدفع منها',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                decoration: BoxDecoration(
                  color: theme.cardTheme.color,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                ),
                child: DropdownButtonHideUnderline(
                  child: DropdownButton<WalletModel>(
                    value: _selectedWallet,
                    isExpanded: true,
                    items: auth.wallets.map((w) {
                      return DropdownMenuItem<WalletModel>(
                        value: w,
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text('${w.currencyNameAr} (${w.currencyCode})', style: const TextStyle(fontWeight: FontWeight.bold)),
                            Text('${w.balance.toStringAsFixed(2)} ${w.currencySymbol}', style: const TextStyle(color: Colors.green, fontWeight: FontWeight.bold)),
                          ],
                        ),
                      );
                    }).toList(),
                    onChanged: (val) {
                      setState(() => _selectedWallet = val);
                    },
                  ),
                ),
              ),
              const SizedBox(height: 18),

              // كود نقطة البيع
              Text(
                'كود نقطة البيع (أو الـ QR)',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _posCodeController,
                      textCapitalization: TextCapitalization.characters,
                      decoration: const InputDecoration(
                        hintText: 'POS-XXXXXX',
                        prefixIcon: Icon(Icons.store_outlined),
                      ),
                      onChanged: (_) {
                        if (_posResult != null) setState(() => _posResult = null);
                      },
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton(
                    onPressed: _isCheckingPos ? null : _checkPos,
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                    ),
                    child: _isCheckingPos
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                          )
                        : const Text('تحقق'),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // نتيجة فحص نقطة البيع
              if (_posResult != null)
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: Colors.purple.withOpacity(0.08),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: Colors.purple.shade200),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.storefront_rounded, color: Colors.purple, size: 36),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              _posResult!.name,
                              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                            ),
                            Text(
                              'التاجر: ${_posResult!.merchantDisplayName} • ${_posResult!.category ?? "متجر"}',
                              style: TextStyle(fontSize: 12, color: theme.colorScheme.onSurface.withOpacity(0.6)),
                            ),
                            if (_posResult!.address != null)
                              Text(
                                'العنوان: ${_posResult!.address}',
                                style: TextStyle(fontSize: 11, color: theme.colorScheme.onSurface.withOpacity(0.5)),
                              ),
                          ],
                        ),
                      ),
                    ],
                  ),
                )
              else if (_lookupError != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: Colors.red.shade300),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.error_outline, color: Colors.red),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(_lookupError!, style: const TextStyle(color: Colors.red, fontSize: 13)),
                      ),
                    ],
                  ),
                ),
              const SizedBox(height: 18),

              // مبلغ الدفع
              Text(
                'قيمة المشتريات (${_selectedWallet?.currencySymbol ?? ""})',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 8),
              TextFormField(
                controller: _amountController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(
                  hintText: '0.00',
                  prefixIcon: const Icon(Icons.payment_rounded),
                  suffixText: _selectedWallet?.currencyCode,
                ),
              ),
              const SizedBox(height: 16),

              // رقم الفاتورة أو ملاحظات
              Text(
                'رقم الفاتورة أو البيان (اختياري)',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 8),
              TextFormField(
                controller: _noteController,
                decoration: const InputDecoration(
                  hintText: 'فاتورة رقم #1024',
                  prefixIcon: Icon(Icons.receipt_outlined),
                ),
              ),
              const SizedBox(height: 32),

              // زر تأكيد الدفع
              ElevatedButton.icon(
                onPressed: _isPaying ? null : _handlePayment,
                icon: const Icon(Icons.check_circle_outline_rounded),
                label: _isPaying
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                      )
                    : const Text('تأكيد الدفع لنقطة البيع'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.purple.shade700,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
