import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../models/wallet_model.dart';
import '../../models/pos_model.dart';
import '../../services/api_service.dart';

/// شاشة التحويل المالي لمشترك برقم الهاتف (Transfer By Phone Screen)
/// تفحص المستلم وتطبق قواعد الخصوصية (إظهار الحروف الأولى فقط إذا كان خيار الإخفاء مفعل لدى المستلم)
class TransferByPhoneScreen extends StatefulWidget {
  const TransferByPhoneScreen({super.key});

  @override
  State<TransferByPhoneScreen> createState() => _TransferByPhoneScreenState();
}

class _TransferByPhoneScreenState extends State<TransferByPhoneScreen> {
  final _phoneController = TextEditingController();
  final _amountController = TextEditingController();
  final _noteController = TextEditingController();

  WalletModel? _selectedWallet;
  RecipientLookupResult? _recipientResult;

  bool _isCheckingRecipient = false;
  bool _isTransferring = false;
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
    _phoneController.dispose();
    _amountController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  /// التحقق من رقم هاتف المستلم وجلب اسمه وقواعد الخصوصية
  Future<void> _checkRecipient() async {
    final phone = _phoneController.text.trim();
    if (phone.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال رقم هاتف المستلم')),
      );
      return;
    }

    setState(() {
      _isCheckingRecipient = true;
      _lookupError = null;
      _recipientResult = null;
    });

    final currency = _selectedWallet?.currencyCode ?? 'YER';
    final result = await ApiService.lookupRecipient(phone: phone, currencyCode: currency);

    setState(() => _isCheckingRecipient = false);

    if (result != null) {
      setState(() => _recipientResult = result);
    } else {
      setState(() => _lookupError = 'رقم الهاتف غير مسجل في النظام أو الحساب غير موثق');
    }
  }

  /// تنفيذ التحويل المالي
  Future<void> _handleTransfer() async {
    if (_recipientResult == null) {
      await _checkRecipient();
      if (_recipientResult == null) return;
    }

    final amount = double.tryParse(_amountController.text.trim());
    if (amount == null || amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال مبلغ تحويل صحيح')),
      );
      return;
    }

    if (_selectedWallet == null || _selectedWallet!.balance < amount) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('رصيد محفظتك غير كافٍ. رصيدك الحالي: ${_selectedWallet?.balance.toStringAsFixed(2)} ${_selectedWallet?.currencySymbol}')),
      );
      return;
    }

    setState(() => _isTransferring = true);

    final result = await ApiService.transferByPhone(
      receiverPhone: _phoneController.text.trim(),
      currencyCode: _selectedWallet!.currencyCode,
      amount: amount,
      note: _noteController.text.trim().isNotEmpty ? _noteController.text.trim() : null,
    );

    setState(() => _isTransferring = false);

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
                'تم التحويل بنجاح!',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                'تم تحويل مبلغ ${amount.toStringAsFixed(2)} ${_selectedWallet!.currencySymbol} إلى ${_recipientResult?.displayName}',
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
          content: Text(result['message'] ?? 'فشل التحويل'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = Provider.of<AuthProvider>(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('تحويل لمشترك برقم الهاتف'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // اختيار المحفظة المصدر
              Text(
                'اختر المحفظة والعملة المصدر',
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
                      setState(() {
                        _selectedWallet = val;
                        _recipientResult = null;
                      });
                    },
                  ),
                ),
              ),
              const SizedBox(height: 18),

              // رقم هاتف المستلم
              Text(
                'رقم هاتف المستلم',
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
                      controller: _phoneController,
                      keyboardType: TextInputType.phone,
                      decoration: const InputDecoration(
                        hintText: '77XXXXXXX',
                        prefixIcon: Icon(Icons.phone_outlined),
                      ),
                      onChanged: (_) {
                        if (_recipientResult != null) {
                          setState(() => _recipientResult = null);
                        }
                      },
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton(
                    onPressed: _isCheckingRecipient ? null : _checkRecipient,
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                    ),
                    child: _isCheckingRecipient
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

              // نتيجة فحص المستلم وتطبيق الخصوصية
              if (_recipientResult != null)
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: Colors.green.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: Colors.green.shade300),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.account_circle, color: Colors.green, size: 36),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Text(
                                  _recipientResult!.displayName,
                                  style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                                ),
                                if (_recipientResult!.isNameMasked)
                                  Container(
                                    margin: const EdgeInsets.only(right: 6),
                                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: Colors.blue.withOpacity(0.15),
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                    child: const Text('اسم مقنع بالرموز', style: TextStyle(fontSize: 10, color: Colors.blue)),
                                  ),
                              ],
                            ),
                            Text(
                              'رقم الهاتف: ${_recipientResult!.phone}',
                              style: TextStyle(fontSize: 12, color: theme.colorScheme.onSurface.withOpacity(0.6)),
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

              // مبلغ التحويل
              Text(
                'المبلغ المراد تحويله (${_selectedWallet?.currencySymbol ?? ""})',
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
                  prefixIcon: const Icon(Icons.payments_outlined),
                  suffixText: _selectedWallet?.currencyCode,
                ),
              ),
              const SizedBox(height: 16),

              // بيان أو ملاحظات
              Text(
                'البيان أو الملاحظة (اختياري)',
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
                  hintText: 'سداد قيمة خدمات، إيجار، إلخ',
                  prefixIcon: Icon(Icons.note_alt_outlined),
                ),
              ),
              const SizedBox(height: 32),

              // زر تأكيد التحويل
              ElevatedButton.icon(
                onPressed: _isTransferring ? null : _handleTransfer,
                icon: const Icon(Icons.send_rounded),
                label: _isTransferring
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                      )
                    : const Text('تأكيد وتنفيذ التحويل المالي'),
                style: ElevatedButton.styleFrom(
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
